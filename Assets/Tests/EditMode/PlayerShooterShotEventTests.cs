using NUnit.Framework;
using TopDownRoguelike.Networking.Protocol;
using TopDownRoguelike.Gameplay.Networking;
using UnityEngine;

namespace TopDownRoguelike.Tests.EditMode
{
    public sealed class PlayerShooterShotEventTests
    {
        [Test]
        public void NotifyShot_CreatesEventWithPlayerIdAndSequence()
        {
            GameObject player =
                new GameObject("Player");

            try
            {
                PlayerShooterShotEventSource source =
                    player.AddComponent<
                        PlayerShooterShotEventSource>();

                source.Configure(7u);

                PlayerShotEvent receivedEvent =
                    null;

                source.ShotGenerated +=
                    shotEvent =>
                    {
                        receivedEvent =
                            shotEvent;
                    };

                source.NotifyShot(
                    new Vector2(3.0f, 4.0f));

                Assert.That(
                    receivedEvent,
                    Is.Not.Null);

                Assert.That(
                    receivedEvent.PlayerId,
                    Is.EqualTo(7u));

                Assert.That(
                    receivedEvent.ShotSequence,
                    Is.EqualTo(1u));

                Assert.That(
                    receivedEvent.DirectionX,
                    Is.EqualTo(0.6f)
                        .Within(0.0001f));

                Assert.That(
                    receivedEvent.DirectionY,
                    Is.EqualTo(0.8f)
                        .Within(0.0001f));
            }
            finally
            {
                Object.DestroyImmediate(player);
            }
        }

        [Test]
        public void NotifyShot_IncrementsSequenceForEachShot()
        {
            GameObject player =
                new GameObject("Player");

            try
            {
                PlayerShooterShotEventSource source =
                    player.AddComponent<
                    PlayerShooterShotEventSource>();

                source.Configure(7u);

                uint firstSequence = 0u;
                uint secondSequence = 0u;

                int invocationCount = 0;

                source.ShotGenerated +=
                    shotEvent =>
                    {
                        invocationCount++;

                        if (invocationCount == 1)
                        {
                            firstSequence =
                                shotEvent.ShotSequence;
                        }
                        else if (invocationCount == 2)
                        {
                            secondSequence =
                                shotEvent.ShotSequence;
                        }
                    };

                source.NotifyShot(
                    Vector2.right);

                source.NotifyShot(
                    Vector2.up);

                Assert.That(
                    invocationCount,
                    Is.EqualTo(2));

                Assert.That(
                    firstSequence,
                    Is.EqualTo(1u));

                Assert.That(
                    secondSequence,
                    Is.EqualTo(2u));
            }
            finally
            {
                Object.DestroyImmediate(player);
            }
        }

        [Test]
        public void NotifyShot_RejectsZeroDirection()
        {
            GameObject player =
                new GameObject("Player");

            try
            {
                PlayerShooterShotEventSource source =
                    player.AddComponent<
                    PlayerShooterShotEventSource>();

                source.Configure(7u);

                Assert.Throws<System.ArgumentException>(
                    () =>
                        source.NotifyShot(Vector2.zero));
            }
            finally
            {
                Object.DestroyImmediate(player);
            }
        }
    }
}