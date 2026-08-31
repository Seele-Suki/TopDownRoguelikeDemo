#pragma once

#include <array>
#include <cstddef>
#include <cstdint>
#include <vector>

namespace tdr::protocol
{
    inline constexpr std::size_t
        kWorldStateSnapshotPrefixSize = 4U;

    inline constexpr std::size_t
        kWorldEntityRecordSize = 32U;

    inline constexpr std::size_t
        kMaxWorldEntityRecords = 64U;

    enum class WorldEntityType : std::uint8_t
    {
        Player = 1U,
        Enemy = 2U,
        Boss = 3U,
        ExperienceOrb = 4U
    };

    enum class WorldEntityLifecycle : std::uint8_t
    {
        Snapshot = 0U,
        Spawn = 1U,
        Update = 2U,
        Dead = 3U,
        Removed = 4U
    };

    enum class NetworkEnemyArchetype : std::uint8_t
    {
        Invalid = 0U,
        Basic = 1U,
        Fast = 2U
    };

    enum class WorldEntityFlags : std::uint16_t
    {
        None = 0U,
        Active = 1U << 0U,
        Dead = 1U << 1U
    };

    struct WorldEntityRecord final
    {
        std::uint32_t entityId = 0U;
        WorldEntityType entityType = WorldEntityType::Player;
        WorldEntityLifecycle lifecycle =
            WorldEntityLifecycle::Snapshot;
        WorldEntityFlags flags = WorldEntityFlags::None;
        float positionX = 0.0F;
        float positionY = 0.0F;
        float rotationDegrees = 0.0F;
        std::uint16_t currentHealth = 0U;
        std::uint16_t maxHealth = 0U;
        std::uint8_t bossPhase = 0U;
        NetworkEnemyArchetype enemyArchetype =
            NetworkEnemyArchetype::Invalid;
    };

    struct WorldStateSnapshotPayload final
    {
        std::vector<WorldEntityRecord> entities;
    };

    class WorldStateSnapshotCodec final
    {
    public:
        [[nodiscard]]
        static std::vector<std::uint8_t> Encode(
            const WorldStateSnapshotPayload& snapshot
        );

        [[nodiscard]]
        static WorldStateSnapshotPayload Decode(
            const std::uint8_t* data,
            std::size_t size
        );
    };
}
