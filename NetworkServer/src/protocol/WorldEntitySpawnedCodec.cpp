#include "protocol/WorldEntitySpawnedCodec.h"

#include <algorithm>
#include <stdexcept>

namespace
{
    void ValidateSpawnRecord(
        const tdr::protocol::WorldEntityRecord& record
    )
    {
        using namespace tdr::protocol;

        if (record.lifecycle !=
            WorldEntityLifecycle::Spawn)
        {
            throw std::invalid_argument(
                "World entity spawn record must use "
                "the Spawn lifecycle."
            );
        }

        if ((static_cast<std::uint16_t>(record.flags) &
            static_cast<std::uint16_t>(
                WorldEntityFlags::Dead)) != 0U)
        {
            throw std::invalid_argument(
                "World entity spawn record cannot be dead."
            );
        }
    }
}

namespace tdr::protocol
{
    std::vector<std::uint8_t>
        WorldEntitySpawnedCodec::Encode(
            const WorldEntityRecord& record
        )
    {
        ValidateSpawnRecord(record);

        WorldStateSnapshotPayload snapshot{};
        snapshot.entities.push_back(record);

        const auto snapshotPayload =
            WorldStateSnapshotCodec::Encode(snapshot);

        return std::vector<std::uint8_t>(
            snapshotPayload.begin() +
                static_cast<std::ptrdiff_t>(
                    kWorldStateSnapshotPrefixSize),
            snapshotPayload.end()
        );
    }

    WorldEntityRecord
        WorldEntitySpawnedCodec::Decode(
            const std::uint8_t* const data,
            const std::size_t size
        )
    {
        if (data == nullptr)
        {
            throw std::invalid_argument(
                "World entity spawn payload is null."
            );
        }

        if (size != kWorldEntitySpawnedPayloadSize)
        {
            throw std::invalid_argument(
                "World entity spawn payload must contain "
                "exactly one entity record."
            );
        }

        std::vector<std::uint8_t> snapshotPayload(
            kWorldStateSnapshotPrefixSize + size,
            0U
        );

        snapshotPayload[3] = 1U;

        std::copy(
            data,
            data + size,
            snapshotPayload.begin() +
                static_cast<std::ptrdiff_t>(
                    kWorldStateSnapshotPrefixSize)
        );

        const auto snapshot =
            WorldStateSnapshotCodec::Decode(
                snapshotPayload.data(),
                snapshotPayload.size());

        const auto record =
            snapshot.entities.front();

        ValidateSpawnRecord(record);
        return record;
    }
}
