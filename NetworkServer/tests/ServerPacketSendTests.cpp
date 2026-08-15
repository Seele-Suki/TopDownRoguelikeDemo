#include "net/ServerCoordinator.h"
#include "net/SocketRuntime.h"
#include "net/TcpConnection.h"
#include "net/TcpListener.h"
#include "protocol/PacketCodec.h"

#include <WinSock2.h>
#include <WS2tcpip.h>

#include <cstdint>
#include <exception>
#include <iostream>
#include <stdexcept>
#include <string>
#include <vector>

namespace
{
    class TestSocket final
    {
    public:
        explicit TestSocket(SOCKET socket)
            : socket_(socket)
        {
        }

        ~TestSocket() noexcept
        {
            if (socket_ != INVALID_SOCKET)
            {
                ::closesocket(socket_);
            }
        }

        TestSocket(const TestSocket&) = delete;
        TestSocket& operator=(const TestSocket&) = delete;

        TestSocket(
            TestSocket&& other
        ) noexcept
            : socket_(other.socket_)
        {
            other.socket_ = INVALID_SOCKET;
        }

        [[nodiscard]]
        SOCKET NativeHandle() const noexcept
        {
            return socket_;
        }

    private:
        SOCKET socket_ = INVALID_SOCKET;
    };

    TestSocket ConnectLoopback(
        unsigned short port
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
                "Failed to create test client socket."
            );
        }

        TestSocket client(socket);

        sockaddr_in6 address{};
        address.sin6_family = AF_INET6;
        address.sin6_port = ::htons(port);

        if (::inet_pton(
            AF_INET6,
            "::1",
            &address.sin6_addr
        ) != 1)
        {
            throw std::runtime_error(
                "Failed to parse IPv6 loopback."
            );
        }

        if (::connect(
            socket,
            reinterpret_cast<const sockaddr*>(
                &address),
            sizeof(address)
        ) == SOCKET_ERROR)
        {
            throw std::runtime_error(
                "Failed to connect test client."
            );
        }

        return client;
    }

    std::vector<std::uint8_t> ReceiveExact(
        SOCKET socket,
        std::size_t size
    )
    {
        std::vector<std::uint8_t> bytes(size);
        std::size_t received = 0;

        while (received < size)
        {
            const int result =
                ::recv(
                    socket,
                    reinterpret_cast<char*>(
                        bytes.data() + received),
                    static_cast<int>(
                        size - received),
                    0
                );

            if (result <= 0)
            {
                throw std::runtime_error(
                    "Test client could not receive "
                    "the complete packet."
                );
            }

            received +=
                static_cast<std::size_t>(result);
        }

        return bytes;
    }
}

int main()
{
    try
    {
        tdr::net::SocketRuntime runtime;

        tdr::net::TcpListener listener;
        listener.BindAndListen(0);

        TestSocket client =
            ConnectLoopback(
                listener.BoundPort()
            );

        tdr::net::ServerCoordinator server;

        auto connection =
            listener.Accept();

        auto& session =
            server.AttachConnection(
                std::move(connection)
            );

        const std::vector<std::uint8_t> payload
        {
            0xAA,
            0x55
        };

        const auto expected =
            tdr::protocol::PacketCodec::Encode(
                tdr::protocol::MessageType::ServerHello,
                payload
            );

        server.SendPacketToPlayer(
            session.PlayerId(),
            tdr::protocol::MessageType::ServerHello,
            payload
        );

        const auto received =
            ReceiveExact(
                client.NativeHandle(),
                expected.size()
            );

        if (received != expected)
        {
            std::cerr
                << "[FAIL] Client received bytes "
                << "different from PacketCodec output."
                << std::endl;

            return 1;
        }

        std::cout
            << "[PASS] Server sent an encoded TCP "
            << "packet to an IPv6 client."
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