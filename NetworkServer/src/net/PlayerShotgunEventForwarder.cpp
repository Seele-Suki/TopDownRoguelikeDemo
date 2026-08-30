#include "net/PlayerShotgunEventForwarder.h"

#include "net/ServerCoordinator.h"
#include "protocol/PlayerShotgunEventCodec.h"
#include "protocol/UdpPacketCodec.h"

#include <stdexcept>

namespace tdr::net
{
    PlayerShotgunEventForwarder::
        PlayerShotgunEventForwarder(
            ServerCoordinator& coordinator
        ) noexcept
        : coordinator_(coordinator)
    {
    }

    ForwardedPlayerShotgunDatagram
        PlayerShotgunEventForwarder::Forward(
            const std::uint8_t* const data,
            const std::size_t size,
            const sockaddr_in6& sourceAddress
        )
    {
        const auto request =
            tdr::protocol::UdpPacketCodec::Decode(
                data,
                size);

        if (request.header.type !=
            tdr::protocol::MessageType::PlayerShotgunEvent)
        {
            throw std::invalid_argument(
                "Only PlayerShotgunEvent can be handled "
                "by PlayerShotgunEventForwarder.");
        }

        const auto shotgunEvent =
            tdr::protocol::PlayerShotgunEventCodec::Decode(
                request.payload.data(),
                request.payload.size());

        auto& sender =
            coordinator_.FindSessionForUdp(
                request.header);

        if (!sender.HasUdpEndpoint() ||
            !sender.MatchesUdpEndpoint(
                sourceAddress))
        {
            throw std::invalid_argument(
                "PlayerShotgunEvent source is not bound "
                "to the requested session.");
        }

        if (!sender.HasRoom())
        {
            throw std::runtime_error(
                "PlayerShotgunEvent sender is not in a room.");
        }

        const auto& room =
            sender.CurrentRoom();

        if (room.Status() !=
            tdr::room::RoomStatus::Started)
        {
            throw std::runtime_error(
                "PlayerShotgunEvent cannot be forwarded "
                "before the room starts.");
        }

        if (sender.PlayerId() !=
            room.HostPlayerId())
        {
            throw std::invalid_argument(
                "Only the room host can send "
                "PlayerShotgunEvent.");
        }

        if (shotgunEvent.playerId !=
            room.HostPlayerId() &&
            shotgunEvent.playerId !=
            sender.PlayerId())
        {
            bool belongsToRoom = false;

            for (std::size_t index = 0U;
                index < room.PlayerCount();
                ++index)
            {
                if (room.PlayerAt(index).playerId ==
                    shotgunEvent.playerId)
                {
                    belongsToRoom = true;
                    break;
                }
            }

            if (!belongsToRoom)
            {
                throw std::invalid_argument(
                    "PlayerShotgunEvent player ID does not "
                    "belong to the current room.");
            }
        }

        bool payloadPlayerBelongsToRoom = false;

        for (std::size_t index = 0U;
            index < room.PlayerCount();
            ++index)
        {
            if (room.PlayerAt(index).playerId ==
                shotgunEvent.playerId)
            {
                payloadPlayerBelongsToRoom = true;
                break;
            }
        }

        if (!payloadPlayerBelongsToRoom)
        {
            throw std::invalid_argument(
                "PlayerShotgunEvent player ID does not "
                "belong to the current room.");
        }

        std::uint32_t guestPlayerId = 0U;

        for (std::size_t index = 0U;
            index < room.PlayerCount();
            ++index)
        {
            const auto candidateId =
                room.PlayerAt(index).playerId;

            if (candidateId != room.HostPlayerId())
            {
                guestPlayerId = candidateId;
                break;
            }
        }

        if (guestPlayerId == 0U)
        {
            throw std::runtime_error(
                "The room has no guest player.");
        }

        auto& guest =
            coordinator_.FindSession(guestPlayerId);

        if (!guest.HasUdpEndpoint())
        {
            throw std::runtime_error(
                "The guest has no bound UDP endpoint.");
        }

        if (!sender.AcceptPlayerInputSequence(
            request.header.sequence))
        {
            throw std::invalid_argument(
                "PlayerShotgunEvent sequence is duplicate "
                "or expired.");
        }

        auto forwardedHeader = request.header;

        forwardedHeader.sessionToken =
            guest.SessionTokenBytes();

        return ForwardedPlayerShotgunDatagram{
            tdr::protocol::UdpPacketCodec::Encode(
                forwardedHeader,
                request.payload),
            guest.UdpEndpointAddress()
        };
    }
}