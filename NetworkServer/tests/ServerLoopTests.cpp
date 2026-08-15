#include "net/ServerCoordinator.h"
#include "net/ServerLoop.h"
#include "net/SocketRuntime.h"
#include "net/TcpListener.h"
#include "protocol/PacketCodec.h"

#include <WinSock2.h>
#include <WS2tcpip.h>

#include <chrono>
#include <exception>
#include <iostream>
#include <stdexcept>
#include <string>
#include <cstdint>
#include <vector>
#include <functional>

namespace
{
    class TestClientSocket final
    {
    public:
        explicit TestClientSocket(
            const SOCKET socket
        )
            : socket_(socket)
        {
        }

        ~TestClientSocket() noexcept
        {
            Close();
        }

        TestClientSocket(
            const TestClientSocket&
        ) = delete;

        TestClientSocket& operator=(
            const TestClientSocket&
            ) = delete;

        TestClientSocket(
            TestClientSocket&& other
        ) noexcept
            : socket_(other.socket_)
        {
            other.socket_ = INVALID_SOCKET;
        }

        void Close() noexcept
        {
            if (socket_ != INVALID_SOCKET)
            {
                ::shutdown(
                    socket_,
                    SD_BOTH
                );

                ::closesocket(socket_);
                socket_ = INVALID_SOCKET;
            }
        }

        void Abort()
        {
            if (socket_ == INVALID_SOCKET)
            {
                return;
            }

            linger resetOption{};
            resetOption.l_onoff = 1;
            resetOption.l_linger = 0;

            const int optionResult =
                ::setsockopt(
                    socket_,
                    SOL_SOCKET,
                    SO_LINGER,
                    reinterpret_cast<const char*>(
                        &resetOption),
                    sizeof(resetOption)
                );

            if (optionResult == SOCKET_ERROR)
            {
                throw std::runtime_error(
                    "Failed to configure test TCP reset. "
                    "WSA error code: "
                    + std::to_string(
                        ::WSAGetLastError())
                );
            }

            ::closesocket(socket_);
            socket_ = INVALID_SOCKET;
        }

        [[nodiscard]]
        SOCKET NativeHandle() const noexcept
        {
            return socket_;
        }

    private:
        SOCKET socket_ = INVALID_SOCKET;
    };

    TestClientSocket ConnectLoopbackClient(
        const unsigned short port
    )
    {
        const SOCKET socket =
            ::socket(
                AF_INET6,
                SOCK_STREAM,
                IPPROTO_TCP
            );

        if (socket == INVALID_SOCKET)
        {
            throw std::runtime_error(
                "Failed to create test client socket. "
                "WSA error code: "
                + std::to_string(
                    ::WSAGetLastError())
            );
        }

        TestClientSocket client(socket);

        sockaddr_in6 serverAddress{};
        serverAddress.sin6_family = AF_INET6;
        serverAddress.sin6_port = ::htons(port);

        const int addressResult =
            ::inet_pton(
                AF_INET6,
                "::1",
                &serverAddress.sin6_addr
            );

        if (addressResult != 1)
        {
            throw std::runtime_error(
                "Failed to parse IPv6 loopback address."
            );
        }

        const int connectResult =
            ::connect(
                client.NativeHandle(),
                reinterpret_cast<const sockaddr*>(
                    &serverAddress),
                sizeof(serverAddress)
            );

        if (connectResult == SOCKET_ERROR)
        {
            throw std::runtime_error(
                "Failed to connect test client. "
                "WSA error code: "
                + std::to_string(
                    ::WSAGetLastError())
            );
        }

        return client;
    }

