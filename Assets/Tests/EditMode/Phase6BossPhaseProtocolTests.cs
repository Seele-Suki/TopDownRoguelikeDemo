using System;
using NUnit.Framework;
using TopDownRoguelike.Networking.Protocol;

namespace TopDownRoguelike.Tests.EditMode
{
    public sealed class Phase6BossPhaseProtocolTests
    {
        [TestCase(BossCombatState.Started)]
        [TestCase(BossCombatState.Paused)]
        [TestCase(BossCombatState.Resumed)]
        public void BossCombatState_RoundTrips(BossCombatState state)
        {
            var payload = BossCombatStateCodec.Encode(
                new BossCombatStatePayload(state));

            BossCombatStatePayload decoded =
                BossCombatStateCodec.Decode(payload);

            Assert.That(decoded.State, Is.EqualTo(state));
        }

        [Test]
        public void BossCombatState_RejectsTruncatedPayload()
        {
            Assert.Throws<ArgumentException>(() =>
                BossCombatStateCodec.Decode(Array.Empty<byte>()));
        }

        [TestCase((byte)0)]
        [TestCase((byte)4)]
        public void BossCombatState_RejectsUnknownValue(byte value)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                BossCombatStateCodec.Decode(new[] { value }));
        }

        [TestCase((byte)1)]
        [TestCase((byte)2)]
        public void BossEntityPhase_AcceptsSupportedValues(byte phase)
        {
            WorldEntityRecord entity = CreateEntity(
                NetworkEntityType.Boss,
                phase);

            WorldStateSnapshotPayload decoded =
                WorldStateSnapshotCodec.Decode(
                    WorldStateSnapshotCodec.Encode(
                        new WorldStateSnapshotPayload(
                            new[] { entity })));

            Assert.That(decoded.Entities[0].BossPhase,
                Is.EqualTo(phase));
        }

        [TestCase((byte)0)]
        [TestCase((byte)3)]
        public void BossEntityPhase_RejectsUnsupportedValues(byte phase)
        {
            Assert.Throws<ArgumentException>(() =>
                WorldStateSnapshotCodec.Encode(
                    new WorldStateSnapshotPayload(
                        new[] { CreateEntity(
                            NetworkEntityType.Boss,
                            phase) })));
        }

        [Test]
        public void NonBossEntityPhase_IsRejected()
        {
            Assert.Throws<ArgumentException>(() =>
                WorldStateSnapshotCodec.Encode(
                    new WorldStateSnapshotPayload(
                        new[] { CreateEntity(
                            NetworkEntityType.Enemy,
                            1) })));
        }

        private static WorldEntityRecord CreateEntity(
            NetworkEntityType entityType,
            byte bossPhase)
        {
            return new WorldEntityRecord(
                1u,
                entityType,
                WorldEntityLifecycle.Snapshot,
                WorldEntityFlags.Active,
                0f,
                0f,
                0f,
                10,
                10,
                bossPhase,
                entityType == NetworkEntityType.Enemy
                    ? NetworkEnemyArchetype.Basic
                    : NetworkEnemyArchetype.Invalid);
        }
    }
}
