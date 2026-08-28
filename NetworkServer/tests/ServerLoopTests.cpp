#include "net/ServerCoordinator.h"
#include "net/ServerLoop.h"
#include "net/SocketRuntime.h"
#include "net/TcpListener.h"
#include "net/UdpSocket.h"
#include "protocol/MessageHeader.h"
#include "protocol/PacketCodec.h"
#include "protocol/PlayerInputCodec.h"
#include "protocol/UdpBindingCredentialsCodec.h"
#include "protocol/UdpPacketCodec.h"
#include "protocol/RoomStateSnapshotCodec.h"
#include "protocol/PlayerStateSnapshotCodec.h"

#include <WinSock2.h>
#include <WS2tcpip.h>

#include <array>
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

    sockaddr_in6 CreateUdpLoopbackAddress(
        const unsigned short port
    )
    {
        sockaddr_in6 address{};
        address.sin6_family = AF_INET6;
        address.sin6_addr = in6addr_loopback;
        address.sin6_port = ::htons(port);

        return address;
    }

    void SetReceiveTimeout(
        const SOCKET socket
    )
    {
        const DWORD timeoutMilliseconds = 1000U;

        const int result =
            ::setsockopt(
                socket,
                SOL_SOCKET,
                SO_RCVTIMEO,
                reinterpret_cast<const char*>(
                    &timeoutMilliseconds),
                sizeof(timeoutMilliseconds)
            );

        if (result == SOCKET_ERROR)
        {
            throw std::runtime_error(
                "Failed to set UDP receive timeout."
            );
        }
    }

    bool IsReadableWithin(
        const SOCKET socket,
        const std::chrono::milliseconds timeout
    )
    {
        fd_set readSet;
        FD_ZERO(&readSet);
        FD_SET(socket, &readSet);

        timeval timeoutValue{};
        timeoutValue.tv_sec =
            static_cast<long>(
                timeout.count() / 1000
            );
        timeoutValue.tv_usec =
            static_cast<long>(
                (timeout.count() % 1000) * 1000
            );

        const int result =
            ::select(
                0,
                &readSet,
                nullptr,
                nullptr,
                &timeoutValue
            );

        if (result == SOCKET_ERROR)
        {
            throw std::runtime_error(
                "UDP test select failed."
            );
        }

        return result > 0;
    }

    void SendSessionPacket(
        tdr::net::TcpClientSession& session,
        const tdr::protocol::MessageType type,
        const std::vector<std::uint8_t>& payload
    )
    {
        const auto encoded =
            tdr::protocol::PacketCodec::Encode(
                type,
                payload
            );

        session.ReceiveBytes(
            encoded.data(),
            encoded.size()
        );
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

    bool PlayerInputIsForwardedToHost()
    {
        tdr::net::TcpListener listener;
        listener.BindAndListen(0);

        tdr::net::UdpSocket serverUdpSocket;
        serverUdpSocket.Bind(0);

        tdr::net::ServerCoordinator coordinator;

        auto& host =
            coordinator.CreateSession();

        auto& guest =
            coordinator.CreateSession();

        SendSessionPacket(
            host,
            tdr::protocol::MessageType::SetNickname,
            {
                static_cast<std::uint8_t>('H'),
                static_cast<std::uint8_t>('o'),
                static_cast<std::uint8_t>('s'),
                static_cast<std::uint8_t>('t')
            });

        SendSessionPacket(
            host,
            tdr::protocol::MessageType::CreateRoomRequest,
            {});

        SendSessionPacket(
            guest,
            tdr::protocol::MessageType::SetNickname,
            {
                static_cast<std::uint8_t>('G'),
                static_cast<std::uint8_t>('u'),
                static_cast<std::uint8_t>('e'),
                static_cast<std::uint8_t>('s'),
                static_cast<std::uint8_t>('t')
            });

        SendSessionPacket(
            guest,
            tdr::protocol::MessageType::JoinRoomRequest,
            {});

        SendSessionPacket(
            host,
            tdr::protocol::MessageType::SetPlayerSelection,
            {
                static_cast<std::uint8_t>(
                    tdr::room::CharacterId::Ranged),
                static_cast<std::uint8_t>(
                    tdr::room::DifficultyId::Normal)
            });

        SendSessionPacket(
            guest,
            tdr::protocol::MessageType::SetPlayerSelection,
            {
                static_cast<std::uint8_t>(
                    tdr::room::CharacterId::Melee),
                static_cast<std::uint8_t>(
                    tdr::room::DifficultyId::None)
            });

        SendSessionPacket(
            host,
            tdr::protocol::MessageType::SetReady,
            { 1U });

        SendSessionPacket(
            guest,
            tdr::protocol::MessageType::SetReady,
            { 1U });

        SendSessionPacket(
            host,
            tdr::protocol::MessageType::StartGameRequest,
            {});

        tdr::net::UdpSocket hostUdpSocket;
        hostUdpSocket.Bind(0);

        tdr::net::UdpSocket guestUdpSocket;
        guestUdpSocket.Bind(0);

        SetReceiveTimeout(
            hostUdpSocket.NativeHandle());

        SetReceiveTimeout(
            guestUdpSocket.NativeHandle());

        const sockaddr_in6 hostAddress =
            CreateUdpLoopbackAddress(
                hostUdpSocket.BoundPort());

        const sockaddr_in6 guestAddress =
            CreateUdpLoopbackAddress(
                guestUdpSocket.BoundPort());

        host.BindUdpEndpoint(hostAddress);
        guest.BindUdpEndpoint(guestAddress);

        tdr::net::ServerLoop serverLoop(
            listener,
            serverUdpSocket,
            coordinator);

        const tdr::protocol::PlayerInputPayload input{
            0.75F,
            -0.5F,
            1.0F,
            0.25F
        };

        const auto payload =
            tdr::protocol::PlayerInputCodec::Encode(
                input);

        tdr::protocol::UdpMessageHeader header{};
        header.type =
            tdr::protocol::MessageType::PlayerInput;
        header.sessionToken =
            guest.SessionTokenBytes();
        header.playerId =
            guest.PlayerId();
        header.sequence = 21U;

        const auto datagram =
            tdr::protocol::UdpPacketCodec::Encode(
                header,
                payload);

        const sockaddr_in6 serverAddress =
            CreateUdpLoopbackAddress(
                serverUdpSocket.BoundPort());

        static_cast<void>(
            guestUdpSocket.SendTo(
                datagram.data(),
                datagram.size(),
                serverAddress)
            );

        serverLoop.PollOnce(
            std::chrono::milliseconds(1000));

        std::array<std::uint8_t, 256>
            receiveBuffer{};

        sockaddr_in6 responseSource{};

        const std::size_t receivedSize =
            hostUdpSocket.ReceiveFrom(
                receiveBuffer.data(),
                receiveBuffer.size(),
                responseSource);

        const auto received =
            tdr::protocol::UdpPacketCodec::Decode(
                receiveBuffer.data(),
                receivedSize);

        if (received.header.type !=
            tdr::protocol::MessageType::PlayerInput ||
            received.header.playerId !=
            guest.PlayerId() ||
            received.header.sequence != 21U ||
            received.header.sessionToken !=
            host.SessionTokenBytes() ||
            received.payload != payload)
        {
            std::cerr
                << "[FAIL] ServerLoop forwarded "
                << "the wrong PlayerInput datagram."
                << std::endl;

            return false;
        }

        const tdr::protocol::PlayerStateSnapshotPayload
            stateSnapshot{
                {
                    {
                        host.PlayerId(),
                        -1.0F,
                        2.0F,
                        1.0F,
                        0.0F
                    },
                    {
                        guest.PlayerId(),
                        3.0F,
                        -2.0F,
                        0.0F,
                        1.0F
                    }
                }
        };

        const auto statePayload =
            tdr::protocol::
            PlayerStateSnapshotCodec::Encode(
                stateSnapshot);

        tdr::protocol::UdpMessageHeader
            stateHeader{};

        stateHeader.type =
            tdr::protocol::MessageType::
            PlayerStateSnapshot;

        stateHeader.sessionToken =
            host.SessionTokenBytes();

        stateHeader.playerId =
            host.PlayerId();

        stateHeader.sequence = 31U;

        const auto stateDatagram =
            tdr::protocol::UdpPacketCodec::Encode(
                stateHeader,
                statePayload);

        static_cast<void>(
            hostUdpSocket.SendTo(
                stateDatagram.data(),
                stateDatagram.size(),
                serverAddress)
            );

        serverLoop.PollOnce(
            std::chrono::milliseconds(1000));

        sockaddr_in6 stateResponseSource{};

        const std::size_t stateReceivedSize =
            guestUdpSocket.ReceiveFrom(
                receiveBuffer.data(),
                receiveBuffer.size(),
                stateResponseSource);

        const auto receivedState =
            tdr::protocol::UdpPacketCodec::Decode(
                receiveBuffer.data(),
                stateReceivedSize);

        if (receivedState.header.type !=
            tdr::protocol::MessageType::
            PlayerStateSnapshot ||
            receivedState.header.playerId !=
            host.PlayerId() ||
            receivedState.header.sequence != 31U ||
            receivedState.header.sessionToken !=
            guest.SessionTokenBytes() ||
            receivedState.payload != statePayload)
        {
            std::cerr
                << "[FAIL] ServerLoop forwarded "
                << "the wrong PlayerStateSnapshot."
                << std::endl;

            return false;
        }

        return true;
    }
}

