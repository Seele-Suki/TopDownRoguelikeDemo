using System;
using System.Collections.Generic;
using NUnit.Framework;
using TopDownRoguelike.Networking.Protocol;

namespace TopDownRoguelike.Tests.EditMode
{
    public sealed class Phase6ExperienceUpgradeProtocolTests
    {
        [Test]
        public void SharedExperienceSnapshot_RoundTripsNetworkFields()
        {
            var expected = new SharedExperienceSnapshotPayload(
                7u,
                3,
                12,
                20);

            SharedExperienceSnapshotPayload actual =
                SharedExperienceSnapshotCodec.Decode(
                    SharedExperienceSnapshotCodec.Encode(expected));

            Assert.That(actual.Sequence, Is.EqualTo(7u));
            Assert.That(actual.CurrentLevel, Is.EqualTo(3));
            Assert.That(actual.CurrentExperience, Is.EqualTo(12));
            Assert.That(actual.ExperienceToNextLevel, Is.EqualTo(20));
        }

        [Test]
        public void SharedExperienceSnapshot_TruncatedPayloadIsRejected()
        {
            Assert.Throws<ArgumentException>(
                () => SharedExperienceSnapshotCodec.Decode(
                    new byte[SharedExperienceSnapshotCodec.PayloadSize - 1]));
        }

        [Test]
        public void UpgradeStartedPayload_RoundTripsOptionIds()
        {
            var expected = new UpgradeStartedPayload(
                9u,
                new ushort[] { 101, 202, 303 });

            UpgradeStartedPayload actual =
                UpgradeNetworkCodec.DecodeStarted(
                    UpgradeNetworkCodec.EncodeStarted(expected));

            Assert.That(actual.Sequence, Is.EqualTo(9u));
            Assert.That(actual.UpgradeIds,
                Is.EqualTo(new ushort[] { 101, 202, 303 }));
        }

        [Test]
        public void UpgradeCompletedPayload_RoundTripsBothPlayerChoices()
        {
            var expected = new UpgradeCompletedPayload(
                11u,
                new Dictionary<uint, ushort>
                {
                    { 1u, 101 },
                    { 2u, 202 }
                });

            UpgradeCompletedPayload actual =
                UpgradeNetworkCodec.DecodeCompleted(
                    UpgradeNetworkCodec.EncodeCompleted(expected));

            Assert.That(actual.Sequence, Is.EqualTo(11u));
            Assert.That(actual.Choices[1u], Is.EqualTo((ushort)101));
            Assert.That(actual.Choices[2u], Is.EqualTo((ushort)202));
        }

        [Test]
        public void UpgradeCompletedPayload_TruncatedPayloadIsRejected()
        {
            Assert.Throws<ArgumentException>(
                () => UpgradeNetworkCodec.DecodeCompleted(
                    new byte[15]));
        }
    }
}
