#pragma once

#include <cstdint>
#include <vector>

namespace tdr::net
{
    class TcpClientSession;

    struct ForwardedWorldEntitySpawn final
    {
        std::uint32_t targetPlayerId = 0U;
        std::vector<std::uint8_t> payload;
    };

    class WorldEntitySpawnForwarder final
    {
    public:
        [[nodiscard]]
        static ForwardedWorldEntitySpawn Forward(
            const TcpClientSession& sender,
            const std::vector<std::uint8_t>& payload
        );
    };
}
