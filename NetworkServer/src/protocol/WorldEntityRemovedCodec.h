#pragma once

#include "protocol/WorldStateSnapshotCodec.h"

#include <cstddef>
#include <cstdint>
#include <vector>

namespace tdr::protocol
{
    inline constexpr std::size_t
        kWorldEntityRemovedPayloadSize = 8U;

    enum class WorldEntityRemovalReason : std::uint8_t
    {
        Invalid = 0,
        Died = 1,
        Cleared = 2,
        Despawned = 3
    };

    struct WorldEntityRemovedPayload final
    {
        std::uint32_t entityId = 0U;
        WorldEntityType entityType =
            static_cast<WorldEntityType>(0U);
        WorldEntityRemovalReason reason =
            WorldEntityRemovalReason::Invalid;
    };

    class WorldEntityRemovedCodec final
    {
    public:
        [[nodiscard]]
        static std::vector<std::uint8_t> Encode(
            const WorldEntityRemovedPayload& payload
        );

        [[nodiscard]]
        static WorldEntityRemovedPayload Decode(
            const std::uint8_t* data,
            std::size_t size
        );
    };
}
