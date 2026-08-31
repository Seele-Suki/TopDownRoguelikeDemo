#pragma once

#include "protocol/WorldStateSnapshotCodec.h"

#include <cstddef>
#include <cstdint>
#include <vector>

namespace tdr::protocol
{
    inline constexpr std::size_t
        kWorldEntitySpawnedPayloadSize =
            kWorldEntityRecordSize;

    class WorldEntitySpawnedCodec final
    {
    public:
        [[nodiscard]]
        static std::vector<std::uint8_t> Encode(
            const WorldEntityRecord& record
        );

        [[nodiscard]]
        static WorldEntityRecord Decode(
            const std::uint8_t* data,
            std::size_t size
        );
    };
}
