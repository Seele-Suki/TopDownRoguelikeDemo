using System;
using System.Collections.Generic;
using NUnit.Framework;
using TopDownRoguelike.Networking.Protocol;

namespace TopDownRoguelike.Tests.EditMode
{
    public sealed class WorldStateSnapshotCodecTests
    {
        [Test]
        public void EncodeThenDecode_PreservesStableWorldFields()
        {
            var player =
                new WorldEntityRecord(
                    1u,
                    NetworkEntityType.Player,
                    WorldEntityLifecycle.Snapshot,
                    WorldEntityFlags.Active,
                    1f,
                    -2f,
                    90f,
                    25,
                    100);

            var boss =
                new WorldEntityRecord(
                    2u,
                    NetworkEntityType.Boss,
                    WorldEntityLifecycle.Snapshot,
                    WorldEntityFlags.Dead,
                    5f,
                    6f,
                    180f,
                    0,
                    200,
                    2);

            var snapshot =
                new WorldStateSnapshotPayload(
                    new List<WorldEntityRecord>
                    {
                        boss,
                        player
                    });

            byte[] encoded =
                WorldStateSnapshotCodec.Encode(
                    snapshot);

            Assert.That(
                encoded.Length,
                Is.EqualTo(
                    WorldStateSnapshotCodec.PrefixSize +
                    2 * WorldStateSnapshotCodec.RecordSize));

            WorldStateSnapshotPayload decoded =
                WorldStateSnapshotCodec.Decode(
                    encoded);

            Assert.That(
                decoded.Entities,
                Has.Count.EqualTo(2));

            Assert.That(
                decoded.Entities[0].EntityId,
                Is.EqualTo(1u));

            Assert.That(
                decoded.Entities[1].EntityId,
                Is.EqualTo(2u));

            Assert.That(
                decoded.Entities[1].BossPhase,
                Is.EqualTo((byte)2));

            Assert.That(
                decoded.Entities[1].CurrentHealth,
                Is.EqualTo((ushort)0));

            Assert.That(
                decoded.Entities[1].IsDead,
                Is.True);
        }

        [Test]
        public void Encode_RejectsDeathFlagThatDoesNotMatchHealth()
        {
            var invalid =
                new WorldEntityRecord(
                    1u,
                    NetworkEntityType.Enemy,
                    WorldEntityLifecycle.Snapshot,
                    WorldEntityFlags.Dead,
                    0f,
                    0f,
                    0f,
                    10,
                    10,
                    0,
                    NetworkEnemyArchetype.Basic);

            var snapshot =
                new WorldStateSnapshotPayload(
                    new List<WorldEntityRecord>
                    {
                        invalid
                    });

            Assert.Throws<ArgumentException>(
                () => WorldStateSnapshotCodec.Encode(
                    snapshot));
        }

        [Test]
        public void Decode_RejectsTruncatedRecord()
        {
            byte[] truncated =
                new byte[WorldStateSnapshotCodec.PrefixSize];

            PacketCodec.WriteNetworkUInt32(
                truncated,
                0,
                1u);

            Assert.Throws<ArgumentException>(
                () => WorldStateSnapshotCodec.Decode(
                    truncated));
        }

        [Test]
        public void EncodeThenDecode_PreservesEnemyArchetypeAtOffset25()
        {
            var enemy =
                new WorldEntityRecord(
                    0x10000001u,
                    NetworkEntityType.Enemy,
                    WorldEntityLifecycle.Snapshot,
                    WorldEntityFlags.Active,
                    1f,
                    2f,
                    0f,
                    1,
                    1,
                    0,
                    NetworkEnemyArchetype.Fast);

            byte[] encoded =
                WorldStateSnapshotCodec.Encode(
                    new WorldStateSnapshotPayload(
                        new[]
                        {
                            enemy
                        }));

            int recordOffset =
                WorldStateSnapshotCodec.PrefixSize;

            Assert.That(
                encoded[recordOffset + 25],
                Is.EqualTo(
                    (byte)NetworkEnemyArchetype.Fast));

            for (int offset = 26;
                offset < WorldStateSnapshotCodec.RecordSize;
                offset++)
            {
                Assert.That(
                    encoded[recordOffset + offset],
                    Is.Zero);
            }

            WorldStateSnapshotPayload decoded =
                WorldStateSnapshotCodec.Decode(encoded);

            Assert.That(
                decoded.Entities[0].EnemyArchetype,
                Is.EqualTo(NetworkEnemyArchetype.Fast));
        }

        [Test]
        public void Encode_RejectsMissingOrMisplacedEnemyArchetype()
        {
            var enemyWithoutArchetype =
                new WorldEntityRecord(
                    0x10000001u,
                    NetworkEntityType.Enemy,
                    WorldEntityLifecycle.Snapshot,
                    WorldEntityFlags.Active,
                    0f,
                    0f,
                    0f,
                    3,
                    3);

            var playerWithArchetype =
                new WorldEntityRecord(
                    1u,
                    NetworkEntityType.Player,
                    WorldEntityLifecycle.Snapshot,
                    WorldEntityFlags.Active,
                    0f,
                    0f,
                    0f,
                    5,
                    5,
                    0,
                    NetworkEnemyArchetype.Basic);

            Assert.Throws<ArgumentException>(
                () => WorldStateSnapshotCodec.Encode(
                    new WorldStateSnapshotPayload(
                        new[]
                        {
                            enemyWithoutArchetype
                        })));

            Assert.Throws<ArgumentException>(
                () => WorldStateSnapshotCodec.Encode(
                    new WorldStateSnapshotPayload(
                        new[]
                        {
                            playerWithArchetype
                        })));
        }
    }
}
