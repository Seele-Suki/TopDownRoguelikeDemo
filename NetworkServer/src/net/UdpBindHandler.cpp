#include "net/UdpBindHandler.h"
#include "protocol/UdpPacketCodec.h"

#include <stdexcept>

namespace tdr::net
{
    UdpBindHandler::UdpBindHandler(
        ServerCoordinator& coordinator
    ) noexcept
        : coordinator_(coordinator)
    {
    }

    std::vector<std::uint8_t>
        UdpBindHandler::Handle(
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

        if (!request.payload.empty())
        {
            throw std::invalid_argument(
                "UDP BindRequest payload must be empty."
            );
        }

        static_cast<void>(
            coordinator_.BindUdpEndpoint(
                request.header,
                sourceAddress
            )
            );

        auto responseHeader =
            request.header;

        responseHeader.type =
            tdr::protocol::MessageType::UdpBindAccepted;

        return tdr::protocol::UdpPacketCodec::Encode(
            responseHeader,
            {}
        );
    }
}