    void SendAll(
        const SOCKET socket,
        const std::vector<std::uint8_t>& bytes
    )
    {
        std::size_t sentByteCount = 0;

        while (sentByteCount < bytes.size())
        {
            const int result =
                ::send(
                    socket,
                    reinterpret_cast<const char*>(
                        bytes.data() + sentByteCount),
                    static_cast<int>(
                        bytes.size() - sentByteCount),
                    0
                );

            if (result == SOCKET_ERROR)
            {
                throw std::runtime_error(
                    "Failed to send test TCP bytes. "
                    "WSA error code: "
                    + std::to_string(
                        ::WSAGetLastError())
                );
            }

            if (result == 0)
            {
                throw std::runtime_error(
                    "Test TCP send made no progress."
                );
            }

            sentByteCount +=
                static_cast<std::size_t>(result);
        }
    }

    std::vector<std::uint8_t> ReceiveWithTimeout(
        const SOCKET socket,
        const std::size_t size
    )
    {
        fd_set readSet;
        FD_ZERO(&readSet);
        FD_SET(socket, &readSet);

        timeval timeout{};
        timeout.tv_sec = 1;
        timeout.tv_usec = 0;

        const int selectResult =
            ::select(
                0,
                &readSet,
                nullptr,
                nullptr,
                &timeout
            );

        if (selectResult == 0)
        {
            throw std::runtime_error(
                "Timed out waiting for "
                "the server response."
            );
        }

        if (selectResult == SOCKET_ERROR)
        {
            throw std::runtime_error(
                "Client select failed. "
                "WSA error code: "
                + std::to_string(
                    ::WSAGetLastError())
            );
        }

        std::vector<std::uint8_t> bytes(size);
        std::size_t receivedByteCount = 0;

        while (receivedByteCount < size)
        {
            const int result =
                ::recv(
                    socket,
                    reinterpret_cast<char*>(
                        bytes.data()
                        + receivedByteCount),
                    static_cast<int>(
                        size - receivedByteCount),
                    0
                );

            if (result <= 0)
            {
                throw std::runtime_error(
                    "Client could not receive "
                    "the complete server response."
                );
            }

            receivedByteCount +=
                static_cast<std::size_t>(result);
        }

        return bytes;
    }
}

