#include "protocol/WorldEntityRemovedCodec.h"

#include <stdexcept>

namespace
{
    void Validate(
        const tdr::protocol::WorldEntityRemovedPayload& payload
    )
    {
        using namespace tdr::protocol;

        const auto entityType =
            static_cast<std::uint8_t>(payload.entityType);
        const auto reason =
            static_cast<std::uint8_t>(payload.reason);

        if (payload.entityId == 0U)
        {
            throw std::invalid_argument(
                "Removed entity ID must be non-zero."
            );
        }

        if (entityType < static_cast<std::uint8_t>(
                WorldEntityType::Player) ||
            entityType > static_cast<std::uint8_t>(
                WorldEntityType::BossProjectile))
        {
            throw std::invalid_argument(
                "Removed entity type is invalid."
            );
        }

        if (reason < static_cast<std::uint8_t>(
                WorldEntityRemovalReason::Died) ||
            reason > static_cast<std::uint8_t>(
                WorldEntityRemovalReason::Despawned))
        {
            throw std::invalid_argument(
                "Entity removal reason is invalid."
            );
        }
    }
}

namespace tdr::protocol
{
    std::vector<std::uint8_t>
        WorldEntityRemovedCodec::Encode(
            const WorldEntityRemovedPayload& payload
        )
    {
        Validate(payload);

        return {
            static_cast<std::uint8_t>(payload.entityId >> 24U),
            static_cast<std::uint8_t>(payload.entityId >> 16U),
            static_cast<std::uint8_t>(payload.entityId >> 8U),
            static_cast<std::uint8_t>(payload.entityId),
            static_cast<std::uint8_t>(payload.entityType),
            static_cast<std::uint8_t>(payload.reason),
            0U,
            0U
        };
    }

    WorldEntityRemovedPayload
        WorldEntityRemovedCodec::Decode(
            const std::uint8_t* const data,
            const std::size_t size
        )
    {
        if (data == nullptr)
        {
            throw std::invalid_argument(
                "World entity removal payload is null."
            );
        }

        if (size != kWorldEntityRemovedPayloadSize)
        {
            throw std::invalid_argument(
                "World entity removal payload must be 8 bytes."
            );
        }

        if (data[6] != 0U || data[7] != 0U)
        {
            throw std::invalid_argument(
                "World entity removal reserved bytes must be zero."
            );
        }

        WorldEntityRemovedPayload payload{};
        payload.entityId =
            (static_cast<std::uint32_t>(data[0]) << 24U) |
            (static_cast<std::uint32_t>(data[1]) << 16U) |
            (static_cast<std::uint32_t>(data[2]) << 8U) |
            static_cast<std::uint32_t>(data[3]);
        payload.entityType =
            static_cast<WorldEntityType>(data[4]);
        payload.reason =
            static_cast<WorldEntityRemovalReason>(data[5]);

        Validate(payload);
        return payload;
    }
}
