#pragma once

#include <cstddef>
#include <cstdint>
#include <vector>

namespace tdr::protocol
{
    inline constexpr std::size_t
        kPlayerStateSnapshotPrefixSize = 4U;

    inline constexpr std::size_t
        kPlayerStateRecordSize = 24U;

    inline constexpr std::size_t
        kMaxPlayerStateRecords = 4U;

    struct PlayerStateRecord final
    {
        std::uint32_t playerId = 0U;
        float positionX = 0.0F;
        float positionY = 0.0F;
        float aimX = 0.0F;
        float aimY = 0.0F;
        std::uint32_t flags = 0U;
    };

    struct PlayerStateSnapshotPayload final
    {
        std::vector<PlayerStateRecord> players;
    };

    class PlayerStateSnapshotCodec final
    {
    public:
        [[nodiscard]]
        static std::vector<std::uint8_t> Encode(
            const PlayerStateSnapshotPayload& snapshot
        );

        [[nodiscard]]
        static PlayerStateSnapshotPayload Decode(
            const std::uint8_t* data,
            std::size_t size
        );
    };
}