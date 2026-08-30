using System;
using NUnit.Framework;
using TopDownRoguelike.Gameplay.Networking;
using TopDownRoguelike.Networking.Protocol;
using UnityEngine;

namespace TopDownRoguelike.Tests.EditMode
{
    public sealed class RemotePlayerShotgunEventReceiverTests
    {
        [Test]
        public void EnqueueAndDequeue_PreservesShotgunEvent()
        {
            GameObject player =
                new GameObject(
                    "Remote Shotgun Receiver Test");

            try
            {
                RemotePlayerShotgunEventReceiver receiver =
                    player.AddComponent<
                        RemotePlayerShotgunEventReceiver>();

                receiver.Configure(
                    9u);

                PlayerShotgunEvent expected =
                    CreateEvent(
                        9u,
                        1u);

                receiver.Enqueue(
                    9u,
                    expected);

                Assert.That(
                    receiver.PendingCount,
                    Is.EqualTo(1));

                bool dequeued =
                    receiver.TryDequeue(
                        out PlayerShotgunEvent actual);

                Assert.That(
                    dequeued,
                    Is.True);

                Assert.That(
                    actual,
                    Is.SameAs(expected));

                Assert.That(
                    receiver.PendingCount,
                    Is.EqualTo(0));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(
                    player);
            }
        }

        [Test]
        public void Enqueue_RejectsWrongPlayerAndDuplicateSequence()
        {
            GameObject player =
                new GameObject(
                    "Remote Shotgun Receiver Test");

            try
            {
                RemotePlayerShotgunEventReceiver receiver =
                    player.AddComponent<
                        RemotePlayerShotgunEventReceiver>();

                receiver.Configure(
                    9u);

                receiver.Enqueue(
                    7u,
                    CreateEvent(
                        9u,
                        5u));

                Assert.Throws<ArgumentException>(
                    () =>
                    {
                        receiver.Enqueue(
                            7u,
                            CreateEvent(
                                8u,
                                6u));
                    });

                Assert.Throws<ArgumentException>(
                    () =>
                    {
                        receiver.Enqueue(
                            7u,
                            CreateEvent(
                                9u,
                                5u));
                    });
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(
                    player);
            }
        }

        [Test]
        public void Enqueue_RejectsOldSequence()
        {
            GameObject player =
                new GameObject(
                    "Remote Shotgun Receiver Test");

            try
            {
                RemotePlayerShotgunEventReceiver receiver =
                    player.AddComponent<
                        RemotePlayerShotgunEventReceiver>();

                receiver.Configure(
                    9u);

                receiver.Enqueue(
                    9u,
                    CreateEvent(
                        9u,
                        100u));

                Assert.Throws<ArgumentException>(
                    () =>
                    {
                        receiver.Enqueue(
                            9u,
                            CreateEvent(
                                9u,
                                99u));
                    });
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(
                    player);
            }
        }

        [Test]
        public void
    Enqueue_AcceptsWrapAndRejectsAmbiguousHalfRange()
        {
            GameObject player =
                new GameObject(
                    "Remote Shotgun Sequence Wrap Test");

            try
            {
                RemotePlayerShotgunEventReceiver receiver =
                    player.AddComponent<
                        RemotePlayerShotgunEventReceiver>();

                receiver.Configure(
                    9u);

                receiver.Enqueue(
                    7u,
                    CreateEvent(
                        9u,
                        uint.MaxValue));

                receiver.Enqueue(
                    7u,
                    CreateEvent(
                        9u,
                        0u));

                Assert.That(
                    receiver.PendingCount,
                    Is.EqualTo(2),
                    "VolleySequence must accept wrapping " +
                    "from uint.MaxValue to zero.");

                Assert.Throws<ArgumentException>(
                    () =>
                    {
                        receiver.Enqueue(
                            7u,
                            CreateEvent(
                                9u,
                                0x80000000u));
                    });

                Assert.That(
                    receiver.PendingCount,
                    Is.EqualTo(2),
                    "An ambiguous half-range sequence " +
                    "must not enter the Gameplay queue.");

                receiver.Enqueue(
                    7u,
                    CreateEvent(
                        9u,
                        1u));

                Assert.That(
                    receiver.PendingCount,
                    Is.EqualTo(3),
                    "Rejecting an ambiguous sequence must " +
                    "not block the next valid event.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(
                    player);
            }
        }

        private static PlayerShotgunEvent CreateEvent(
            uint playerId,
            uint volleySequence)
        {
            return new PlayerShotgunEvent(
                playerId,
                volleySequence,
                1.0f,
                2.0f,
                1.0f,
                0.0f,
                5u,
                40.0f,
                0.75f);
        }
    }
}