int main()
{
    try
    {
        tdr::net::SocketRuntime socketRuntime;

        if (!PlayerInputIsForwardedToHost())
        {
            return 1;
        }

        tdr::net::TcpListener listener;
        listener.BindAndListen(0);

        tdr::net::UdpSocket udpSocket;
        udpSocket.Bind(0);

        tdr::net::ServerCoordinator coordinator;

        tdr::net::ServerLoop serverLoop(
            listener,
            udpSocket,
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

        const auto serverHelloBytes =
            ReceiveWithTimeout(
                client.NativeHandle(),
                tdr::protocol::kMessageHeaderSize
                + tdr::protocol::
                    kUdpBindingCredentialsSize
            );

        tdr::protocol::PacketCodec serverHelloCodec;
        serverHelloCodec.Append(
            serverHelloBytes.data(),
            serverHelloBytes.size()
        );

        const auto serverHelloPackets =
            serverHelloCodec.DecodeAvailable();

        if (serverHelloPackets.size() != 1
            || serverHelloPackets.front().type
                != tdr::protocol::MessageType::ServerHello)
        {
            std::cerr
                << "[FAIL] Server did not send exactly one "
                << "ServerHello after accepting TCP."
                << std::endl;

            return 1;
        }

        const auto udpCredentials =
            tdr::protocol::UdpBindingCredentialsCodec::Decode(
                serverHelloPackets.front().payload.data(),
                serverHelloPackets.front().payload.size()
            );

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

        const auto expectedSnapshotPayload =
            tdr::protocol::
            RoomStateSnapshotCodec::Encode(
                coordinator.BuildRoomStateSnapshot(
                    "ROOM-1"
                )
            );

        const auto expectedSnapshotPacket =
            tdr::protocol::PacketCodec::Encode(
                tdr::protocol::MessageType::
                RoomStateSnapshot,
                expectedSnapshotPayload
            );

        try
        {
            const auto receivedSnapshot =
                ReceiveWithTimeout(
                    client.NativeHandle(),
                    expectedSnapshotPacket.size()
                );

            if (receivedSnapshot !=
                expectedSnapshotPacket)
            {
                std::cerr
                    << "[FAIL] Server loop sent "
                    << "the wrong room snapshot."
                    << std::endl;

                return 1;
            }
        }
        catch (const std::exception& exception)
        {
            std::cerr
                << "[FAIL] Server loop did not "
                << "automatically broadcast the "
                << "created room snapshot: "
                << exception.what()
                << std::endl;

            return 1;
        }

        const auto& room =
            coordinator.Rooms().FindRoom(
                "ROOM-1"
            );

        const std::uint32_t playerId =
            udpCredentials.playerId;

        if (room.PlayerAt(0).playerId != playerId)
        {
            std::cerr
                << "[FAIL] ServerHello contained the wrong "
                << "player ID."
                << std::endl;

            return 1;
        }

        auto& session =
            coordinator.FindSession(
                playerId
            );

        TestClientSocket gameGuest =
            ConnectLoopbackClient(
                listener.BoundPort()
            );

        serverLoop.PollOnce(
            std::chrono::milliseconds(1000)
        );

        const auto guestHelloBytes =
            ReceiveWithTimeout(
                gameGuest.NativeHandle(),
                tdr::protocol::kMessageHeaderSize
                + tdr::protocol::
                kUdpBindingCredentialsSize
            );

        tdr::protocol::PacketCodec
            guestHelloCodec;

        guestHelloCodec.Append(
            guestHelloBytes.data(),
            guestHelloBytes.size()
        );

        const auto guestHelloPackets =
            guestHelloCodec.DecodeAvailable();

        if (guestHelloPackets.size() != 1U ||
            guestHelloPackets.front().type !=
            tdr::protocol::MessageType::
            ServerHello)
        {
            std::cerr
                << "[FAIL] Game-start guest did "
                << "not receive ServerHello."
                << std::endl;

            return 1;
        }

        const auto guestCredentials =
            tdr::protocol::
            UdpBindingCredentialsCodec::Decode(
                guestHelloPackets.front()
                .payload.data(),
                guestHelloPackets.front()
                .payload.size()
            );

        auto& guestSession =
            coordinator.FindSession(
                guestCredentials.playerId
            );

        SendSessionPacket(
            guestSession,
            tdr::protocol::MessageType::SetNickname,
            {
                static_cast<std::uint8_t>('G'),
                static_cast<std::uint8_t>('u'),
                static_cast<std::uint8_t>('e'),
                static_cast<std::uint8_t>('s'),
                static_cast<std::uint8_t>('t')
            }
        );

        SendSessionPacket(
            guestSession,
            tdr::protocol::MessageType::
            JoinRoomRequest,
            {}
        );

        static_cast<void>(
            guestSession.TakeOutgoingPackets()
            );

        static_cast<void>(
            guestSession.TakeChangedRoomIds()
            );

        SendSessionPacket(
            session,
            tdr::protocol::MessageType::
            SetPlayerSelection,
            {
                static_cast<std::uint8_t>(
                    tdr::room::CharacterId::Ranged),
                static_cast<std::uint8_t>(
                    tdr::room::DifficultyId::Normal)
            }
        );

        SendSessionPacket(
            session,
            tdr::protocol::MessageType::SetReady,
            {
                static_cast<std::uint8_t>(1)
            }
        );

        static_cast<void>(
            session.TakeChangedRoomIds()
            );

        SendSessionPacket(
            guestSession,
            tdr::protocol::MessageType::
            SetPlayerSelection,
            {
                static_cast<std::uint8_t>(
                    tdr::room::CharacterId::Melee),
                static_cast<std::uint8_t>(
                    tdr::room::DifficultyId::None)
            }
        );

        SendSessionPacket(
            guestSession,
            tdr::protocol::MessageType::SetReady,
            {
                static_cast<std::uint8_t>(1)
            }
        );

        static_cast<void>(
            guestSession.TakeChangedRoomIds()
            );

        SendSessionPacket(
            session,
            tdr::protocol::MessageType::
            StartGameRequest,
            {}
        );

        const auto finalSnapshotPayload =
            tdr::protocol::
            RoomStateSnapshotCodec::Encode(
                coordinator.BuildRoomStateSnapshot(
                    "ROOM-1"
                )
            );

        const auto finalSnapshotPacket =
            tdr::protocol::PacketCodec::Encode(
                tdr::protocol::MessageType::
                RoomStateSnapshot,
                finalSnapshotPayload
            );

        const auto gameStartedPacket =
            tdr::protocol::PacketCodec::Encode(
                tdr::protocol::MessageType::
                GameStarted,
                {}
            );

        const auto wakeLoopPacket =
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

        SendAll(
            client.NativeHandle(),
            wakeLoopPacket
        );

        serverLoop.PollOnce(
            std::chrono::milliseconds(1000)
        );

        const auto hostFinalSnapshot =
            ReceiveWithTimeout(
                client.NativeHandle(),
                finalSnapshotPacket.size()
            );

        const auto guestFinalSnapshot =
            ReceiveWithTimeout(
                gameGuest.NativeHandle(),
                finalSnapshotPacket.size()
            );

        if (hostFinalSnapshot !=
            finalSnapshotPacket ||
            guestFinalSnapshot !=
            finalSnapshotPacket)
        {
            std::cerr
                << "[FAIL] Server loop did not "
                << "broadcast the final started snapshot."
                << std::endl;

            return 1;
        }

        const auto hostGameStarted =
            ReceiveWithTimeout(
                client.NativeHandle(),
                gameStartedPacket.size()
            );

        const auto guestGameStarted =
            ReceiveWithTimeout(
                gameGuest.NativeHandle(),
                gameStartedPacket.size()
            );

        if (hostGameStarted != gameStartedPacket ||
            guestGameStarted != gameStartedPacket)
        {
            std::cerr
                << "[FAIL] Server loop did not "
                << "broadcast GameStarted to both players."
                << std::endl;

            return 1;
        }

        gameGuest.Close();

        serverLoop.PollOnce(
            std::chrono::milliseconds(1000)
        );

        if (coordinator.ConnectionCount() != 1U ||
            coordinator.SessionCount() != 1U)
        {
            std::cerr
                << "[FAIL] Game-start guest cleanup "
                << "left the wrong connection count."
                << std::endl;

            return 1;
        }

        const auto guestDisconnectedSnapshot =
            tdr::protocol::PacketCodec::Encode(
                tdr::protocol::MessageType::
                RoomStateSnapshot,
                tdr::protocol::
                RoomStateSnapshotCodec::Encode(
                    coordinator.BuildRoomStateSnapshot(
                        "ROOM-1"
                    )
                )
            );

        try
        {
            const auto receivedSnapshot =
                ReceiveWithTimeout(
                    client.NativeHandle(),
                    guestDisconnectedSnapshot.size()
                );

            if (receivedSnapshot !=
                guestDisconnectedSnapshot)
            {
                std::cerr
                    << "[FAIL] Guest disconnect sent "
                    << "the wrong room snapshot."
                    << std::endl;

                return 1;
            }
        }
        catch (const std::exception& exception)
        {
            std::cerr
                << "[FAIL] Guest disconnect did not "
                << "broadcast the updated room snapshot: "
                << exception.what()
                << std::endl;

            return 1;
        }

        tdr::net::UdpSocket udpClient;
        udpClient.Bind(0);
        SetReceiveTimeout(
            udpClient.NativeHandle()
        );

        tdr::protocol::UdpMessageHeader bindHeader{};
        bindHeader.type =
            tdr::protocol::MessageType::UdpBindRequest;
        bindHeader.sessionToken =
            udpCredentials.sessionToken;
        bindHeader.playerId = playerId;
        bindHeader.sequence = 41U;

        const auto bindRequest =
            tdr::protocol::UdpPacketCodec::Encode(
                bindHeader,
                {}
            );

        const sockaddr_in6 udpServerAddress =
            CreateUdpLoopbackAddress(
                udpSocket.BoundPort()
            );

        static_cast<void>(
            udpClient.SendTo(
                bindRequest.data(),
                bindRequest.size(),
                udpServerAddress
            )
        );

        serverLoop.PollOnce(
            std::chrono::milliseconds(1000)
        );

        std::array<std::uint8_t, 256>
            bindResponseBuffer{};
        sockaddr_in6 bindResponseSource{};

        const std::size_t bindResponseSize =
            udpClient.ReceiveFrom(
                bindResponseBuffer.data(),
                bindResponseBuffer.size(),
                bindResponseSource
            );

        const auto bindResponse =
            tdr::protocol::UdpPacketCodec::Decode(
                bindResponseBuffer.data(),
                bindResponseSize
            );

        const sockaddr_in6 expectedClientAddress =
            CreateUdpLoopbackAddress(
                udpClient.BoundPort()
            );

        if (bindResponse.header.type
                != tdr::protocol::MessageType::UdpBindAccepted
            || bindResponse.header.sessionToken
                != bindHeader.sessionToken
            || bindResponse.header.playerId
                != bindHeader.playerId
            || bindResponse.header.sequence
                != bindHeader.sequence
            || !bindResponse.payload.empty()
            || !session.MatchesUdpEndpoint(
                expectedClientAddress))
        {
            std::cerr
                << "[FAIL] Server loop did not complete "
                << "the UDP binding handshake."
                << std::endl;

            return 1;
        }

        auto pingHeader = bindHeader;
        pingHeader.type =
            tdr::protocol::MessageType::UdpPing;
        pingHeader.sequence = 42U;

        const auto pingRequest =
            tdr::protocol::UdpPacketCodec::Encode(
                pingHeader,
                {}
            );

        static_cast<void>(
            udpClient.SendTo(
                pingRequest.data(),
                pingRequest.size(),
                udpServerAddress
            )
        );

        serverLoop.PollOnce(
            std::chrono::milliseconds(1000)
        );

        const std::size_t pongSize =
            udpClient.ReceiveFrom(
                bindResponseBuffer.data(),
                bindResponseBuffer.size(),
                bindResponseSource
            );

        const auto pong =
            tdr::protocol::UdpPacketCodec::Decode(
                bindResponseBuffer.data(),
                pongSize
            );

        if (pong.header.type
                != tdr::protocol::MessageType::UdpPong
            || pong.header.sessionToken
                != pingHeader.sessionToken
            || pong.header.playerId
                != pingHeader.playerId
            || pong.header.sequence
                != pingHeader.sequence
            || !pong.payload.empty())
        {
            std::cerr
                << "[FAIL] Server loop did not route "
                << "UDP Ping to Pong."
                << std::endl;

            return 1;
        }

        const auto expectRejectedUdpRequest =
            [&serverLoop, &udpServerAddress](
                tdr::net::UdpSocket& sender,
                const tdr::protocol::UdpMessageHeader& header,
                const std::vector<std::uint8_t>& payload,
                const char* const description
            )
            {
                const auto request =
                    tdr::protocol::UdpPacketCodec::Encode(
                        header,
                        payload
                    );

                static_cast<void>(
                    sender.SendTo(
                        request.data(),
                        request.size(),
                        udpServerAddress
                    )
                );

                try
                {
                    serverLoop.PollOnce(
                        std::chrono::milliseconds(1000)
                    );
                }
                catch (const std::exception& exception)
                {
                    std::cerr
                        << "[FAIL] Rejected UDP "
                        << description
                        << " escaped the server loop: "
                        << exception.what()
                        << std::endl;

                    return false;
                }

                if (IsReadableWithin(
                        sender.NativeHandle(),
                        std::chrono::milliseconds(100)))
                {
                    std::cerr
                        << "[FAIL] Server responded to "
                        << description
                        << "."
                        << std::endl;

                    return false;
                }

                return true;
            };

        auto invalidTokenHeader = bindHeader;
        invalidTokenHeader.sessionToken[0] ^= 0xFFU;
        invalidTokenHeader.sequence = 43U;

        if (!expectRejectedUdpRequest(
                udpClient,
                invalidTokenHeader,
                {},
                "an invalid session token"))
        {
            return 1;
        }

        if (!expectRejectedUdpRequest(
                udpClient,
                bindHeader,
                {},
                "a duplicate UDP sequence"))
        {
            return 1;
        }

        auto expiredSequenceHeader = bindHeader;
        expiredSequenceHeader.sequence = 40U;

        if (!expectRejectedUdpRequest(
                udpClient,
                expiredSequenceHeader,
                {},
                "an expired UDP sequence"))
        {
            return 1;
        }

        auto unknownPlayerHeader = bindHeader;
        unknownPlayerHeader.playerId += 1000U;
        unknownPlayerHeader.sequence = 43U;

        if (!expectRejectedUdpRequest(
                udpClient,
                unknownPlayerHeader,
                {},
                "an unknown player"))
        {
            return 1;
        }

        auto nonEmptyPayloadHeader = bindHeader;
        nonEmptyPayloadHeader.sequence = 44U;

        if (!expectRejectedUdpRequest(
                udpClient,
                nonEmptyPayloadHeader,
                { 0x01U },
                "a non-empty BindRequest payload"))
        {
            return 1;
        }

        tdr::net::UdpSocket replacementUdpClient;
        replacementUdpClient.Bind(0);
        SetReceiveTimeout(
            replacementUdpClient.NativeHandle()
        );

        auto replacementAddressHeader = bindHeader;
        replacementAddressHeader.sequence = 45U;

        if (!expectRejectedUdpRequest(
                replacementUdpClient,
                replacementAddressHeader,
                {},
                "a replacement source address"))
        {
            return 1;
        }

        if (!session.MatchesUdpEndpoint(
            expectedClientAddress))
        {
            std::cerr
                << "[FAIL] Rejected UDP data changed "
                << "the existing endpoint binding."
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

        const std::string resetRoomId =
            "ROOM-2";

        const auto resetServerHello =
            ReceiveWithTimeout(
                resetClient.NativeHandle(),
                tdr::protocol::kMessageHeaderSize
                + tdr::protocol::
                kUdpBindingCredentialsSize
            );

        tdr::protocol::PacketCodec
            resetServerHelloCodec;

        resetServerHelloCodec.Append(
            resetServerHello.data(),
            resetServerHello.size()
        );

        if (resetServerHelloCodec
            .DecodeAvailable()
            .size() != 1U)
        {
            std::cerr
                << "[FAIL] Reset client did not "
                << "receive ServerHello."
                << std::endl;

            return 1;
        }

        const auto resetNicknamePacket =
            tdr::protocol::PacketCodec::Encode(
                tdr::protocol::MessageType::SetNickname,
                {
                    static_cast<std::uint8_t>('R'),
                    static_cast<std::uint8_t>('e'),
                    static_cast<std::uint8_t>('s'),
                    static_cast<std::uint8_t>('e'),
                    static_cast<std::uint8_t>('t')
                }
            );

        const auto resetCreatePacket =
            tdr::protocol::PacketCodec::Encode(
                tdr::protocol::MessageType::
                CreateRoomRequest,
                {}
            );

        std::vector<std::uint8_t>
            resetCreatePackets =
            resetNicknamePacket;

        resetCreatePackets.insert(
            resetCreatePackets.end(),
            resetCreatePacket.begin(),
            resetCreatePacket.end()
        );

        SendAll(
            resetClient.NativeHandle(),
            resetCreatePackets
        );

        serverLoop.PollOnce(
            std::chrono::milliseconds(1000)
        );

        const auto resetCreateResponse =
            tdr::protocol::PacketCodec::Encode(
                tdr::protocol::MessageType::
                CreateRoomResponse,
                {
                    static_cast<std::uint8_t>('R'),
                    static_cast<std::uint8_t>('O'),
                    static_cast<std::uint8_t>('O'),
                    static_cast<std::uint8_t>('M'),
                    static_cast<std::uint8_t>('-'),
                    static_cast<std::uint8_t>('2')
                }
            );

        static_cast<void>(
            ReceiveWithTimeout(
                resetClient.NativeHandle(),
                resetCreateResponse.size()
            )
            );

        const auto resetSnapshot =
            tdr::protocol::PacketCodec::Encode(
                tdr::protocol::MessageType::
                RoomStateSnapshot,
                tdr::protocol::
                RoomStateSnapshotCodec::Encode(
                    coordinator.BuildRoomStateSnapshot(
                        resetRoomId
                    )
                )
            );

        static_cast<void>(
            ReceiveWithTimeout(
                resetClient.NativeHandle(),
                resetSnapshot.size()
            )
            );

        TestClientSocket closureGuest =
            ConnectLoopbackClient(
                listener.BoundPort()
            );

        serverLoop.PollOnce(
            std::chrono::milliseconds(1000)
        );

        const auto closureGuestHello =
            ReceiveWithTimeout(
                closureGuest.NativeHandle(),
                tdr::protocol::kMessageHeaderSize
                + tdr::protocol::
                kUdpBindingCredentialsSize
            );

        if (closureGuestHello.empty())
        {
            std::cerr
                << "[FAIL] Closure guest did not "
                << "receive ServerHello."
                << std::endl;

            return 1;
        }

        const auto closureGuestNickname =
            tdr::protocol::PacketCodec::Encode(
                tdr::protocol::MessageType::SetNickname,
                {
                    static_cast<std::uint8_t>('C'),
                    static_cast<std::uint8_t>('l'),
                    static_cast<std::uint8_t>('o'),
                    static_cast<std::uint8_t>('s'),
                    static_cast<std::uint8_t>('e')
                }
            );

        const auto closureGuestJoin =
            tdr::protocol::PacketCodec::Encode(
                tdr::protocol::MessageType::
                JoinRoomRequest,
                {}
            );

        std::vector<std::uint8_t>
            closureGuestPackets =
            closureGuestNickname;

        closureGuestPackets.insert(
            closureGuestPackets.end(),
            closureGuestJoin.begin(),
            closureGuestJoin.end()
        );

        SendAll(
            closureGuest.NativeHandle(),
            closureGuestPackets
        );

        serverLoop.PollOnce(
            std::chrono::milliseconds(1000)
        );

        const auto closureGuestJoinResponse =
            tdr::protocol::PacketCodec::Encode(
                tdr::protocol::MessageType::
                JoinRoomResponse,
                {
                    static_cast<std::uint8_t>('R'),
                    static_cast<std::uint8_t>('O'),
                    static_cast<std::uint8_t>('O'),
                    static_cast<std::uint8_t>('M'),
                    static_cast<std::uint8_t>('-'),
                    static_cast<std::uint8_t>('2')
                }
            );

        static_cast<void>(
            ReceiveWithTimeout(
                closureGuest.NativeHandle(),
                closureGuestJoinResponse.size()
            )
            );

        const auto twoPlayerSnapshot =
            tdr::protocol::PacketCodec::Encode(
                tdr::protocol::MessageType::
                RoomStateSnapshot,
                tdr::protocol::
                RoomStateSnapshotCodec::Encode(
                    coordinator.BuildRoomStateSnapshot(
                        resetRoomId
                    )
                )
            );

        static_cast<void>(
            ReceiveWithTimeout(
                resetClient.NativeHandle(),
                twoPlayerSnapshot.size()
            )
            );

        static_cast<void>(
            ReceiveWithTimeout(
                closureGuest.NativeHandle(),
                twoPlayerSnapshot.size()
            )
            );

        const auto resetLeavePacket =
            tdr::protocol::PacketCodec::Encode(
                tdr::protocol::MessageType::LeaveRoom,
                {}
            );

        SendAll(
            resetClient.NativeHandle(),
            resetLeavePacket
        );

        serverLoop.PollOnce(
            std::chrono::milliseconds(1000)
        );

        const auto closureError =
            tdr::protocol::PacketCodec::Encode(
                tdr::protocol::MessageType::
                ErrorMessage,
                {
                    static_cast<std::uint8_t>('R'),
                    static_cast<std::uint8_t>('o'),
                    static_cast<std::uint8_t>('o'),
                    static_cast<std::uint8_t>('m'),
                    static_cast<std::uint8_t>(' '),
                    static_cast<std::uint8_t>('w'),
                    static_cast<std::uint8_t>('a'),
                    static_cast<std::uint8_t>('s'),
                    static_cast<std::uint8_t>(' '),
                    static_cast<std::uint8_t>('c'),
                    static_cast<std::uint8_t>('l'),
                    static_cast<std::uint8_t>('o'),
                    static_cast<std::uint8_t>('s'),
                    static_cast<std::uint8_t>('e'),
                    static_cast<std::uint8_t>('d'),
                    static_cast<std::uint8_t>(' '),
                    static_cast<std::uint8_t>('b'),
                    static_cast<std::uint8_t>('y'),
                    static_cast<std::uint8_t>(' '),
                    static_cast<std::uint8_t>('t'),
                    static_cast<std::uint8_t>('h'),
                    static_cast<std::uint8_t>('e'),
                    static_cast<std::uint8_t>(' '),
                    static_cast<std::uint8_t>('h'),
                    static_cast<std::uint8_t>('o'),
                    static_cast<std::uint8_t>('s'),
                    static_cast<std::uint8_t>('t'),
                    static_cast<std::uint8_t>('.')
                }
            );

        static_cast<void>(
            ReceiveWithTimeout(
                resetClient.NativeHandle(),
                resetLeavePacket.size()
            )
            );

        static_cast<void>(
            ReceiveWithTimeout(
                closureGuest.NativeHandle(),
                resetLeavePacket.size()
            )
            );

        static_cast<void>(
            ReceiveWithTimeout(
                closureGuest.NativeHandle(),
                closureError.size()
            )
            );

        closureGuest.Close();

        serverLoop.PollOnce(
            std::chrono::milliseconds(1000)
        );

        if (coordinator.ConnectionCount() != 1U ||
            coordinator.SessionCount() != 1U)
        {
            std::cerr
                << "[FAIL] Closure guest cleanup "
                << "left the wrong connection count."
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
                << "[FAIL] Server loop threw while "
                << "handling the reset client: "
                << exception.what()
                << std::endl;

            return 1;
        }

        if (coordinator.ConnectionCount() != 0U ||
            coordinator.SessionCount() != 0U)
        {
            std::cerr
                << "[FAIL] Server loop did not remove "
                << "the reset client and closure guest."
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
            << "[PASS] Server loop handles TCP data, UDP "
            << "binding, disconnects, and controlled shutdown."
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
