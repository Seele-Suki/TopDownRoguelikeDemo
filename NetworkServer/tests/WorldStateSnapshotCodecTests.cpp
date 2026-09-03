#include "protocol/WorldStateSnapshotCodec.h"

#include <cstdint>
#include <exception>
#include <iostream>
#include <stdexcept>
#include <vector>

namespace
{
    void Require(
        const bool condition,
        const char* const message
    )
    {
        if (!condition)
        {
            throw std::runtime_error(message);
        }
    }

    template<typename Action>
    void RequireInvalidArgument(
        Action&& action,
        const char* const message
    )
    {
        try
        {
            action();
        }
        catch (const std::invalid_argument&)
        {
            return;
        }

        throw std::runtime_error(message);
    }

    void WorldStateSnapshotUsesStableWireLayout()
    {
        using namespace tdr::protocol;

        WorldEntityRecord record{};
        record.entityId = 0x01020304U;
        record.entityType = WorldEntityType::Player;
        record.lifecycle = WorldEntityLifecycle::Snapshot;
        record.flags = WorldEntityFlags::Active;
        record.positionX = 1.0F;
        record.positionY = -2.0F;
        record.rotationDegrees = 90.0F;
        record.currentHealth = 25U;
        record.maxHealth = 100U;
        record.bossPhase = 0U;

        WorldStateSnapshotPayload snapshot{};
        snapshot.entities.push_back(record);

        const auto encoded =
            WorldStateSnapshotCodec::Encode(snapshot);

        Require(
            encoded.size() ==
                kWorldStateSnapshotPrefixSize +
                kWorldEntityRecordSize,
            "World snapshot size is incorrect."
        );

        Require(
            encoded[0] == 0x00U &&
            encoded[1] == 0x00U &&
            encoded[2] == 0x00U &&
            encoded[3] == 0x01U,
            "World entity count is not network ordered."
        );

        Require(
            encoded[8] == 0x01U &&
            encoded[9] == 0x00U &&
            encoded[10] == 0x00U &&
            encoded[11] == 0x01U,
            "World entity identity fields are incorrect."
        );

        Require(
            encoded[24] == 0x00U &&
            encoded[25] == 0x19U &&
            encoded[26] == 0x00U &&
            encoded[27] == 0x64U,
            "World health fields are not network ordered."
        );
    }

    void WorldStateSnapshotRoundTripsMultipleEntities()
    {
        using namespace tdr::protocol;

        WorldStateSnapshotPayload snapshot{};

        WorldEntityRecord player{};
        player.entityId = 1U;
        player.entityType = WorldEntityType::Player;
        player.lifecycle = WorldEntityLifecycle::Snapshot;
        player.flags = WorldEntityFlags::Active;
        player.currentHealth = 10U;
        player.maxHealth = 10U;

        WorldEntityRecord boss{};
        boss.entityId = 2U;
        boss.entityType = WorldEntityType::Boss;
        boss.lifecycle = WorldEntityLifecycle::Snapshot;
        boss.flags = WorldEntityFlags::Active;
        boss.currentHealth = 0U;
        boss.maxHealth = 200U;
        boss.bossPhase = 2U;
        boss.flags = WorldEntityFlags::Dead;

        snapshot.entities = { boss, player };

        const auto encoded =
            WorldStateSnapshotCodec::Encode(snapshot);
        const auto decoded =
            WorldStateSnapshotCodec::Decode(
                encoded.data(),
                encoded.size());

        Require(
            decoded.entities.size() == 2U,
            "World snapshot entity count changed."
        );

        Require(
            decoded.entities[0].entityId == 1U &&
            decoded.entities[1].entityId == 2U,
            "World snapshot entities were not ordered by ID."
        );

        Require(
            decoded.entities[1].bossPhase == 2U &&
            decoded.entities[1].currentHealth == 0U &&
            decoded.entities[1].maxHealth == 200U,
            "Boss state did not round-trip."
        );
    }

    void WorldStateSnapshotRejectsInvalidHealth()
    {
        using namespace tdr::protocol;

        WorldEntityRecord record{};
        record.entityId = 1U;
        record.entityType = WorldEntityType::Enemy;
        record.lifecycle = WorldEntityLifecycle::Snapshot;
        record.flags = WorldEntityFlags::Active;
        record.currentHealth = 11U;
        record.maxHealth = 10U;
        record.enemyArchetype =
            NetworkEnemyArchetype::Basic;

        WorldStateSnapshotPayload snapshot{};
        snapshot.entities.push_back(record);

        RequireInvalidArgument(
            [&snapshot]()
            {
                static_cast<void>(
                    WorldStateSnapshotCodec::Encode(snapshot));
            },
            "Invalid world health was accepted."
        );
    }

    void WorldStateSnapshotPreservesEnemyArchetype()
    {
        using namespace tdr::protocol;

        WorldEntityRecord enemy{};
        enemy.entityId = 0x10000001U;
        enemy.entityType = WorldEntityType::Enemy;
        enemy.lifecycle = WorldEntityLifecycle::Snapshot;
        enemy.flags = WorldEntityFlags::Active;
        enemy.currentHealth = 1U;
        enemy.maxHealth = 1U;
        enemy.enemyArchetype =
            NetworkEnemyArchetype::Fast;

        WorldStateSnapshotPayload snapshot{};
        snapshot.entities.push_back(enemy);

        const auto encoded =
            WorldStateSnapshotCodec::Encode(snapshot);

        Require(
            encoded[
                kWorldStateSnapshotPrefixSize + 25U] ==
                static_cast<std::uint8_t>(
                    NetworkEnemyArchetype::Fast),
            "Enemy archetype is not at record offset 25."
        );

        for (std::size_t offset = 28U;
            offset < kWorldEntityRecordSize;
            ++offset)
        {
            Require(
                encoded[
                    kWorldStateSnapshotPrefixSize + offset] ==
                    0U,
                "World entity reserved byte is not zero."
            );
        }

        const auto decoded =
            WorldStateSnapshotCodec::Decode(
                encoded.data(),
                encoded.size());

        Require(
            decoded.entities[0].enemyArchetype ==
                NetworkEnemyArchetype::Fast,
            "Enemy archetype did not round-trip."
        );
    }

