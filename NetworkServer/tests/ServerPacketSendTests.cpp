#include "net/ServerCoordinator.h"
#include "net/SocketRuntime.h"
#include "net/TcpConnection.h"
#include "net/TcpListener.h"
#include "protocol/PacketCodec.h"
#include "protocol/RoomStateSnapshotCodec.h"

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

    std::vector<std::uint8_t> ToBytes(
        const std::string& value
    )
    {
        return std::vector<std::uint8_t>(
            value.begin(),
            value.end()
        );
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

        SendSessionPacket(
            session,
            tdr::protocol::MessageType::SetNickname,
            ToBytes("Host")
        );

        SendSessionPacket(
            session,
            tdr::protocol::MessageType::
            CreateRoomRequest,
            {}
        );

        const std::string roomId =
            session.CurrentRoom().Id();

        TestSocket guestClient =
            ConnectLoopback(
                listener.BoundPort()
            );

        auto guestConnection =
            listener.Accept();

        auto& guestSession =
            server.AttachConnection(
                std::move(guestConnection)
            );

        SendSessionPacket(
            guestSession,
            tdr::protocol::MessageType::SetNickname,
            ToBytes("Guest")
        );

        SendSessionPacket(
            guestSession,
            tdr::protocol::MessageType::
            JoinRoomRequest,
            ToBytes(roomId)
        );

        const auto snapshotPayload =
            tdr::protocol::
            RoomStateSnapshotCodec::Encode(
                server.BuildRoomStateSnapshot(
                    roomId
                )
            );

        const auto expectedBroadcast =
            tdr::protocol::PacketCodec::Encode(
                tdr::protocol::MessageType::
                RoomStateSnapshot,
                snapshotPayload
            );

        server.BroadcastRoomState(
            roomId
        );

        const auto hostBroadcast =
            ReceiveExact(
                client.NativeHandle(),
                expectedBroadcast.size()
            );

        const auto guestBroadcast =
            ReceiveExact(
                guestClient.NativeHandle(),
                expectedBroadcast.size()
            );

        if (hostBroadcast != expectedBroadcast)
        {
            std::cerr
                << "[FAIL] Host received the wrong "
                << "room snapshot broadcast."
                << std::endl;

            return 1;
        }

        if (guestBroadcast != expectedBroadcast)
        {
            std::cerr
                << "[FAIL] Guest received the wrong "
                << "room snapshot broadcast."
                << std::endl;

            return 1;
        }

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

        SendSessionPacket(
            session,
            tdr::protocol::MessageType::
            StartGameRequest,
            {}
        );

        if (session.CurrentRoom().Status() !=
            tdr::room::RoomStatus::Started)
        {
            std::cerr
                << "[FAIL] GameStarted broadcast "
                << "test room did not start."
                << std::endl;

            return 1;
        }

        const auto expectedGameStarted =
            tdr::protocol::PacketCodec::Encode(
                tdr::protocol::MessageType::
                GameStarted,
                {}
            );

        server.BroadcastGameStarted(
            roomId
        );

        const auto hostGameStarted =
            ReceiveExact(
                client.NativeHandle(),
                expectedGameStarted.size()
            );

        const auto guestGameStarted =
            ReceiveExact(
                guestClient.NativeHandle(),
                expectedGameStarted.size()
            );

        if (hostGameStarted != expectedGameStarted)
        {
            std::cerr
                << "[FAIL] Host received the wrong "
                << "GameStarted packet."
                << std::endl;

            return 1;
        }

        if (guestGameStarted != expectedGameStarted)
        {
            std::cerr
                << "[FAIL] Guest received the wrong "
                << "GameStarted packet."
                << std::endl;

            return 1;
        }

        SendSessionPacket(
            session,
            tdr::protocol::MessageType::LeaveRoom,
            {}
        );

        const auto expectedForcedLeave =
            tdr::protocol::PacketCodec::Encode(
                tdr::protocol::MessageType::LeaveRoom,
                {}
            );

        const std::string roomClosedMessage =
            "Room was closed by the host.";

        const std::vector<std::uint8_t>
            roomClosedPayload(
                roomClosedMessage.begin(),
                roomClosedMessage.end()
            );

        const auto expectedRoomClosedError =
            tdr::protocol::PacketCodec::Encode(
                tdr::protocol::MessageType::
                ErrorMessage,
                roomClosedPayload
            );

        server.NotifyRoomClosed(
            roomId
        );

        const auto guestForcedLeave =
            ReceiveExact(
                guestClient.NativeHandle(),
                expectedForcedLeave.size()
            );

        const auto guestRoomClosedError =
            ReceiveExact(
                guestClient.NativeHandle(),
                expectedRoomClosedError.size()
            );

        if (guestForcedLeave != expectedForcedLeave)
        {
            std::cerr
                << "[FAIL] Guest received the wrong "
                << "forced LeaveRoom packet."
                << std::endl;

            return 1;
        }

        if (guestRoomClosedError !=
            expectedRoomClosedError)
        {
            std::cerr
                << "[FAIL] Guest received the wrong "
                << "room-closed ErrorMessage."
                << std::endl;

            return 1;
        }

        if (guestSession.InvalidateRoom(
            roomId))
        {
            std::cerr
                << "[FAIL] Room-close notification "
                << "did not clear the guest room reference."
                << std::endl;

            return 1;
        }

        tdr::net::ServerCoordinator
            disconnectServer;

        TestSocket disconnectHostClient =
            ConnectLoopback(
                listener.BoundPort()
            );

        auto disconnectHostConnection =
            listener.Accept();

        const SOCKET disconnectHostSocket =
            disconnectHostConnection.NativeHandle();

        auto& disconnectHostSession =
            disconnectServer.AttachConnection(
                std::move(disconnectHostConnection)
            );

        SendSessionPacket(
            disconnectHostSession,
            tdr::protocol::MessageType::SetNickname,
            ToBytes("DisconnectHost")
        );

        SendSessionPacket(
            disconnectHostSession,
            tdr::protocol::MessageType::
            CreateRoomRequest,
            {}
        );

        static_cast<void>(
            disconnectHostSession.TakeOutgoingPackets()
            );

        static_cast<void>(
            disconnectHostSession.TakeChangedRoomIds()
            );

        TestSocket disconnectGuestClient =
            ConnectLoopback(
                listener.BoundPort()
            );

        auto disconnectGuestConnection =
            listener.Accept();

        auto& disconnectGuestSession =
            disconnectServer.AttachConnection(
                std::move(disconnectGuestConnection)
            );

        SendSessionPacket(
            disconnectGuestSession,
            tdr::protocol::MessageType::SetNickname,
            ToBytes("DisconnectGuest")
        );

        SendSessionPacket(
            disconnectGuestSession,
            tdr::protocol::MessageType::
            JoinRoomRequest,
            ToBytes("ROOM-1")
        );

        static_cast<void>(
            disconnectGuestSession.TakeOutgoingPackets()
            );

        static_cast<void>(
            disconnectGuestSession.TakeChangedRoomIds()
            );

        disconnectServer.RemoveConnection(
            disconnectHostSocket
        );

        const auto disconnectedGuestLeave =
            ReceiveExact(
                disconnectGuestClient.NativeHandle(),
                expectedForcedLeave.size()
            );

        const auto disconnectedGuestError =
            ReceiveExact(
                disconnectGuestClient.NativeHandle(),
                expectedRoomClosedError.size()
            );

        if (disconnectedGuestLeave !=
            expectedForcedLeave)
        {
            std::cerr
                << "[FAIL] Host TCP disconnect did "
                << "not send LeaveRoom to the guest."
                << std::endl;

            return 1;
        }

        if (disconnectedGuestError !=
            expectedRoomClosedError)
        {
            std::cerr
                << "[FAIL] Host TCP disconnect did "
                << "not send ErrorMessage to the guest."
                << std::endl;

            return 1;
        }

        std::cout
            << "[PASS] Server sends direct packets "
            << "and broadcasts room snapshots."
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