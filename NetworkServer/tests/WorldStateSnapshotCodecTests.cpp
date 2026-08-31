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

        for (std::size_t offset = 26U;
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
}

int main()
{
    try
    {
        WorldStateSnapshotUsesStableWireLayout();
        WorldStateSnapshotRoundTripsMultipleEntities();
        WorldStateSnapshotRejectsInvalidHealth();
        WorldStateSnapshotPreservesEnemyArchetype();
        std::cout << "WorldStateSnapshotCodec tests passed.\n";
        return 0;
    }
    catch (const std::exception& exception)
    {
        std::cerr << exception.what() << '\n';
        return 1;
    }
}
