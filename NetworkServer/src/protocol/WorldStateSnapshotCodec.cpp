#include "protocol/WorldStateSnapshotCodec.h"

#include "protocol/NetworkByteOrder.h"

#include <algorithm>
#include <cmath>
#include <cstring>
#include <stdexcept>

namespace
{
    constexpr std::uint16_t kActiveFlag = 1U << 0U;
    constexpr std::uint16_t kDeadFlag = 1U << 1U;
    constexpr std::uint16_t kKnownFlags =
        kActiveFlag | kDeadFlag;

    constexpr std::size_t kEntityIdOffset = 0U;
    constexpr std::size_t kEntityTypeOffset = 4U;
    constexpr std::size_t kLifecycleOffset = 5U;
    constexpr std::size_t kFlagsOffset = 6U;
    constexpr std::size_t kPositionXOffset = 8U;
    constexpr std::size_t kPositionYOffset = 12U;
    constexpr std::size_t kRotationOffset = 16U;
    constexpr std::size_t kCurrentHealthOffset = 20U;
    constexpr std::size_t kMaxHealthOffset = 22U;
    constexpr std::size_t kBossPhaseOffset = 24U;
    constexpr std::size_t kEnemyArchetypeOffset = 25U;
    constexpr std::size_t kExperienceAmountOffset = 26U;

    bool IsValidEntityType(
        const tdr::protocol::WorldEntityType type
    )
    {
        switch (type)
        {
        case tdr::protocol::WorldEntityType::Player:
        case tdr::protocol::WorldEntityType::Enemy:
        case tdr::protocol::WorldEntityType::Boss:
        case tdr::protocol::WorldEntityType::ExperienceOrb:
        case tdr::protocol::WorldEntityType::BossProjectile:
            return true;
        default:
            return false;
        }
    }

    bool IsValidLifecycle(
        const tdr::protocol::WorldEntityLifecycle lifecycle
    )
    {
        const auto value =
            static_cast<std::uint8_t>(lifecycle);

        return value <= 4U;
    }

    bool IsValidEnemyArchetype(
        const tdr::protocol::NetworkEnemyArchetype archetype
    )
    {
        using tdr::protocol::NetworkEnemyArchetype;

        return archetype == NetworkEnemyArchetype::Basic ||
            archetype == NetworkEnemyArchetype::Fast;
    }

    void ValidateEntity(
        const tdr::protocol::WorldEntityRecord& entity
    )
    {
        using namespace tdr::protocol;

        if (entity.entityId == 0U)
        {
            throw std::invalid_argument(
                "World entity ID must be non-zero."
            );
        }

        if (!IsValidEntityType(entity.entityType))
        {
            throw std::invalid_argument(
                "World entity type is invalid."
            );
        }

        if (!IsValidLifecycle(entity.lifecycle))
        {
            throw std::invalid_argument(
                "World entity lifecycle is invalid."
            );
        }

        if (entity.entityType ==
            WorldEntityType::Enemy)
        {
            if (!IsValidEnemyArchetype(
                    entity.enemyArchetype))
            {
                throw std::invalid_argument(
                    "Enemy archetype is invalid."
                );
            }
        }
        else if (entity.enemyArchetype !=
            NetworkEnemyArchetype::Invalid)
        {
            throw std::invalid_argument(
                "Non-enemy entity contains an enemy archetype."
            );
        }

        const auto rawFlags =
            static_cast<std::uint16_t>(entity.flags);

        if ((rawFlags & ~kKnownFlags) != 0U)
        {
            throw std::invalid_argument(
                "World entity contains unknown flags."
            );
        }

        if (!std::isfinite(entity.positionX) ||
            !std::isfinite(entity.positionY) ||
            !std::isfinite(entity.rotationDegrees))
        {
            throw std::invalid_argument(
                "World entity contains a non-finite value."
            );
        }

        if (entity.entityType ==
            WorldEntityType::ExperienceOrb)
        {
            if (entity.currentHealth != 0U ||
                entity.maxHealth != 0U ||
                entity.bossPhase != 0U ||
                (rawFlags & kDeadFlag) != 0U)
            {
                throw std::invalid_argument(
                    "Experience orb contains combat state."
                );
            }

            if (entity.experienceAmount == 0U)
            {
                throw std::invalid_argument(
                    "Experience orb amount must be positive."
                );
            }

            return;
        }

        if (entity.entityType == WorldEntityType::BossProjectile)
        {
            if (!std::isfinite(entity.directionX) ||
                !std::isfinite(entity.directionY) ||
                !std::isfinite(entity.projectileSpeed) ||
                entity.projectileSpeed <= 0.0F ||
                entity.projectileDamage == 0U ||
                entity.projectileSequence == 0U ||
                entity.currentHealth != 0U ||
                entity.maxHealth != 0U ||
                entity.experienceAmount != 0U ||
                entity.bossPhase != 0U ||
                entity.enemyArchetype != NetworkEnemyArchetype::Invalid)
            {
                throw std::invalid_argument(
                    "Boss projectile metadata is invalid."
                );
            }
            return;
        }

        if (entity.experienceAmount != 0U)
        {
            throw std::invalid_argument(
                "Non-orb entity contains experience amount."
            );
        }

        if (entity.maxHealth == 0U ||
            entity.currentHealth > entity.maxHealth)
        {
            throw std::invalid_argument(
                "World entity contains an invalid health range."
            );
        }

        const bool isDead =
            (rawFlags & kDeadFlag) != 0U;

        if (isDead != (entity.currentHealth == 0U))
        {
            throw std::invalid_argument(
                "World entity death flag does not match health."
            );
        }

        if (entity.entityType == WorldEntityType::Boss)
        {
            if (entity.bossPhase < 1U ||
                entity.bossPhase > 2U)
            {
                throw std::invalid_argument(
                    "Boss phase is outside the supported range."
                );
            }
        }
        else if (entity.bossPhase != 0U)
        {
            throw std::invalid_argument(
                "Non-Boss entity contains a Boss phase."
            );
        }
    }