    void WorldStateSnapshotPreservesExperienceOrbAmount()
    {
        using namespace tdr::protocol;

        WorldEntityRecord orb{};
        orb.entityId = 0x40000001U;
        orb.entityType = WorldEntityType::ExperienceOrb;
        orb.lifecycle = WorldEntityLifecycle::Spawn;
        orb.flags = WorldEntityFlags::Active;
        orb.experienceAmount = 17U;

        WorldStateSnapshotPayload snapshot{};
        snapshot.entities.push_back(orb);

        const auto encoded =
            WorldStateSnapshotCodec::Encode(snapshot);
        const auto decoded =
            WorldStateSnapshotCodec::Decode(
                encoded.data(),
                encoded.size());

        Require(
            decoded.entities[0].experienceAmount == 17U,
            "Experience orb amount did not round-trip."
        );
    }

    void WorldStateSnapshotPreservesBossProjectileMetadata()
    {
        using namespace tdr::protocol;

        WorldEntityRecord projectile{};
        projectile.entityId = 0x30000001U;
        projectile.entityType = WorldEntityType::BossProjectile;
        projectile.lifecycle = WorldEntityLifecycle::Snapshot;
        projectile.flags = WorldEntityFlags::Active;
        projectile.directionX = 0.7071F;
        projectile.directionY = 0.7071F;
        projectile.projectileSpeed = 8.0F;
        projectile.projectileDamage = 12U;
        projectile.projectileSequence = 7U;

        WorldStateSnapshotPayload snapshot{};
        snapshot.entities.push_back(projectile);
        const auto encoded = WorldStateSnapshotCodec::Encode(snapshot);
        const auto decoded = WorldStateSnapshotCodec::Decode(
            encoded.data(),
            encoded.size());

        Require(decoded.entities[0].projectileDamage == 12U,
            "Boss projectile damage did not round-trip.");
        Require(decoded.entities[0].projectileSequence == 7U,
            "Boss projectile sequence did not round-trip.");
    }

    void WorldStateSnapshotValidatesBossPhase()
    {
        using namespace tdr::protocol;

        WorldEntityRecord boss{};
        boss.entityId = 7U;
        boss.entityType = WorldEntityType::Boss;
        boss.lifecycle = WorldEntityLifecycle::Snapshot;
        boss.flags = WorldEntityFlags::Active;
        boss.currentHealth = 10U;
        boss.maxHealth = 10U;

        WorldStateSnapshotPayload snapshot{};
        snapshot.entities.push_back(boss);

        for (const std::uint8_t phase : { 1U, 2U })
        {
            snapshot.entities[0].bossPhase = phase;
            const auto encoded = WorldStateSnapshotCodec::Encode(snapshot);
            const auto decoded = WorldStateSnapshotCodec::Decode(
                encoded.data(), encoded.size());
            Require(decoded.entities[0].bossPhase == phase,
                "Supported Boss phase did not round-trip.");
        }

        for (const std::uint8_t phase : { 0U, 3U })
        {
            snapshot.entities[0].bossPhase = phase;
            RequireInvalidArgument(
                [&snapshot]()
                {
                    static_cast<void>(
                        WorldStateSnapshotCodec::Encode(snapshot));
                },
                "Unsupported Boss phase was accepted.");
        }
    }

    void WorldStateSnapshotRejectsBossPhaseOnNonBoss()
    {
        using namespace tdr::protocol;

        WorldEntityRecord enemy{};
        enemy.entityId = 8U;
        enemy.entityType = WorldEntityType::Enemy;
        enemy.lifecycle = WorldEntityLifecycle::Snapshot;
        enemy.flags = WorldEntityFlags::Active;
        enemy.currentHealth = 10U;
        enemy.maxHealth = 10U;
        enemy.enemyArchetype = NetworkEnemyArchetype::Basic;
        enemy.bossPhase = 1U;

        WorldStateSnapshotPayload snapshot{};
        snapshot.entities.push_back(enemy);

        RequireInvalidArgument(
            [&snapshot]()
            {
                static_cast<void>(
                    WorldStateSnapshotCodec::Encode(snapshot));
            },
            "Non-Boss entity accepted a Boss phase.");
    }
}

int main()
{
    try
    {
        WorldStateSnapshotUsesStableWireLayout();
        WorldStateSnapshotRoundTripsMultipleEntities();
        WorldStateSnapshotRejectsInvalidHealth();
        WorldStateSnapshotPreservesEnemyArchetype();
        WorldStateSnapshotPreservesExperienceOrbAmount();
        WorldStateSnapshotPreservesBossProjectileMetadata();
        WorldStateSnapshotValidatesBossPhase();
        WorldStateSnapshotRejectsBossPhaseOnNonBoss();
        std::cout << "WorldStateSnapshotCodec tests passed.\n";
        return 0;
    }
    catch (const std::exception& exception)
    {
        std::cerr << exception.what() << '\n';
        return 1;
    }
}
