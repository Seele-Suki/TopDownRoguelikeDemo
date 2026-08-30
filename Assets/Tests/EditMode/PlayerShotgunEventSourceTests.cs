using NUnit.Framework;
using TopDownRoguelike.Gameplay.Networking;
using TopDownRoguelike.Networking.Protocol;
using UnityEngine;

namespace TopDownRoguelike.Tests.EditMode
{
    public sealed class PlayerShotgunEventSourceTests
    {
        [Test]
        public void NotifyShotgun_CreatesEventWithIncrementingVolleySequence()
        {
            GameObject player =
                new GameObject(
                    "Player Shotgun Event Source Test");

            try
            {
                player.transform.position =
                    new Vector3(
                        3.0f,
                        -2.0f,
                        0.0f);

                PlayerShotgunEventSource source =
                    player.AddComponent<
                        PlayerShotgunEventSource>();

                source.Configure(7u);

                PlayerShotgunEvent firstEvent =
                    null;

                PlayerShotgunEvent secondEvent =
                    null;

                source.ShotgunGenerated +=
                    shotgunEvent =>
                    {
                        if (firstEvent == null)
                        {
                            firstEvent =
                                shotgunEvent;
                        }
                        else
                        {
                            secondEvent =
                                shotgunEvent;
                        }
                    };

                source.NotifyShotgun(
                    new Vector2(
                        3.0f,
                        4.0f),
                    5u,
                    40.0f,
                    0.75f);

                source.NotifyShotgun(
                    Vector2.right,
                    2u,
                    0.0f,
                    0.30f);

                Assert.That(
                    firstEvent,
                    Is.Not.Null);

                Assert.That(
                    secondEvent,
                    Is.Not.Null);

                Assert.That(
                    firstEvent.PlayerId,
                    Is.EqualTo(7u));

                Assert.That(
                    firstEvent.VolleySequence,
                    Is.EqualTo(1u));

                Assert.That(
                    firstEvent.OriginX,
                    Is.EqualTo(3.0f)
                        .Within(0.0001f));

                Assert.That(
                    firstEvent.OriginY,
                    Is.EqualTo(-2.0f)
                        .Within(0.0001f));

                Assert.That(
                    firstEvent.CenterDirectionX,
                    Is.EqualTo(0.6f)
                        .Within(0.0001f));

                Assert.That(
                    firstEvent.CenterDirectionY,
                    Is.EqualTo(0.8f)
                        .Within(0.0001f));

                Assert.That(
                    firstEvent.ProjectileCount,
                    Is.EqualTo(5u));

                Assert.That(
                    firstEvent.SpreadAngle,
                    Is.EqualTo(40.0f)
                        .Within(0.0001f));

                Assert.That(
                    firstEvent.EffectiveCooldown,
                    Is.EqualTo(0.75f)
                        .Within(0.0001f));

                Assert.That(
                    secondEvent.VolleySequence,
                    Is.EqualTo(2u));

                Assert.That(
                    secondEvent.ProjectileCount,
                    Is.EqualTo(2u));

                Assert.That(
                    secondEvent.SpreadAngle,
                    Is.EqualTo(0.0f)
                        .Within(0.0001f));

                Assert.That(
                    secondEvent.EffectiveCooldown,
                    Is.EqualTo(0.30f)
                        .Within(0.0001f));
            }
            finally
            {
                Object.DestroyImmediate(player);
            }
        }

        [Test]
        public void Configure_RejectsZeroPlayerId()
        {
            GameObject player =
                new GameObject(
                    "Player Shotgun Event Source Test");

            try
            {
                PlayerShotgunEventSource source =
                    player.AddComponent<
                        PlayerShotgunEventSource>();

                Assert.Throws<System.ArgumentException>(
                    () =>
                    {
                        source.Configure(0u);
                    });
            }
            finally
            {
                Object.DestroyImmediate(player);
            }
        }

        [Test]
        public void
        NotifyShotgun_WrapsVolleySequenceFromMaxValueToZero()
        {
            GameObject player =
                new GameObject(
                    "Player Shotgun Sequence Wrap Test");

            try
            {
                PlayerShotgunEventSource source =
                    player.AddComponent<
                        PlayerShotgunEventSource>();

                source.Configure(
                    7u);

                var nextSequenceField =
                    typeof(PlayerShotgunEventSource)
                        .GetField(
                            "nextVolleySequence",
                            System.Reflection.BindingFlags.Instance |
                            System.Reflection.BindingFlags.NonPublic);

                Assert.That(
                    nextSequenceField,
                    Is.Not.Null);

                nextSequenceField.SetValue(
                    source,
                    uint.MaxValue);

                PlayerShotgunEvent firstEvent =
                    null;

                PlayerShotgunEvent secondEvent =
                    null;

                source.ShotgunGenerated +=
                    shotgunEvent =>
                    {
                        if (firstEvent == null)
                        {
                            firstEvent =
                                shotgunEvent;
                        }
                        else
                        {
                            secondEvent =
                                shotgunEvent;
                        }
                    };

                source.NotifyShotgun(
                    Vector2.right,
                    5u,
                    40.0f,
                    0.75f);

                source.NotifyShotgun(
                    Vector2.right,
                    5u,
                    40.0f,
                    0.75f);

                Assert.That(
                    firstEvent,
                    Is.Not.Null);

                Assert.That(
                    secondEvent,
                    Is.Not.Null);

                Assert.That(
                    firstEvent.VolleySequence,
                    Is.EqualTo(uint.MaxValue),
                    "The last sequence before wrapping " +
                    "must remain uint.MaxValue.");

                Assert.That(
                    secondEvent.VolleySequence,
                    Is.EqualTo(0u),
                    "The next Host volley must wrap " +
                    "from uint.MaxValue to zero.");
            }
            finally
            {
                Object.DestroyImmediate(
                    player);
            }
        }

        [Test]
        public void NotifyShotgun_RejectsZeroDirection()
        {
            GameObject player =
                new GameObject(
                    "Player Shotgun Event Source Test");

            try
            {
                PlayerShotgunEventSource source =
                    player.AddComponent<
                        PlayerShotgunEventSource>();

                source.Configure(7u);

                Assert.Throws<System.ArgumentException>(
                    () =>
                    {
                        source.NotifyShotgun(
                            Vector2.zero,
                            5u,
                            40.0f,
                            0.75f);
                    });
            }
            finally
            {
                Object.DestroyImmediate(player);
            }
        }
    }
}