    void ValidateEntities(
        const std::vector<tdr::protocol::WorldEntityRecord>& entities,
        const bool requireAscendingOrder
    )
    {
        if (entities.empty() ||
            entities.size() >
                tdr::protocol::kMaxWorldEntityRecords)
        {
            throw std::invalid_argument(
                "World snapshot has an invalid entity count."
            );
        }

        for (std::size_t index = 0U;
            index < entities.size();
            ++index)
        {
            ValidateEntity(entities[index]);

            if (requireAscendingOrder &&
                index > 0U &&
                entities[index - 1U].entityId >=
                    entities[index].entityId)
            {
                throw std::invalid_argument(
                    "World entities are not ordered by ID."
                );
            }
        }
    }

    void AppendNetwork16(
        std::vector<std::uint8_t>& output,
        const std::uint16_t value
    )
    {
        const std::uint16_t networkValue =
            tdr::protocol::HostToNetwork16(value);

        const auto* bytes =
            reinterpret_cast<const std::uint8_t*>(
                &networkValue);

        output.insert(
            output.end(),
            bytes,
            bytes + sizeof(networkValue));
    }

    void AppendNetwork32(
        std::vector<std::uint8_t>& output,
        const std::uint32_t value
    )
    {
        const std::uint32_t networkValue =
            tdr::protocol::HostToNetwork32(value);

        const auto* bytes =
            reinterpret_cast<const std::uint8_t*>(
                &networkValue);

        output.insert(
            output.end(),
            bytes,
            bytes + sizeof(networkValue));
    }

    void AppendNetworkFloat(
        std::vector<std::uint8_t>& output,
        const float value
    )
    {
        std::uint32_t bits = 0U;
        std::memcpy(&bits, &value, sizeof(bits));
        AppendNetwork32(output, bits);
    }

    std::uint16_t ReadNetwork16(
        const std::uint8_t* data,
        std::size_t& offset
    )
    {
        std::uint16_t networkValue = 0U;
        std::memcpy(
            &networkValue,
            data + offset,
            sizeof(networkValue));
        offset += sizeof(networkValue);
        return tdr::protocol::NetworkToHost16(networkValue);
    }

    std::uint32_t ReadNetwork32(
        const std::uint8_t* data,
        std::size_t& offset
    )
    {
        std::uint32_t networkValue = 0U;
        std::memcpy(
            &networkValue,
            data + offset,
            sizeof(networkValue));
        offset += sizeof(networkValue);
        return tdr::protocol::NetworkToHost32(networkValue);
    }

    float ReadNetworkFloat(
        const std::uint8_t* data,
        std::size_t& offset
    )
    {
        const std::uint32_t bits =
            ReadNetwork32(data, offset);
        float value = 0.0F;
        std::memcpy(&value, &bits, sizeof(value));
        return value;
    }
}

