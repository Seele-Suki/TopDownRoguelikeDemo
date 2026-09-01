using System;
using NUnit.Framework;
using TopDownRoguelike.Gameplay.Networking;
using TopDownRoguelike.Networking.Protocol;
using UnityEngine;

namespace TopDownRoguelike.Tests.EditMode
{
    public sealed class HostPlayerShotgunPublisherTests
    {
        [Test]
        public void Configure_ForwardsGeneratedShotgunEvent()
        {
            GameObject root =
                new GameObject(
                    "Host Shotgun Publisher Test");

            GameObject player =
                new GameObject(
                    "Host Shotgun Player Test");

            try
            {
                PlayerShotgunEventSource source =
                    player.AddComponent<
                        PlayerShotgunEventSource>();

                HostPlayerShotgunPublisher publisher =
                    root.AddComponent<
                        HostPlayerShotgunPublisher>();

                source.Configure(
                    7u);

                PlayerShotgunEvent received =
                    null;

                publisher.Configure(
                    source,
                    shotgunEvent =>
                    {
                        received =
                            shotgunEvent;
                    });

                source.NotifyShotgun(
                    new Vector2(
                        3.0f,
                        4.0f),
                    5u,
                    40.0f,
                    0.75f);

                Assert.That(
                    received,
                    Is.Not.Null);

                Assert.That(
                    received.PlayerId,
                    Is.EqualTo(7u));

                Assert.That(
                    received.VolleySequence,
                    Is.EqualTo(1u));

                Assert.That(
                    received.ProjectileCount,
                    Is.EqualTo(5u));

                Assert.That(
                    received.SpreadAngle,
                    Is.EqualTo(40.0f)
                        .Within(0.0001f));

                Assert.That(
                    received.EffectiveCooldown,
                    Is.EqualTo(0.75f)
                        .Within(0.0001f));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(
                    root);

                UnityEngine.Object.DestroyImmediate(
                    player);
            }
        }

        [Test]
        public void Disable_StopsForwardingShotgunEvents()
        {
            GameObject root =
                new GameObject(
                    "Host Shotgun Publisher Test");

            GameObject player =
                new GameObject(
                    "Host Shotgun Player Test");

            try
            {
                PlayerShotgunEventSource source =
                    player.AddComponent<
                        PlayerShotgunEventSource>();

                HostPlayerShotgunPublisher publisher =
                    root.AddComponent<
                        HostPlayerShotgunPublisher>();

                source.Configure(
                    7u);

                int receivedCount =
                    0;

                publisher.Configure(
                    source,
                    shotgunEvent =>
                    {
                        receivedCount++;
                    });

                root.SetActive(
                    false);

                source.NotifyShotgun(
                    Vector2.right,
                    5u,
                    40.0f,
                    0.75f);

                Assert.That(
                    receivedCount,
                    Is.EqualTo(0));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(
                    root);

                UnityEngine.Object.DestroyImmediate(
                    player);
            }
        }

        [Test]
        public void SendFailure_DoesNotBubbleBackToShotgunSource()
        {
            GameObject root = new GameObject("Host Shotgun Publisher Failure Test");
            GameObject player = new GameObject("Host Shotgun Failure Test");
            try
            {
                var source = player.AddComponent<PlayerShotgunEventSource>();
                var publisher = root.AddComponent<HostPlayerShotgunPublisher>();
                source.Configure(7u);
                publisher.Configure(source, _ => throw new InvalidOperationException("send failed"));

                Assert.DoesNotThrow(() => source.NotifyShotgun(Vector2.right, 1u, 20f, 0.5f));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
                UnityEngine.Object.DestroyImmediate(player);
            }
        }

        [Test]
        public void Configure_RejectsNullDependencies()
        {
            GameObject root =
                new GameObject(
                    "Host Shotgun Publisher Test");

            try
            {
                HostPlayerShotgunPublisher publisher =
                    root.AddComponent<
                        HostPlayerShotgunPublisher>();

                Assert.Throws<ArgumentNullException>(
                    () =>
                    {
                        publisher.Configure(
                            null,
                            shotgunEvent =>
                            {
                            });
                    });

                GameObject player =
                    new GameObject(
                        "Host Shotgun Player Test");

                try
                {
                    PlayerShotgunEventSource source =
                        player.AddComponent<
                            PlayerShotgunEventSource>();

                    source.Configure(
                        7u);

                    Assert.Throws<ArgumentNullException>(
                        () =>
                        {
                            publisher.Configure(
                                source,
                                null);
                        });
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(
                        player);
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(
                    root);
            }
        }
    }
}
