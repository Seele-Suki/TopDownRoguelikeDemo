#include "net/PlayerStateForwarder.h"

#include "net/ServerCoordinator.h"
#include "protocol/PlayerStateSnapshotCodec.h"
#include "protocol/UdpPacketCodec.h"

#include <stdexcept>

namespace tdr::net
{
    PlayerStateForwarder::PlayerStateForwarder(
        ServerCoordinator& coordinator
    ) noexcept
        : coordinator_(coordinator)
    {
    }

    ForwardedPlayerStateDatagram
        PlayerStateForwarder::Forward(
            const std::uint8_t* const data,
            const std::size_t size,
            const sockaddr_in6& sourceAddress
        )
    {
        const auto request =
            tdr::protocol::UdpPacketCodec::Decode(
                data,
                size
            );

        if (request.header.type !=
            tdr::protocol::MessageType::
            PlayerStateSnapshot)
        {
            throw std::invalid_argument(
                "Only PlayerStateSnapshot can be "
                "handled by PlayerStateForwarder."
            );
        }

        const auto snapshot =
            tdr::protocol::
            PlayerStateSnapshotCodec::Decode(
                request.payload.data(),
                request.payload.size()
            );

        auto& sender =
            coordinator_.FindSessionForUdp(
                request.header
            );

        if (!sender.HasUdpEndpoint() ||
            !sender.MatchesUdpEndpoint(
                sourceAddress))
        {
            throw std::invalid_argument(
                "PlayerStateSnapshot source is not "
                "bound to the requested session."
            );
        }

        if (!sender.HasRoom())
        {
            throw std::runtime_error(
                "PlayerStateSnapshot sender is not "
                "in a room."
            );
        }

        const auto& room =
            sender.CurrentRoom();

        if (room.Status() !=
            tdr::room::RoomStatus::Started)
        {
            throw std::runtime_error(
                "PlayerStateSnapshot cannot be "
                "forwarded before the room starts."
            );
        }

        if (sender.PlayerId() !=
            room.HostPlayerId())
        {
            throw std::invalid_argument(
                "Only the room host can send "
                "PlayerStateSnapshot."
            );
        }

        if (snapshot.players.size() !=
            room.PlayerCount())
        {
            throw std::invalid_argument(
                "PlayerStateSnapshot player count "
                "does not match the room."
            );
        }

        for (std::size_t roomIndex = 0U;
            roomIndex < room.PlayerCount();
            ++roomIndex)
        {
            const std::uint32_t expectedPlayerId =
                room.PlayerAt(
                    roomIndex).playerId;

            bool playerWasFound =
                false;

            for (const auto& state :
                snapshot.players)
            {
                if (state.playerId ==
                    expectedPlayerId)
                {
                    playerWasFound =
                        true;

                    break;
                }
            }

            if (!playerWasFound)
            {
                throw std::invalid_argument(
                    "PlayerStateSnapshot players do "
                    "not match the room."
                );
            }
        }

        std::uint32_t guestPlayerId = 0U;

        for (std::size_t index = 0U;
            index < room.PlayerCount();
            ++index)
        {
            const std::uint32_t candidateId =
                room.PlayerAt(index).playerId;

            if (candidateId !=
                room.HostPlayerId())
            {
                guestPlayerId =
                    candidateId;

                break;
            }
        }

        if (guestPlayerId == 0U)
        {
            throw std::runtime_error(
                "The room has no guest player."
            );
        }

        auto& guest =
            coordinator_.FindSession(
                guestPlayerId
            );

        if (!guest.HasUdpEndpoint())
        {
            throw std::runtime_error(
                "The guest has no bound UDP endpoint."
            );
        }

        if (!sender.AcceptPlayerStateSequence(
            request.header.sequence))
        {
            throw std::invalid_argument(
                "PlayerStateSnapshot sequence is "
                "duplicate or expired."
            );
        }

        auto forwardedHeader =
            request.header;

        forwardedHeader.sessionToken =
            guest.SessionTokenBytes();

        return ForwardedPlayerStateDatagram{
            tdr::protocol::UdpPacketCodec::Encode(
                forwardedHeader,
                request.payload
            ),
            guest.UdpEndpointAddress()
        };
    }
}