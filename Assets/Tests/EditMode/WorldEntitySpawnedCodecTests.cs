using System;
using NUnit.Framework;
using TopDownRoguelike.Networking.Protocol;

namespace TopDownRoguelike.Tests.EditMode
{
    public sealed class WorldEntitySpawnedCodecTests
    {
        [Test]
        public void EncodeThenDecode_UsesSingleWorldEntityRecord()
        {
            WorldEntityRecord expected =
                CreateEnemySpawnRecord();

            byte[] payload =
                WorldEntitySpawnedCodec.Encode(
                    expected);

            Assert.That(
                payload,
                Has.Length.EqualTo(
                    WorldStateSnapshotCodec.RecordSize));

            WorldEntityRecord decoded =
                WorldEntitySpawnedCodec.Decode(
                    payload);

            Assert.That(
                decoded.EntityId,
                Is.EqualTo(expected.EntityId));
            Assert.That(
                decoded.EntityType,
                Is.EqualTo(NetworkEntityType.Enemy));
            Assert.That(
                decoded.Lifecycle,
                Is.EqualTo(WorldEntityLifecycle.Spawn));
            Assert.That(
                decoded.Flags,
                Is.EqualTo(WorldEntityFlags.Active));
            Assert.That(decoded.PositionX, Is.EqualTo(2.5f));
            Assert.That(decoded.PositionY, Is.EqualTo(-3.5f));
            Assert.That(decoded.RotationDegrees, Is.EqualTo(45f));
            Assert.That(decoded.CurrentHealth, Is.EqualTo(3));
            Assert.That(decoded.MaxHealth, Is.EqualTo(3));
            Assert.That(
                decoded.EnemyArchetype,
                Is.EqualTo(NetworkEnemyArchetype.Fast));
        }

        [Test]
        public void Encode_RejectsNonSpawnLifecycle()
        {
            var record =
                new WorldEntityRecord(
                    0x10000001u,
                    NetworkEntityType.Enemy,
                    WorldEntityLifecycle.Snapshot,
                    WorldEntityFlags.Active,
                    0f,
                    0f,
                    0f,
                    3,
                    3,
                    0,
                    NetworkEnemyArchetype.Basic);

            Assert.Throws<ArgumentException>(
                () => WorldEntitySpawnedCodec.Encode(
                    record));
        }

        [Test]
        public void Decode_RejectsDeadSpawnRecord()
        {
            byte[] snapshotPayload =
                WorldStateSnapshotCodec.Encode(
                    new WorldStateSnapshotPayload(
                        new[]
                        {
                            new WorldEntityRecord(
                                0x10000001u,
                                NetworkEntityType.Enemy,
                                WorldEntityLifecycle.Spawn,
                                WorldEntityFlags.Dead,
                                0f,
                                0f,
                                0f,
                                0,
                                3,
                                0,
                                NetworkEnemyArchetype.Basic)
                        }));

            var payload =
                new byte[WorldStateSnapshotCodec.RecordSize];

            Buffer.BlockCopy(
                snapshotPayload,
                WorldStateSnapshotCodec.PrefixSize,
                payload,
                0,
                payload.Length);

            Assert.Throws<ArgumentException>(
                () => WorldEntitySpawnedCodec.Decode(
                    payload));
        }

        private static WorldEntityRecord
            CreateEnemySpawnRecord()
        {
            return new WorldEntityRecord(
                0x10000001u,
                NetworkEntityType.Enemy,
                WorldEntityLifecycle.Spawn,
                WorldEntityFlags.Active,
                2.5f,
                -3.5f,
                45f,
                3,
                3,
                0,
                NetworkEnemyArchetype.Fast);
        }
    }
}
