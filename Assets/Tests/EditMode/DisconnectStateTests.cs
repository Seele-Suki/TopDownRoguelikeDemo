using NUnit.Framework;
using TopDownRoguelike.Networking.Client;

namespace TopDownRoguelike.Tests.EditMode
{
    public sealed class DisconnectStateTests
    {
        [Test]
        public void TryBegin_FirstValidReasonWins()
        {
            var state = new DisconnectState();

            bool accepted =
                state.TryBegin(DisconnectReason.TransportError);

            Assert.That(accepted, Is.True);
            Assert.That(state.IsHandled, Is.True);
            Assert.That(
                state.Reason,
                Is.EqualTo(DisconnectReason.TransportError));
        }

        [Test]
        public void TryBegin_RejectsNone()
        {
            var state = new DisconnectState();

            bool accepted =
                state.TryBegin(DisconnectReason.None);

            Assert.That(accepted, Is.False);
            Assert.That(state.IsHandled, Is.False);
            Assert.That(
                state.Reason,
                Is.EqualTo(DisconnectReason.None));
        }

        [Test]
        public void TryBegin_DoesNotOverwriteFirstReason()
        {
            var state = new DisconnectState();

            Assert.That(
                state.TryBegin(DisconnectReason.ServerClosed),
                Is.True);

            Assert.That(
                state.TryBegin(DisconnectReason.HeartbeatTimeout),
                Is.False);

            Assert.That(
                state.Reason,
                Is.EqualTo(DisconnectReason.ServerClosed));
        }

        [Test]
        public void NewState_StartsUnhandledForNextConnectionLifecycle()
        {
            var state = new DisconnectState();

            Assert.That(state.IsHandled, Is.False);
            Assert.That(
                state.Reason,
                Is.EqualTo(DisconnectReason.None));
        }
    }
}
