#pragma once

#include "net/TcpClientSession.h"
#include "net/TcpConnection.h"
#include "room/PlayerIdAllocator.h"
#include "room/RoomManager.h"
#include "room/SessionTokenGenerator.h"
#include "protocol/PacketCodec.h"
#include "protocol/UdpMessageHeader.h"
#include "protocol/RoomStateSnapshotCodec.h"

#include <cstddef>
#include <cstdint>
#include <memory>
#include <unordered_map>
#include <vector>
#include <string>
#include <chrono>

namespace tdr::net
{
    class ServerCoordinator final
    {
    public:
        [[nodiscard]]
        TcpClientSession& CreateSession();

        [[nodiscard]]
        TcpClientSession& AttachConnection(
            TcpConnection connection
        );

        [[nodiscard]]
        TcpClientSession& FindSession(
            std::uint32_t playerId
        );

        [[nodiscard]]
        const TcpClientSession& FindSession(
            std::uint32_t playerId
        ) const;

        [[nodiscard]]
        TcpClientSession& FindSessionForUdp(
            const tdr::protocol::UdpMessageHeader& header
        );

        [[nodiscard]]
        TcpClientSession& BindUdpEndpoint(
            const tdr::protocol::UdpMessageHeader& header,
            const sockaddr_in6& sourceAddress
        );

        [[nodiscard]]
        TcpClientSession& FindSessionBySocket(
            SOCKET socket
        );

        [[nodiscard]]
        TcpConnection& FindConnectionByPlayerId(
            std::uint32_t playerId
        );

        [[nodiscard]]
        const TcpConnection& FindConnectionByPlayerId(
            std::uint32_t playerId
        ) const;

        void SendPacketToPlayer(
            std::uint32_t playerId,
            tdr::protocol::MessageType type,
            const std::vector<std::uint8_t>& payload
        );

        [[nodiscard]]
        tdr::protocol::RoomStateSnapshot
            BuildRoomStateSnapshot(
                const std::string& roomId
            ) const;

        void BroadcastRoomState(
            const std::string& roomId
        );

        void RemoveSession(
            std::uint32_t playerId
        );

        void BroadcastGameStarted(
            const std::string& roomId
        );

        void NotifyRoomClosed(
            const std::string& roomId
        );

        void RemoveConnection(
            SOCKET socket
        );

        [[nodiscard]] std::vector<SOCKET> RemoveTimedOutConnections(
            TcpClientSession::TimePoint now,
            std::chrono::milliseconds timeout
        );

        [[nodiscard]]
        std::size_t SessionCount() const noexcept;

        [[nodiscard]]
        std::size_t ConnectionCount() const noexcept;

        [[nodiscard]]
        tdr::room::RoomManager& Rooms() noexcept;

    private:
        struct ConnectedClient final
        {
            TcpConnection connection;
            std::uint32_t playerId = 0;

            ConnectedClient(
                TcpConnection ownedConnection,
                std::uint32_t assignedPlayerId
            );
        };

        tdr::room::PlayerIdAllocator playerIdAllocator_;
        tdr::room::SessionTokenGenerator tokenGenerator_;
        tdr::room::RoomManager roomManager_;

        std::unordered_map<
            std::uint32_t,
            std::unique_ptr<TcpClientSession>
        > sessions_;

        std::unordered_map<
            SOCKET,
            ConnectedClient
        > connections_;
    };
}
