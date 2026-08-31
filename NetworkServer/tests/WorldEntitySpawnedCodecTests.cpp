#include "protocol/WorldEntitySpawnedCodec.h"
#include "protocol/WorldEntityRemovedCodec.h"

#include <cstdint>
#include <exception>
#include <iostream>
#include <stdexcept>

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

    tdr::protocol::WorldEntityRecord CreateEnemySpawn()
    {
        using namespace tdr::protocol;

        WorldEntityRecord record{};
        record.entityId = 0x10000001U;
        record.entityType = WorldEntityType::Enemy;
        record.lifecycle = WorldEntityLifecycle::Spawn;
        record.flags = WorldEntityFlags::Active;
        record.positionX = 2.5F;
        record.positionY = -4.0F;
        record.rotationDegrees = 90.0F;
        record.currentHealth = 3U;
        record.maxHealth = 3U;
        record.enemyArchetype =
            NetworkEnemyArchetype::Fast;

        return record;
    }

    void EnemySpawnUsesOneStableRecord()
    {
        using namespace tdr::protocol;

        const auto encoded =
            WorldEntitySpawnedCodec::Encode(
                CreateEnemySpawn());

        Require(
            encoded.size() == kWorldEntityRecordSize,
            "Enemy spawn payload is not one 32-byte record."
        );

        const auto decoded =
            WorldEntitySpawnedCodec::Decode(
                encoded.data(),
                encoded.size());

        Require(
            decoded.entityId == 0x10000001U &&
            decoded.entityType == WorldEntityType::Enemy &&
            decoded.lifecycle == WorldEntityLifecycle::Spawn &&
            decoded.currentHealth == 3U &&
            decoded.maxHealth == 3U &&
            decoded.enemyArchetype ==
                NetworkEnemyArchetype::Fast,
            "Enemy spawn record did not round-trip."
        );
    }

    void EnemySpawnRejectsNonSpawnLifecycle()
    {
        using namespace tdr::protocol;

        auto record = CreateEnemySpawn();
        record.lifecycle = WorldEntityLifecycle::Update;

        RequireInvalidArgument(
            [&record]()
            {
                static_cast<void>(
                    WorldEntitySpawnedCodec::Encode(record));
            },
            "Enemy spawn accepted a non-Spawn lifecycle."
        );
    }

    void EnemySpawnRejectsDeadRecord()
    {
        using namespace tdr::protocol;

        auto record = CreateEnemySpawn();
        record.flags = WorldEntityFlags::Dead;
        record.currentHealth = 0U;

        RequireInvalidArgument(
            [&record]()
            {
                static_cast<void>(
                    WorldEntitySpawnedCodec::Encode(record));
            },
            "Enemy spawn accepted a dead record."
        );
    }

    void EnemyRemovalUsesStableEightBytePayload()
    {
        using namespace tdr::protocol;

        const WorldEntityRemovedPayload expected{
            0x1000002AU,
            WorldEntityType::Enemy,
            WorldEntityRemovalReason::Died
        };

        const auto encoded =
            WorldEntityRemovedCodec::Encode(expected);

        Require(
            encoded == std::vector<std::uint8_t>{
                0x10U, 0x00U, 0x00U, 0x2AU,
                0x02U, 0x01U, 0x00U, 0x00U
            },
            "Enemy removal wire layout changed."
        );

        const auto decoded =
            WorldEntityRemovedCodec::Decode(
                encoded.data(),
                encoded.size());

        Require(
            decoded.entityId == expected.entityId &&
            decoded.entityType == expected.entityType &&
            decoded.reason == expected.reason,
            "Enemy removal payload did not round-trip."
        );
    }

    void EnemyRemovalRejectsReservedBytes()
    {
        using namespace tdr::protocol;

        auto encoded =
            WorldEntityRemovedCodec::Encode({
                1U,
                WorldEntityType::Enemy,
                WorldEntityRemovalReason::Died
            });

        encoded[6] = 1U;

        RequireInvalidArgument(
            [&encoded]()
            {
                static_cast<void>(
                    WorldEntityRemovedCodec::Decode(
                        encoded.data(),
                        encoded.size()));
            },
            "Enemy removal accepted a reserved byte."
        );
    }
}

int main()
{
    try
    {
        EnemySpawnUsesOneStableRecord();
        EnemySpawnRejectsNonSpawnLifecycle();
        EnemySpawnRejectsDeadRecord();
        EnemyRemovalUsesStableEightBytePayload();
        EnemyRemovalRejectsReservedBytes();
        std::cout << "WorldEntitySpawnedCodec tests passed.\n";
        return 0;
    }
    catch (const std::exception& exception)
    {
        std::cerr << exception.what() << '\n';
        return 1;
    }
}
