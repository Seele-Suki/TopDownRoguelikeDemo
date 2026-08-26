#include "net/ServerCoordinator.h"

#include <stdexcept>
#include <utility>
#include <string>

namespace tdr::net
{
    ServerCoordinator::ConnectedClient::ConnectedClient(
        TcpConnection ownedConnection,
        const std::uint32_t assignedPlayerId
    )
        : connection(std::move(ownedConnection)),
        playerId(assignedPlayerId)
    {
    }

    TcpClientSession&
        ServerCoordinator::CreateSession()
    {
        auto session =
            std::make_unique<TcpClientSession>(
                playerIdAllocator_,
                tokenGenerator_,
                roomManager_
            );

        const std::uint32_t playerId =
            session->PlayerId();

        const auto result =
            sessions_.emplace(
                playerId,
                std::move(session)
            );

        if (!result.second)
        {
            throw std::runtime_error(
                "Server generated a duplicate player ID."
            );
        }

        return *result.first->second;
    }

    TcpClientSession&
        ServerCoordinator::AttachConnection(
            TcpConnection connection
        )
    {
        if (!connection.IsValid())
        {
            throw std::invalid_argument(
                "Cannot attach an invalid TCP connection."
            );
        }

        const SOCKET socket =
            connection.NativeHandle();

        if (connections_.find(socket)
            != connections_.end())
        {
            throw std::runtime_error(
                "TCP connection is already attached."
            );
        }

        TcpClientSession& session =
            CreateSession();

        const std::uint32_t playerId =
            session.PlayerId();

        try
        {
            const auto result =
                connections_.emplace(
                    std::piecewise_construct,
                    std::forward_as_tuple(socket),
                    std::forward_as_tuple(
                        std::move(connection),
                        playerId
                    )
                );

            if (!result.second)
            {
                RemoveSession(playerId);

                throw std::runtime_error(
                    "Failed to attach TCP connection."
                );
            }
        }
        catch (...)
        {
            if (sessions_.find(playerId)
                != sessions_.end())
            {
                sessions_.erase(playerId);
            }

            throw;
        }

        return session;
    }

    TcpClientSession&
        ServerCoordinator::FindSession(
            const std::uint32_t playerId
        )
    {
        const auto iterator =
            sessions_.find(playerId);

        if (iterator == sessions_.end())
        {
            throw std::out_of_range(
                "Client session does not exist."
            );
        }

        return *iterator->second;
    }

    const TcpClientSession&
        ServerCoordinator::FindSession(
            const std::uint32_t playerId
        ) const
    {
        const auto iterator =
            sessions_.find(playerId);

        if (iterator == sessions_.end())
        {
            throw std::out_of_range(
                "Client session does not exist."
            );
        }

        return *iterator->second;
    }

    TcpClientSession&
        ServerCoordinator::FindSessionForUdp(
            const tdr::protocol::UdpMessageHeader& header
        )
    {
        TcpClientSession& session =
            FindSession(header.playerId);

        if (!session.MatchesSessionToken(
            header.sessionToken))
        {
            throw std::invalid_argument(
                "UDP session token does not match "
                "the requested player."
            );
        }

        return session;
    }

    TcpClientSession&
        ServerCoordinator::BindUdpEndpoint(
            const tdr::protocol::UdpMessageHeader& header,
            const sockaddr_in6& sourceAddress
        )
    {
        if (header.type
            != tdr::protocol::MessageType::UdpBindRequest)
        {
            throw std::invalid_argument(
                "Only UdpBindRequest can create "
                "a UDP endpoint binding."
            );
        }

        TcpClientSession& session =
            FindSessionForUdp(header);

        session.BindUdpEndpoint(
            sourceAddress
        );

        return session;
    }

    TcpClientSession&
        ServerCoordinator::FindSessionBySocket(
            const SOCKET socket
        )
    {
        const auto connectionIterator =
            connections_.find(socket);

        if (connectionIterator == connections_.end())
        {
            throw std::out_of_range(
                "TCP connection is not attached."
            );
        }

        return FindSession(
            connectionIterator->second.playerId
        );
    }

    TcpConnection&
        ServerCoordinator::FindConnectionByPlayerId(
            const std::uint32_t playerId
        )
    {
        for (auto& connectionEntry :
            connections_)
        {
            if (connectionEntry.second.playerId
                == playerId)
            {
                return connectionEntry.second.connection;
            }
        }

        throw std::out_of_range(
            "TCP connection does not exist "
            "for the player."
        );
    }

    const TcpConnection&
        ServerCoordinator::FindConnectionByPlayerId(
            const std::uint32_t playerId
        ) const
    {
        for (const auto& connectionEntry :
            connections_)
        {
            if (connectionEntry.second.playerId
                == playerId)
            {
                return connectionEntry.second.connection;
            }
        }

        throw std::out_of_range(
            "TCP connection does not exist "
            "for the player."
        );
    }

    void ServerCoordinator::SendPacketToPlayer(
        const std::uint32_t playerId,
        const tdr::protocol::MessageType type,
        const std::vector<std::uint8_t>& payload
    )
    {
        TcpConnection& connection =
            FindConnectionByPlayerId(
                playerId
            );

        const auto encodedPacket =
            tdr::protocol::PacketCodec::Encode(
                type,
                payload
            );

        connection.SendAll(
            encodedPacket.data(),
            encodedPacket.size()
        );
    }

