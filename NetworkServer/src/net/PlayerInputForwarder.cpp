#include "net/PlayerInputForwarder.h"

#include "net/ServerCoordinator.h"
#include "protocol/PlayerInputCodec.h"
#include "protocol/UdpPacketCodec.h"

#include <stdexcept>

namespace tdr::net
{
    PlayerInputForwarder::PlayerInputForwarder(
        ServerCoordinator& coordinator
    ) noexcept
        : coordinator_(coordinator)
    {
    }

    ForwardedUdpDatagram
        PlayerInputForwarder::Forward(
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
            tdr::protocol::MessageType::PlayerInput)
        {
            throw std::invalid_argument(
                "Only PlayerInput can be handled by "
                "PlayerInputForwarder."
            );
        }

        static_cast<void>(
            tdr::protocol::PlayerInputCodec::Decode(
                request.payload.data(),
                request.payload.size()
            )
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
                "PlayerInput source is not bound "
                "to the requested session."
            );
        }

        if (!sender.HasRoom())
        {
            throw std::runtime_error(
                "PlayerInput sender is not in a room."
            );
        }

        const auto& room =
            sender.CurrentRoom();

        if (room.Status() !=
            tdr::room::RoomStatus::Started)
        {
            throw std::runtime_error(
                "PlayerInput cannot be forwarded "
                "before the room starts."
            );
        }

        if (sender.PlayerId() ==
            room.HostPlayerId())
        {
            throw std::invalid_argument(
                "The room host cannot send "
                "PlayerInput for forwarding."
            );
        }

        auto& host =
            coordinator_.FindSession(
                room.HostPlayerId()
            );

        if (!host.HasUdpEndpoint())
        {
            throw std::runtime_error(
                "The room host has no bound "
                "UDP endpoint."
            );
        }

        if (!sender.AcceptPlayerInputSequence(
            request.header.sequence))
        {
            throw std::invalid_argument(
                "PlayerInput sequence is duplicate "
                "or expired."
            );
        }

        auto forwardedHeader =
            request.header;

        forwardedHeader.sessionToken =
            host.SessionTokenBytes();

        return ForwardedUdpDatagram{
            tdr::protocol::UdpPacketCodec::Encode(
                forwardedHeader,
                request.payload
            ),
            host.UdpEndpointAddress()
        };
    }
}