namespace tdr::protocol
{
    std::vector<std::uint8_t>
        WorldStateSnapshotCodec::Encode(
            const WorldStateSnapshotPayload& snapshot
        )
    {
        std::vector<WorldEntityRecord> entities =
            snapshot.entities;

        ValidateEntities(entities, false);

        std::sort(
            entities.begin(),
            entities.end(),
            [](const WorldEntityRecord& left,
               const WorldEntityRecord& right)
            {
                return left.entityId < right.entityId;
            });

        ValidateEntities(entities, true);

        std::vector<std::uint8_t> encoded;
        encoded.reserve(
            kWorldStateSnapshotPrefixSize +
            entities.size() * kWorldEntityRecordSize);

        AppendNetwork32(
            encoded,
            static_cast<std::uint32_t>(entities.size()));

        for (const WorldEntityRecord& entity : entities)
        {
            AppendNetwork32(encoded, entity.entityId);
            encoded.push_back(
                static_cast<std::uint8_t>(entity.entityType));
            encoded.push_back(
                static_cast<std::uint8_t>(entity.lifecycle));
            AppendNetwork16(
                encoded,
                static_cast<std::uint16_t>(entity.flags));
            AppendNetworkFloat(encoded, entity.positionX);
            AppendNetworkFloat(encoded, entity.positionY);
            AppendNetworkFloat(encoded, entity.rotationDegrees);
            AppendNetwork16(encoded, entity.currentHealth);
            AppendNetwork16(encoded, entity.maxHealth);
            encoded.push_back(entity.bossPhase);
            encoded.push_back(
                static_cast<std::uint8_t>(
                    entity.enemyArchetype));
            AppendNetwork16(encoded, entity.experienceAmount);
            AppendNetworkFloat(encoded, entity.directionX);
            AppendNetworkFloat(encoded, entity.directionY);
            AppendNetworkFloat(encoded, entity.projectileSpeed);
            AppendNetwork16(encoded, entity.projectileDamage);
            AppendNetwork32(encoded, entity.projectileSequence);
            encoded.insert(encoded.end(), 2U, 0U);
        }

        return encoded;
    }

    WorldStateSnapshotPayload
        WorldStateSnapshotCodec::Decode(
            const std::uint8_t* const data,
            const std::size_t size
        )
    {
        if (data == nullptr)
        {
            throw std::invalid_argument(
                "World snapshot payload cannot be null."
            );
        }

        if (size < kWorldStateSnapshotPrefixSize)
        {
            throw std::invalid_argument(
                "World snapshot payload is truncated."
            );
        }

        std::size_t offset = 0U;
        const std::uint32_t entityCount =
            ReadNetwork32(data, offset);

        if (entityCount == 0U ||
            entityCount > kMaxWorldEntityRecords)
        {
            throw std::invalid_argument(
                "World snapshot has an invalid entity count."
            );
        }

        const std::size_t expectedSize =
            kWorldStateSnapshotPrefixSize +
            static_cast<std::size_t>(entityCount) *
            kWorldEntityRecordSize;

        if (size != expectedSize)
        {
            throw std::invalid_argument(
                "World snapshot payload has an invalid size."
            );
        }

        WorldStateSnapshotPayload snapshot{};
        snapshot.entities.reserve(entityCount);

        for (std::uint32_t index = 0U;
            index < entityCount;
            ++index)
        {
            WorldEntityRecord entity{};
            entity.entityId = ReadNetwork32(data, offset);
            entity.entityType = static_cast<WorldEntityType>(
                data[offset++]);
            entity.lifecycle = static_cast<WorldEntityLifecycle>(
                data[offset++]);
            entity.flags = static_cast<WorldEntityFlags>(
                ReadNetwork16(data, offset));
            entity.positionX = ReadNetworkFloat(data, offset);
            entity.positionY = ReadNetworkFloat(data, offset);
            entity.rotationDegrees = ReadNetworkFloat(data, offset);
            entity.currentHealth = ReadNetwork16(data, offset);
            entity.maxHealth = ReadNetwork16(data, offset);
            entity.bossPhase = data[offset++];
            entity.enemyArchetype =
                static_cast<NetworkEnemyArchetype>(
                    data[offset++]);

            entity.experienceAmount =
                ReadNetwork16(data, offset);

            entity.directionX = ReadNetworkFloat(data, offset);
            entity.directionY = ReadNetworkFloat(data, offset);
            entity.projectileSpeed = ReadNetworkFloat(data, offset);
            entity.projectileDamage = ReadNetwork16(data, offset);
            entity.projectileSequence = ReadNetwork32(data, offset);

            for (std::size_t reservedIndex = 0U;
                reservedIndex < 2U;
                ++reservedIndex)
            {
                if (data[offset + reservedIndex] != 0U)
                {
                    throw std::invalid_argument(
                        "World entity reserved byte must be zero."
                    );
                }
            }

            offset += 2U;
            snapshot.entities.push_back(entity);
        }

        ValidateEntities(snapshot.entities, true);
        return snapshot;
    }
}
