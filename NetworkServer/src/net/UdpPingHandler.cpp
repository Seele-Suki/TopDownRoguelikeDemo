#include "net/UdpPingHandler.h"

#include "protocol/UdpPacketCodec.h"

#include <stdexcept>

namespace tdr::net
{
    UdpPingHandler::UdpPingHandler(
        ServerCoordinator& coordinator
    ) noexcept
        : coordinator_(coordinator)
    {
    }

    std::vector<std::uint8_t>
        UdpPingHandler::Handle(
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

        if (request.header.type
                != tdr::protocol::MessageType::UdpPing)
        {
            throw std::invalid_argument(
                "Only UdpPing can be handled by "
                "UdpPingHandler."
            );
        }

        if (!request.payload.empty())
        {
            throw std::invalid_argument(
                "UDP Ping payload must be empty."
            );
        }

        auto& session =
            coordinator_.FindSessionForUdp(
                request.header
            );

        if (!session.HasUdpEndpoint()
            || !session.MatchesUdpEndpoint(
                sourceAddress))
        {
            throw std::invalid_argument(
                "UDP Ping source is not bound to "
                "the requested session."
            );
        }

        if (!session.AcceptUdpSequence(
            request.header.sequence))
        {
            throw std::invalid_argument(
                "UDP Ping sequence is duplicate "
                "or expired."
            );
        }

        auto responseHeader =
            request.header;

        responseHeader.type =
            tdr::protocol::MessageType::UdpPong;

        return tdr::protocol::UdpPacketCodec::Encode(
            responseHeader,
            {}
        );
    }
}
