#pragma once

#include <cstddef>
#include <cstdint>
#include <vector>

namespace tdr::protocol
{
    inline constexpr std::size_t
        kPlayerInputPayloadSize = 24U;

    struct PlayerInputPayload final
    {
        float moveX = 0.0F;
        float moveY = 0.0F;
        float aimX = 0.0F;
        float aimY = 0.0F;
        std::uint32_t flags = 0U;
        std::uint32_t dashRequestSequence = 0U;
    };

    class PlayerInputCodec final
    {
    public:
        [[nodiscard]]
        static std::vector<std::uint8_t> Encode(
            const PlayerInputPayload& input
        );

        [[nodiscard]]
        static PlayerInputPayload Decode(
            const std::uint8_t* data,
            std::size_t size
        );
    };
}