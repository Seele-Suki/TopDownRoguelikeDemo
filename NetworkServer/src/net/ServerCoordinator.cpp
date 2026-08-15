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
        }

        session.LeaveRoom();

        if (!closedRoomId.empty())
        {
            for (auto& sessionEntry : sessions_)
            {
                const std::uint32_t otherPlayerId =
                    sessionEntry.first;

                if (otherPlayerId == playerId)
                {
                    continue;
                }

                sessionEntry.second->InvalidateRoom(
                    closedRoomId
                );
            }
        }

        connections_.erase(
            connectionIterator
        );

        RemoveSession(
            playerId
        );
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