#pragma once

#include <cstddef>
#include <cstdint>
#include <vector>

namespace tdr::protocol
{
    inline constexpr std::size_t
        kPlayerShotgunEventPayloadSize = 36U;

    inline constexpr std::uint32_t
        kMaxShotgunProjectileCount = 32U;

    struct PlayerShotgunEvent final
    {
        std::uint32_t playerId = 0U;
        std::uint32_t volleySequence = 0U;
        float originX = 0.0F;
        float originY = 0.0F;
        float centerDirectionX = 0.0F;
        float centerDirectionY = 0.0F;
        std::uint32_t projectileCount = 0U;
        float spreadAngle = 0.0F;
        float effectiveCooldown = 0.0F;
    };

    class PlayerShotgunEventCodec final
    {
    public:
        [[nodiscard]]
        static std::vector<std::uint8_t> Encode(
            const PlayerShotgunEvent& shotgunEvent
        );

        [[nodiscard]]
        static PlayerShotgunEvent Decode(
            const std::uint8_t* data,
            std::size_t size
        );
    };
}