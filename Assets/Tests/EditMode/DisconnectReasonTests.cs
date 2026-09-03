using NUnit.Framework;
using TopDownRoguelike.Networking.Client;

namespace TopDownRoguelike.Tests.EditMode
{
    public sealed class DisconnectReasonTests
    {
        [Test]
        public void DisconnectReason_ContainsDistinctLifecycleCauses()
        {
            Assert.That(
                DisconnectReason.LocalLeaveRoom,
                Is.Not.EqualTo(DisconnectReason.RemotePeerLeft));
            Assert.That(
                DisconnectReason.ServerClosed,
                Is.Not.EqualTo(DisconnectReason.TransportError));
            Assert.That(
                DisconnectReason.HeartbeatTimeout,
                Is.Not.EqualTo(DisconnectReason.ApplicationQuit));
        }

        [Test]
        public void DisconnectReason_NoneIsNotAHandledCause()
        {
            Assert.That(
                DisconnectReason.None,
                Is.EqualTo((DisconnectReason)0));
        }
    }
}
