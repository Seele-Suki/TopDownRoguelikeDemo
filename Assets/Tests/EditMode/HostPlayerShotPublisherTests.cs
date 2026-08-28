using NUnit.Framework;
using TopDownRoguelike.Gameplay.Networking;
using TopDownRoguelike.Networking.Protocol;
using UnityEngine;

namespace TopDownRoguelike.Tests.EditMode
{
    public sealed class HostPlayerShotPublisherTests
    {
        [Test]
        public void Configure_ForwardsGeneratedShotEvent()
        {
            GameObject root =
                new GameObject(
                    "Host Shot Publisher Test");

            GameObject player =
                new GameObject(
                    "Host Player Test");

            try
            {
                PlayerShooterShotEventSource source =
                    player.AddComponent<
                        PlayerShooterShotEventSource>();

                HostPlayerShotPublisher publisher =
                    root.AddComponent<
                        HostPlayerShotPublisher>();

                source.Configure(
                    7u);

                PlayerShotEvent received =
                    null;

                publisher.Configure(
                    source,
                    shotEvent =>
                    {
                        received =
                            shotEvent;
                    });

                source.NotifyShot(
                    new Vector2(
                        3.0f,
                        4.0f));

                Assert.That(
                    received,
                    Is.Not.Null);

                Assert.That(
                    received.PlayerId,
                    Is.EqualTo(7u));

                Assert.That(
                    received.ShotSequence,
                    Is.EqualTo(1u));

                Assert.That(
                    received.DirectionX,
                    Is.EqualTo(0.6f)
                        .Within(0.0001f));

                Assert.That(
                    received.DirectionY,
                    Is.EqualTo(0.8f)
                        .Within(0.0001f));
            }
            finally
            {
                Object.DestroyImmediate(
                    root);

                Object.DestroyImmediate(
                    player);
            }
        }

        [Test]
        public void Disable_StopsForwardingShots()
        {
            GameObject root =
                new GameObject(
                    "Host Shot Publisher Test");

            GameObject player =
                new GameObject(
                    "Host Player Test");

            try
            {
                PlayerShooterShotEventSource source =
                    player.AddComponent<
                        PlayerShooterShotEventSource>();

                HostPlayerShotPublisher publisher =
                    root.AddComponent<
                        HostPlayerShotPublisher>();

                source.Configure(
                    7u);

                int receivedCount =
                    0;

                publisher.Configure(
                    source,
                    shotEvent =>
                    {
                        receivedCount++;
                    });

                root.SetActive(
                    false);

                source.NotifyShot(
                    Vector2.right);

                Assert.That(
                    receivedCount,
                    Is.EqualTo(0));
            }
            finally
            {
                Object.DestroyImmediate(
                    root);

                Object.DestroyImmediate(
                    player);
            }
        }
    }
}