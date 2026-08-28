#pragma once

#include <cstddef>
#include <cstdint>
#include <vector>

namespace tdr::protocol
{
    inline constexpr std::size_t
        kPlayerShotEventPayloadSize = 24U;

    struct PlayerShotEvent final
    {
        std::uint32_t playerId = 0U;
        std::uint32_t shotSequence = 0U;
        float originX = 0.0F;
        float originY = 0.0F;
        float directionX = 0.0F;
        float directionY = 0.0F;
    };

    class PlayerShotEventCodec final
    {
    public:
        [[nodiscard]]
        static std::vector<std::uint8_t> Encode(
            const PlayerShotEvent& shotEvent
        );

        [[nodiscard]]
        static PlayerShotEvent Decode(
            const std::uint8_t* data,
            std::size_t size
        );
    };
}