int main()
{
    try
    {
        tdr::net::SocketRuntime socketRuntime;

        tdr::net::TcpListener listener;
        listener.BindAndListen(0);

        tdr::net::ServerCoordinator coordinator;

        tdr::net::ServerLoop serverLoop(
            listener,
            coordinator
        );

        TestClientSocket client =
            ConnectLoopbackClient(
                listener.BoundPort());

        serverLoop.PollOnce(
            std::chrono::milliseconds(1000)
        );

        if (coordinator.ConnectionCount() != 1)
        {
            std::cerr
                << "[FAIL] Server loop did not store "
                << "the accepted TCP connection."
                << std::endl;

            return 1;
        }

        if (coordinator.SessionCount() != 1)
        {
            std::cerr
                << "[FAIL] Server loop did not create "
                << "a session for the client."
                << std::endl;

            return 1;
        }

        const auto nicknamePacket =
            tdr::protocol::PacketCodec::Encode(
                tdr::protocol::MessageType::SetNickname,
                {
                    static_cast<std::uint8_t>('S'),
                    static_cast<std::uint8_t>('e'),
                    static_cast<std::uint8_t>('e'),
                    static_cast<std::uint8_t>('l'),
                    static_cast<std::uint8_t>('e')
                }
            );

        const auto createRoomPacket =
            tdr::protocol::PacketCodec::Encode(
                tdr::protocol::MessageType::CreateRoomRequest,
                {}
            );

        std::vector<std::uint8_t> combinedPackets;
        combinedPackets.reserve(
            nicknamePacket.size()
            + createRoomPacket.size()
        );

        combinedPackets.insert(
            combinedPackets.end(),
            nicknamePacket.begin(),
            nicknamePacket.end()
        );

        combinedPackets.insert(
            combinedPackets.end(),
            createRoomPacket.begin(),
            createRoomPacket.end()
        );

        SendAll(
            client.NativeHandle(),
            combinedPackets
        );

        serverLoop.PollOnce(
            std::chrono::milliseconds(1000)
        );

        try
        {
            const auto& createdRoom =
                coordinator.Rooms().FindRoom(
                    "ROOM-1"
                );

            if (createdRoom.PlayerCount() != 1
                || createdRoom.PlayerAt(0).nickname != "Seele")
            {
                std::cerr
                    << "[FAIL] Server loop produced "
                    << "incorrect room state."
                    << std::endl;

                return 1;
            }
        }
        catch (const std::out_of_range&)
        {
            std::cerr
                << "[FAIL] Server loop did not deliver "
                << "TCP packets to the client session."
                << std::endl;

            return 1;
        }

        const auto expectedResponse =
            tdr::protocol::PacketCodec::Encode(
                tdr::protocol::MessageType::
                CreateRoomResponse,
                {
                    static_cast<std::uint8_t>('R'),
                    static_cast<std::uint8_t>('O'),
                    static_cast<std::uint8_t>('O'),
                    static_cast<std::uint8_t>('M'),
                    static_cast<std::uint8_t>('-'),
                    static_cast<std::uint8_t>('1')
                }
            );

        try
        {
            const auto receivedResponse =
                ReceiveWithTimeout(
                    client.NativeHandle(),
                    expectedResponse.size()
                );

            if (receivedResponse != expectedResponse)
            {
                std::cerr
                    << "[FAIL] Server response bytes "
                    << "were incorrect."
                    << std::endl;

                return 1;
            }
        }
        catch (const std::exception& exception)
        {
            std::cerr
                << "[FAIL] Server did not send "
                << "CreateRoomResponse: "
                << exception.what()
                << std::endl;

            return 1;
        }

        client.Close();

        serverLoop.PollOnce(
            std::chrono::milliseconds(1000)
        );

        if (coordinator.ConnectionCount() != 0)
        {
            std::cerr
                << "[FAIL] Server loop did not remove "
                << "the disconnected TCP connection."
                << std::endl;

            return 1;
        }

        if (coordinator.SessionCount() != 0)
        {
            std::cerr
                << "[FAIL] Server loop did not remove "
                << "the disconnected client session."
                << std::endl;

            return 1;
        }

        TestClientSocket resetClient =
            ConnectLoopbackClient(
                listener.BoundPort());

        serverLoop.PollOnce(
            std::chrono::milliseconds(1000)
        );

        if (coordinator.ConnectionCount() != 1
            || coordinator.SessionCount() != 1)
        {
            std::cerr
                << "[FAIL] Server loop did not register "
                << "the reset-test client."
                << std::endl;

            return 1;
        }

        resetClient.Abort();

        try
        {
            serverLoop.PollOnce(
                std::chrono::milliseconds(1000)
            );
        }
        catch (const std::exception& exception)
        {
            std::cerr
                << "[FAIL] Server loop threw while handling "
                << "an aborted TCP connection: "
                << exception.what()
                << std::endl;

            return 1;
        }

        if (coordinator.ConnectionCount() != 0
            || coordinator.SessionCount() != 0)
        {
            std::cerr
                << "[FAIL] Server loop did not remove "
                << "the aborted TCP connection and session."
                << std::endl;

            return 1;
        }

        int stopCheckCount = 0;

        serverLoop.RunUntil(
            [&stopCheckCount]()
            {
                ++stopCheckCount;
                return stopCheckCount >= 3;
            },
            std::chrono::milliseconds(0)
        );

        if (stopCheckCount != 3)
        {
            std::cerr
                << "[FAIL] Server loop did not stop "
                << "when requested."
                << std::endl;

            return 1;
        }

        std::cout
            << "[PASS] Server loop handles TCP data, "
            << "disconnects, and controlled shutdown."
            << std::endl;

        return 0;
    }
    catch (const std::exception& exception)
    {
        std::cerr
            << "[FAIL] "
            << exception.what()
            << std::endl;

        return 1;
    }
}