    tdr::protocol::RoomStateSnapshot
        ServerCoordinator::BuildRoomStateSnapshot(
            const std::string& roomId
        ) const
    {
        const tdr::room::Room& room =
            roomManager_.FindRoom(
                roomId
            );

        tdr::protocol::RoomStateSnapshot
            snapshot{};

        snapshot.roomId =
            room.Id();

        snapshot.roomStatus =
            static_cast<std::uint8_t>(
                room.Status()
                );

        snapshot.difficultyId =
            static_cast<std::uint8_t>(
                room.SelectedDifficulty()
                );

        snapshot.players.reserve(
            room.PlayerCount()
        );

        for (std::size_t index = 0U;
            index < room.PlayerCount();
            ++index)
        {
            const tdr::room::RoomPlayer& player =
                room.PlayerAt(index);

            snapshot.players.push_back(
                tdr::protocol::RoomPlayerSnapshot
                {
                    player.playerId,
                    player.isHost,
                    player.isReady,
                    static_cast<std::uint8_t>(
                        player.selectedCharacter
                    ),
                    player.nickname
                }
            );
        }

        return snapshot;
    }

    void ServerCoordinator::BroadcastRoomState(
        const std::string& roomId
    )
    {
        const auto snapshot =
            BuildRoomStateSnapshot(
                roomId
            );

        const auto payload =
            tdr::protocol::
            RoomStateSnapshotCodec::Encode(
                snapshot
            );

        for (const auto& sessionEntry :
            sessions_)
        {
            const TcpClientSession& session =
                *sessionEntry.second;

            if (!session.HasRoom())
            {
                continue;
            }

            if (session.CurrentRoom().Id()
                != roomId)
            {
                continue;
            }

            SendPacketToPlayer(
                session.PlayerId(),
                tdr::protocol::MessageType::
                RoomStateSnapshot,
                payload
            );
        }
    }

    void ServerCoordinator::BroadcastGameStarted(
        const std::string& roomId
    )
    {
        const tdr::room::Room& room =
            roomManager_.FindRoom(
                roomId
            );

        if (room.Status() !=
            tdr::room::RoomStatus::Started)
        {
            throw std::runtime_error(
                "Cannot broadcast GameStarted "
                "for a waiting room."
            );
        }

        const std::vector<std::uint8_t>
            emptyPayload;

        for (const auto& sessionEntry :
            sessions_)
        {
            const TcpClientSession& session =
                *sessionEntry.second;

            if (!session.HasRoom())
            {
                continue;
            }

            if (session.CurrentRoom().Id()
                != roomId)
            {
                continue;
            }

            SendPacketToPlayer(
                session.PlayerId(),
                tdr::protocol::MessageType::
                GameStarted,
                emptyPayload
            );
        }
    }

    void ServerCoordinator::NotifyRoomClosed(
        const std::string& roomId
    )
    {
        const std::string errorMessage =
            "Room was closed by the host.";

        const std::vector<std::uint8_t>
            errorPayload(
                errorMessage.begin(),
                errorMessage.end()
            );

        const std::vector<std::uint8_t>
            emptyPayload;

        for (const auto& sessionEntry :
            sessions_)
        {
            TcpClientSession& session =
                *sessionEntry.second;

            if (!session.InvalidateRoom(
                roomId))
            {
                continue;
            }

            try
            {
                SendPacketToPlayer(
                    session.PlayerId(),
                    tdr::protocol::MessageType::
                    LeaveRoom,
                    emptyPayload
                );
            }
            catch (const std::exception&)
            {
                // A stale or already-closed client must not
                // abort cleanup for the remaining sessions.
            }

            try
            {
                SendPacketToPlayer(
                    session.PlayerId(),
                    tdr::protocol::MessageType::
                    ErrorMessage,
                    errorPayload
                );
            }
            catch (const std::exception&)
            {
                // Room invalidation has already completed.
            }
        }
    }

    void ServerCoordinator::RemoveSession(
        const std::uint32_t playerId
    )
    {
        const std::size_t removedCount =
            sessions_.erase(playerId);

        if (removedCount == 0)
        {
            throw std::out_of_range(
                "Client session does not exist."
            );
        }
    }

    void ServerCoordinator::RemoveConnection(
        const SOCKET socket
    )
    {
        const auto connectionIterator =
            connections_.find(socket);

        if (connectionIterator
            == connections_.end())
        {
            throw std::out_of_range(
                "TCP connection is not attached."
            );
        }

        const std::uint32_t playerId =
            connectionIterator->second.playerId;

        TcpClientSession& session =
            FindSession(
                playerId
            );

        std::string closedRoomId;
        std::string remainingRoomId;

        if (session.HasRoom())
        {
            const auto& room =
                session.CurrentRoom();

            if (room.HostPlayerId()
                == playerId)
            {
                closedRoomId =
                    room.Id();
            }
            else
            {
                remainingRoomId =
                    room.Id();
            }
        }

        session.LeaveRoom();

        if (!closedRoomId.empty())
        {
            NotifyRoomClosed(
                closedRoomId
            );
        }

        connections_.erase(
            connectionIterator
        );

        RemoveSession(
            playerId
        );

        if (!remainingRoomId.empty())
        {
            BroadcastRoomState(
                remainingRoomId
            );
        }
    }

    std::size_t
        ServerCoordinator::SessionCount() const noexcept
    {
        return sessions_.size();
    }

    std::size_t
        ServerCoordinator::ConnectionCount() const noexcept
    {
        return connections_.size();
    }

    tdr::room::RoomManager&
        ServerCoordinator::Rooms() noexcept
    {
        return roomManager_;
    }
}