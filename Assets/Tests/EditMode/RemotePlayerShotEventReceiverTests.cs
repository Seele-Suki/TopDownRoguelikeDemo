using NUnit.Framework;
using TopDownRoguelike.Gameplay.Networking;
using TopDownRoguelike.Networking.Protocol;
using UnityEngine;

namespace TopDownRoguelike.Tests.EditMode
{
    public sealed class RemotePlayerShotEventReceiverTests
    {
        [Test]
        public void Enqueue_StoresMatchingRemoteShot()
        {
            GameObject receiverObject =
                new GameObject(
                    "Remote Shot Receiver Test");

            try
            {
                RemotePlayerShotEventReceiver receiver =
                    receiverObject.AddComponent<
                        RemotePlayerShotEventReceiver>();

                receiver.Configure(
                    9u);

                var expected =
                    new PlayerShotEvent(
                        9u,
                        12u,
                        1.0f,
                        2.0f,
                        0.6f,
                        0.8f);

                receiver.Enqueue(
                    9u,
                    expected);

                Assert.That(
                    receiver.PendingCount,
                    Is.EqualTo(1));

                Assert.That(
                    receiver.TryDequeue(
                        out PlayerShotEvent actual),
                    Is.True);

                Assert.That(
                    actual.PlayerId,
                    Is.EqualTo(9u));

                Assert.That(
                    actual.ShotSequence,
                    Is.EqualTo(12u));
            }
            finally
            {
                Object.DestroyImmediate(
                    receiverObject);
            }
        }

        [Test]
        public void Enqueue_RejectsDifferentSender()
        {
            GameObject receiverObject =
                new GameObject(
                    "Remote Shot Receiver Test");

            try
            {
                RemotePlayerShotEventReceiver receiver =
                    receiverObject.AddComponent<
                        RemotePlayerShotEventReceiver>();

                receiver.Configure(
                    9u);

                var shotEvent =
                    new PlayerShotEvent(
                        9u,
                        1u,
                        0.0f,
                        0.0f,
                        1.0f,
                        0.0f);

                Assert.Throws<System.ArgumentException>(
                    () =>
                        receiver.Enqueue(
                            7u,
                            shotEvent));
            }
            finally
            {
                Object.DestroyImmediate(
                    receiverObject);
            }
        }

        [Test]
        public void Clear_RemovesPendingShots()
        {
            GameObject receiverObject =
                new GameObject(
                    "Remote Shot Receiver Test");

            try
            {
                RemotePlayerShotEventReceiver receiver =
                    receiverObject.AddComponent<
                        RemotePlayerShotEventReceiver>();

                receiver.Configure(
                    9u);

                receiver.Enqueue(
                    9u,
                    new PlayerShotEvent(
                        9u,
                        1u,
                        0.0f,
                        0.0f,
                        1.0f,
                        0.0f));

                receiver.Clear();

                Assert.That(
                    receiver.PendingCount,
                    Is.EqualTo(0));
            }
            finally
            {
                Object.DestroyImmediate(
                    receiverObject);
            }
        }
    }
}