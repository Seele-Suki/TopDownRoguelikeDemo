using NUnit.Framework;
using TopDownRoguelike.Infrastructure;
using TopDownRoguelike.Networking.Client;
using TopDownRoguelike.Networking.Room;
using UnityEngine;

namespace TopDownRoguelike.Tests.EditMode
{
    public sealed class DisconnectPauseControllerTests
    {
        [SetUp]
        public void SetUp()
        {
            DisconnectPauseController.Clear();
            Time.timeScale = 1f;
        }

        [TearDown]
        public void TearDown()
        {
            DisconnectPauseController.Clear();
            Time.timeScale = 1f;
            GameSession.Reset();
        }

        [Test]
        public void HostGameplayRemotePeerLeft_PausesOnceAndRestoresExactScale()
        {
            GameSession.ConfigureMultiplayerHost();
            Time.timeScale = 0.75f;
            var context = new DisconnectContext(
                RoomRole.Host,
                true,
                DisconnectReason.RemotePeerLeft);

            Assert.That(DisconnectPauseController.TryPause(context), Is.True);
            Assert.That(Time.timeScale, Is.EqualTo(0f));
            Assert.That(DisconnectPauseController.TryPause(context), Is.False);

            DisconnectPauseController.Restore();

            Assert.That(Time.timeScale, Is.EqualTo(0.75f));
            Assert.That(DisconnectPauseController.IsPaused, Is.False);
        }

        [Test]
        public void ClientGameplayRemotePeerLeft_DoesNotPause()
        {
            GameSession.ConfigureMultiplayerClient();
            Time.timeScale = 0.5f;

            Assert.That(
                DisconnectPauseController.TryPause(
                    new DisconnectContext(
                        RoomRole.Client,
                        true,
                        DisconnectReason.RemotePeerLeft)),
                Is.False);
            Assert.That(Time.timeScale, Is.EqualTo(0.5f));
        }

        [Test]
        public void HostNonGameplayOrOtherReason_DoesNotPause()
        {
            GameSession.ConfigureMultiplayerHost();
            Time.timeScale = 0.5f;

            Assert.That(
                DisconnectPauseController.TryPause(
                    new DisconnectContext(
                        RoomRole.Host,
                        false,
                        DisconnectReason.RemotePeerLeft)),
                Is.False);
            Assert.That(
                DisconnectPauseController.TryPause(
                    new DisconnectContext(
                        RoomRole.Host,
                        true,
                        DisconnectReason.ServerClosed)),
                Is.False);
            Assert.That(Time.timeScale, Is.EqualTo(0.5f));
        }
    }
}
