#include "net/WorldStateForwarder.h"

#include "net/ServerCoordinator.h"
#include "protocol/UdpPacketCodec.h"
#include "protocol/WorldStateSnapshotCodec.h"

#include <stdexcept>

namespace tdr::net
{
    WorldStateForwarder::WorldStateForwarder(
        ServerCoordinator& coordinator
    ) noexcept
        : coordinator_(coordinator)
    {
    }

    ForwardedWorldStateDatagram
        WorldStateForwarder::Forward(
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
            tdr::protocol::MessageType::WorldStateSnapshot)
        {
            throw std::invalid_argument(
                "Only WorldStateSnapshot can be handled "
                "by WorldStateForwarder.");
        }

        const auto snapshot =
            tdr::protocol::WorldStateSnapshotCodec::Decode(
                request.payload.data(),
                request.payload.size());

        auto& sender =
            coordinator_.FindSessionForUdp(
                request.header);

        if (!sender.HasUdpEndpoint() ||
            !sender.MatchesUdpEndpoint(sourceAddress))
        {
            throw std::invalid_argument(
                "WorldStateSnapshot source is not bound "
                "to the requested session.");
        }

        if (!sender.HasRoom())
        {
            throw std::runtime_error(
                "WorldStateSnapshot sender is not in a room.");
        }

        const auto& room = sender.CurrentRoom();

        if (room.Status() !=
            tdr::room::RoomStatus::Started)
        {
            throw std::runtime_error(
                "WorldStateSnapshot cannot be forwarded "
                "before the room starts.");
        }

        if (sender.PlayerId() != room.HostPlayerId())
        {
            throw std::invalid_argument(
                "Only the room host can send "
                "WorldStateSnapshot.");
        }

        std::uint32_t guestPlayerId = 0U;

        for (std::size_t index = 0U;
            index < room.PlayerCount();
            ++index)
        {
            const std::uint32_t candidateId =
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

        if (!sender.AcceptPlayerStateSequence(
                request.header.sequence))
        {
            throw std::invalid_argument(
                "WorldStateSnapshot sequence is duplicate "
                "or expired.");
        }

        auto forwardedHeader = request.header;
        forwardedHeader.sessionToken =
            guest.SessionTokenBytes();

        return ForwardedWorldStateDatagram{
            tdr::protocol::UdpPacketCodec::Encode(
                forwardedHeader,
                request.payload),
            guest.UdpEndpointAddress()};
    }
}
