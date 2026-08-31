#pragma once

#include <cstdint>
#include <vector>

namespace tdr::net
{
    class TcpClientSession;

    struct ForwardedWorldEntityRemoval final
    {
        std::uint32_t targetPlayerId = 0U;
        std::vector<std::uint8_t> payload;
    };

    class WorldEntityRemovalForwarder final
    {
    public:
        [[nodiscard]]
        static ForwardedWorldEntityRemoval Forward(
            const TcpClientSession& sender,
            const std::vector<std::uint8_t>& payload
        );
    };
}
