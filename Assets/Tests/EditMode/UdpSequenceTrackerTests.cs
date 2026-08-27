using NUnit.Framework;
using TopDownRoguelike.Networking.Protocol;

namespace TopDownRoguelike.Tests.EditMode
{
    public sealed class UdpSequenceTrackerTests
    {
        [Test]
        public void Accept_RejectsDuplicateAndOlderSequence()
        {
            var tracker = new UdpSequenceTracker();

            Assert.That(tracker.HasSequence, Is.False);

            Assert.That(
                tracker.Accept(100u),
                Is.True);

            Assert.That(tracker.HasSequence, Is.True);
            Assert.That(
                tracker.LastSequence,
                Is.EqualTo(100u));

            Assert.That(
                tracker.Accept(100u),
                Is.False);

            Assert.That(
                tracker.Accept(99u),
                Is.False);

            Assert.That(
                tracker.Accept(101u),
                Is.True);

            Assert.That(
                tracker.LastSequence,
                Is.EqualTo(101u));
        }

        [Test]
        public void Accept_AllowsSequenceWrapAround()
        {
            var tracker = new UdpSequenceTracker();

            Assert.That(
                tracker.Accept(0xFFFFFFFEu),
                Is.True);

            Assert.That(
                tracker.Accept(0xFFFFFFFFu),
                Is.True);

            Assert.That(
                tracker.Accept(0u),
                Is.True);

            Assert.That(
                tracker.Accept(0xFFFFFFFFu),
                Is.False);
        }

        [Test]
        public void Accept_RejectsAmbiguousHalfRange()
        {
            var tracker = new UdpSequenceTracker();

            Assert.That(
                tracker.Accept(0u),
                Is.True);

            Assert.That(
                tracker.Accept(0x80000000u),
                Is.False);
        }

        [Test]
        public void SeparateTrackers_DoNotSuppressEachOther()
        {
            var playerInputTracker =
                new UdpSequenceTracker();

            var snapshotTracker =
                new UdpSequenceTracker();

            Assert.That(
                playerInputTracker.Accept(42u),
                Is.True);

            Assert.That(
                snapshotTracker.Accept(42u),
                Is.True);

            Assert.That(
                playerInputTracker.Accept(42u),
                Is.False);

            Assert.That(
                snapshotTracker.Accept(42u),
                Is.False);
        }
    }
}