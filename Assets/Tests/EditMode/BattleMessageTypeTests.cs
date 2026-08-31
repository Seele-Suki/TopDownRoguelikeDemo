using System;
using NUnit.Framework;
using TopDownRoguelike.Networking.Protocol;

namespace TopDownRoguelike.Tests.EditMode
{
    public sealed class BattleMessageTypeTests
    {
        [Test]
        public void Phase6BattleEvents_HaveStableTcpValues()
        {
            AssertMessageType(
                "WorldEntitySpawned",
                41);

            AssertMessageType(
                "WorldEntityRemoved",
                42);

            AssertMessageType(
                "PlayerDied",
                43);

            AssertMessageType(
                "ExperienceOrbSpawned",
                44);

            AssertMessageType(
                "ExperienceOrbCollected",
                45);

            AssertMessageType(
                "UpgradeStarted",
                46);

            AssertMessageType(
                "UpgradeChoiceSubmitted",
                47);

            AssertMessageType(
                "UpgradeCompleted",
                48);

            AssertMessageType(
                "BossPhaseChanged",
                49);

            AssertMessageType(
                "GameResult",
                50);
        }

        private static void AssertMessageType(
            string name,
            ushort expectedValue)
        {
            Assert.That(
                Enum.IsDefined(
                    typeof(MessageType),
                    name),
                Is.True,
                $"{name} is not defined.");

            MessageType messageType =
                (MessageType)Enum.Parse(
                    typeof(MessageType),
                    name);

            Assert.That(
                (ushort)messageType,
                Is.EqualTo(expectedValue));
        }
    }
}