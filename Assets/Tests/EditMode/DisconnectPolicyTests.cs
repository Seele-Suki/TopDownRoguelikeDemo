using NUnit.Framework;
using TopDownRoguelike.Networking.Client;
using TopDownRoguelike.Networking.Room;

namespace TopDownRoguelike.Tests.EditMode
{
    public sealed class DisconnectPolicyTests
    {
        [Test]
        public void HostInGameplay_WhenClientLeaves_ShowsContinuationDialog()
        {
            DisconnectAction action =
                DisconnectPolicy.Resolve(
                    new DisconnectContext(
                        RoomRole.Host,
                        true,
                        DisconnectReason.RemotePeerLeft));

            Assert.That(
                action,
                Is.EqualTo(DisconnectAction.ShowClientDisconnectedDialog));
        }

        [Test]
        public void HostInRoom_WhenClientLeaves_ReturnsToMultiplayerMenu()
        {
            DisconnectAction action =
                DisconnectPolicy.Resolve(
                    new DisconnectContext(
                        RoomRole.Host,
                        false,
                        DisconnectReason.RemotePeerLeft));

            Assert.That(
                action,
                Is.EqualTo(DisconnectAction.ReturnToMultiplayerMenu));
        }

        [Test]
        public void Client_WhenHostLeaves_ShowsConfirmationDialog()
        {
            DisconnectAction action =
                DisconnectPolicy.Resolve(
                    new DisconnectContext(
                        RoomRole.Client,
                        true,
                        DisconnectReason.RemotePeerLeft));

            Assert.That(
                action,
                Is.EqualTo(DisconnectAction.ShowHostDisconnectedDialog));
        }

        [TestCase(DisconnectReason.ServerClosed)]
        [TestCase(DisconnectReason.TransportError)]
        [TestCase(DisconnectReason.HeartbeatTimeout)]
        public void NetworkFailure_ReturnsToMultiplayerMenu(
            DisconnectReason reason)
        {
            DisconnectAction hostAction =
                DisconnectPolicy.Resolve(
                    new DisconnectContext(
                        RoomRole.Host,
                        true,
                        reason));

            DisconnectAction clientAction =
                DisconnectPolicy.Resolve(
                    new DisconnectContext(
                        RoomRole.Client,
                        true,
                        reason));

            Assert.That(
                hostAction,
                Is.EqualTo(DisconnectAction.ReturnToMultiplayerMenu));
            Assert.That(
                clientAction,
                Is.EqualTo(DisconnectAction.ReturnToMultiplayerMenu));
        }

        [Test]
        public void LocalLeaveRoom_ReturnsToMultiplayerMenu()
        {
            DisconnectAction action =
                DisconnectPolicy.Resolve(
                    new DisconnectContext(
                        RoomRole.Client,
                        false,
                        DisconnectReason.LocalLeaveRoom,
                        true));

            Assert.That(
                action,
                Is.EqualTo(DisconnectAction.ReturnToMultiplayerMenu));
        }

        [Test]
        public void ApplicationQuit_DoesNotOpenDisconnectUi()
        {
            DisconnectAction action =
                DisconnectPolicy.Resolve(
                    new DisconnectContext(
                        RoomRole.Host,
                        true,
                        DisconnectReason.ApplicationQuit,
                        true));

            Assert.That(
                action,
                Is.EqualTo(DisconnectAction.None));
        }

        [Test]
        public void InvalidContext_DoesNothing()
        {
            DisconnectAction action =
                DisconnectPolicy.Resolve(
                    new DisconnectContext(
                        RoomRole.None,
                        false,
                        DisconnectReason.None));

            Assert.That(
                action,
                Is.EqualTo(DisconnectAction.None));
        }
    }
}
