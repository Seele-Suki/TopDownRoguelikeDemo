using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TopDownRoguelike.Networking.Gameplay;
using TopDownRoguelike.Networking.Protocol;
using UnityEngine;

namespace TopDownRoguelike.Tests.EditMode
{
    public sealed class HostPlayerStatePublisherTests
    {
        private const string PublisherTypeName =
            "TopDownRoguelike.Gameplay.Networking." +
            "HostPlayerStatePublisher";

        private const string PlayerControllerTypeName =
            "PlayerController";

        private const string RemoteInputSourceTypeName =
            "TopDownRoguelike.Gameplay.Networking." +
            "RemotePlayerInputSource";

        [Test]
        public void Advance_PublishesTwoRegisteredPlayersAtTwentyHertz()
        {
            Type publisherType =
                FindType(PublisherTypeName);

            Assert.That(
                publisherType,
                Is.Not.Null,
                "HostPlayerStatePublisher must exist.");

            Type controllerType =
                FindType(PlayerControllerTypeName);

            Type remoteInputSourceType =
                FindType(RemoteInputSourceTypeName);

            Assert.That(
                remoteInputSourceType,
                Is.Not.Null,
                "RemotePlayerInputSource must exist.");

            Assert.That(
                controllerType,
                Is.Not.Null,
                "PlayerController must exist.");

            var publisherObject =
                new GameObject("Host State Publisher Test");

            var firstPlayer =
                new GameObject("First Player");

            var secondPlayer =
                new GameObject("Second Player");

            publisherObject.SetActive(false);
            firstPlayer.SetActive(false);
            secondPlayer.SetActive(false);

            try
            {
                firstPlayer.transform.position =
                    new Vector3(1.5f, -2f, 0f);

                secondPlayer.transform.position =
                    new Vector3(-3f, 4.25f, 0f);

                Component firstController =
                    firstPlayer.AddComponent(controllerType);

                Component secondController =
                    secondPlayer.AddComponent(controllerType);

                Component secondInputSource =
                    secondPlayer.AddComponent(
                        remoteInputSourceType);

                SetPrivateField(
                    firstController,
                    "aimDirection",
                    Vector2.right);

                SetPrivateField(
                    secondController,
                    "aimDirection",
                    Vector2.up);

                MethodInfo applyInputMethod =
                    remoteInputSourceType.GetMethod(
                        "ApplyInputWithFireState",
                        new[]
                {
                    typeof(Vector2),
                    typeof(Vector2),
                    typeof(bool)
                });

                Assert.That(
                    applyInputMethod,
                    Is.Not.Null,
                    "ApplyInputWithFireState must exist.");

                applyInputMethod.Invoke(
                    secondInputSource,
                    new object[]
                    {
                        Vector2.zero,
                        Vector2.up,
                        true
                    });

                var registry =
                    new NetworkPlayerRegistry();

                Assert.That(
                    registry.TryRegister(11u, firstPlayer),
                    Is.True);

                Assert.That(
                    registry.TryRegister(22u, secondPlayer),
                    Is.True);

                Component publisher =
                    publisherObject.AddComponent(
                        publisherType);

                var sentSnapshots =
                    new List<PlayerStateSnapshotPayload>();

                Action<PlayerStateSnapshotPayload> sender =
                    sentSnapshots.Add;

                MethodInfo configureMethod =
                    publisherType.GetMethod(
                        "Configure",
                        BindingFlags.Instance |
                        BindingFlags.Public);

                Assert.That(
                    configureMethod,
                    Is.Not.Null,
                    "Configure must exist.");

                configureMethod.Invoke(
                    publisher,
                    new object[]
                    {
                        registry,
                        new uint[] { 11u, 22u },
                        sender
                    });

                InvokeAdvance(publisher, 0.049f);

                Assert.That(
                    sentSnapshots.Count,
                    Is.EqualTo(0));

                InvokeAdvance(publisher, 0.002f);

                Assert.That(
                    sentSnapshots.Count,
                    Is.EqualTo(1));

                PlayerStateSnapshotPayload snapshot =
                    sentSnapshots[0];

                Assert.That(
                    snapshot.Players.Count,
                    Is.EqualTo(2));

                AssertPlayerState(
                    snapshot.Players[0],
                    11u,
                    1.5f,
                    -2f,
                    1f,
                    0f);

                AssertPlayerState(
                    snapshot.Players[1],
                    22u,
                    -3f,
                    4.25f,
                    0f,
                    1f);

                Assert.That(
                    snapshot.Players[1].FireHeld,
                    Is.True,
                    "Host snapshot must include FireHeld.");

                InvokeAdvance(publisher, 0.051f);

                Assert.That(
                    sentSnapshots.Count,
                    Is.EqualTo(2));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(
                    publisherObject);

                UnityEngine.Object.DestroyImmediate(
                    firstPlayer);

                UnityEngine.Object.DestroyImmediate(
                    secondPlayer);
            }
        }

        [Test]
        public void Advance_WhenRegisteredPlayerHasNoController_DoesNotSendStaleSnapshot()
        {
            Type publisherType =
                FindType(PublisherTypeName);

            Type controllerType =
                FindType(PlayerControllerTypeName);

            Assert.That(publisherType, Is.Not.Null);
            Assert.That(controllerType, Is.Not.Null);

            var publisherObject =
                new GameObject(
                    "Invalid Host State Publisher Test");

            var firstPlayer =
                new GameObject("First Player");

            var secondPlayer =
                new GameObject("Second Player");

            publisherObject.SetActive(false);
            firstPlayer.SetActive(false);
            secondPlayer.SetActive(false);

            try
            {
                firstPlayer.AddComponent(controllerType);

                var registry =
                    new NetworkPlayerRegistry();

                Assert.That(
                    registry.TryRegister(
                        11u,
                        firstPlayer),
                    Is.True);

                Assert.That(
                    registry.TryRegister(
                        22u,
                        secondPlayer),
                    Is.True);

                Component publisher =
                    publisherObject.AddComponent(
                        publisherType);

                var sentSnapshots =
                    new List<PlayerStateSnapshotPayload>();

                MethodInfo configureMethod =
                    publisherType.GetMethod(
                        "Configure",
                        BindingFlags.Instance |
                        BindingFlags.Public);

                Assert.That(
                    configureMethod,
                    Is.Not.Null);

                configureMethod.Invoke(
                    publisher,
                    new object[]
                    {
                registry,
                new uint[] { 11u, 22u },
                (Action<PlayerStateSnapshotPayload>)
                    sentSnapshots.Add
                    });

                InvokeAdvance(
                    publisher,
                    0.05f);

                Assert.That(
                    sentSnapshots.Count,
                    Is.EqualTo(0),
                    "An incomplete player set must not " +
                    "produce a snapshot.");

                Component secondController =
                    secondPlayer.AddComponent(
                        controllerType);

                SetPrivateField(
                    secondController,
                    "aimDirection",
                    Vector2.up);

                InvokeAdvance(
                    publisher,
                    0.001f);

                Assert.That(
                    sentSnapshots.Count,
                    Is.EqualTo(0),
                    "The invalid frame must reset the " +
                    "publisher timer.");

                InvokeAdvance(
                    publisher,
                    0.051f);

                Assert.That(
                    sentSnapshots.Count,
                    Is.EqualTo(1));

                Assert.That(
                    sentSnapshots[0].Players.Count,
                    Is.EqualTo(2));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(
                    publisherObject);

                UnityEngine.Object.DestroyImmediate(
                    firstPlayer);

                UnityEngine.Object.DestroyImmediate(
                    secondPlayer);
            }
        }

        [Test]
        public void RemotePlayerInterpolator_AppliesLatestStateSmoothly()
        {
            Type interpolatorType =
                FindType(
                    "TopDownRoguelike.Gameplay.Networking." +
                    "RemotePlayerInterpolator");

            Assert.That(
                interpolatorType,
                Is.Not.Null,
                "RemotePlayerInterpolator must exist.");

            var remotePlayer =
                new GameObject(
                    "Remote Player Interpolator Test");

            remotePlayer.SetActive(false);

            try
            {
                Component interpolator =
                    remotePlayer.AddComponent(
                        interpolatorType);

                MethodInfo configureMethod =
                    interpolatorType.GetMethod(
                        "Configure",
                        BindingFlags.Instance |
                        BindingFlags.Public);

                MethodInfo applySnapshotMethod =
                    interpolatorType.GetMethod(
                        "ApplySnapshot",
                        BindingFlags.Instance |
                        BindingFlags.Public);

                Assert.That(
                    configureMethod,
                    Is.Not.Null,
                    "Configure must exist.");

                Assert.That(
                    applySnapshotMethod,
                    Is.Not.Null,
                    "ApplySnapshot must exist.");

                configureMethod.Invoke(
                    interpolator,
                    new object[]
                    {
                22u
                    });

                var snapshot =
                    new PlayerStateSnapshotPayload(
                        new[]
                        {
                    new PlayerStateRecord(
                        11u,
                        100f,
                        100f,
                        0f,
                        1f),

                    new PlayerStateRecord(
                        22u,
                        10f,
                        4f,
                        1f,
                        0f)
                        });

                applySnapshotMethod.Invoke(
                    interpolator,
                    new object[]
                    {
                snapshot
                    });

                MethodInfo advanceMethod =
                    interpolatorType.GetMethod(
                        "Advance",
                        BindingFlags.Instance |
                        BindingFlags.NonPublic);

                Assert.That(
                    advanceMethod,
                    Is.Not.Null,
                    "Advance must exist.");

                advanceMethod.Invoke(
                    interpolator,
                    new object[]
                    {
                0.025f
                    });

                Assert.That(
                    remotePlayer.transform.position.x,
                    Is.EqualTo(5f).Within(0.01f));

                Assert.That(
                    remotePlayer.transform.position.y,
                    Is.EqualTo(2f).Within(0.01f));

                Assert.That(
                    remotePlayer.transform.eulerAngles.z,
                    Is.EqualTo(0f).Within(0.01f));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(
                    remotePlayer);
            }
        }

        [Test]
        public void RemotePlayerInterpolator_StoresFireHeldFromSnapshot()
        {
            Type interpolatorType =
                FindType(
                    "TopDownRoguelike.Gameplay.Networking." +
                    "RemotePlayerInterpolator");

            Assert.That(
                interpolatorType,
                Is.Not.Null,
                "RemotePlayerInterpolator must exist.");

            GameObject remotePlayer =
                new GameObject(
                    "Remote Player Fire State Test");

            try
            {
                Component interpolator =
                    remotePlayer.AddComponent(
                        interpolatorType);

                MethodInfo configureMethod =
                    interpolatorType.GetMethod(
                        "Configure",
                        new[]
                        {
                    typeof(uint)
                        });

                Assert.That(
                    configureMethod,
                    Is.Not.Null,
                    "Configure(uint) must exist.");

                configureMethod.Invoke(
                    interpolator,
                    new object[]
                    {
                22u
                    });

                var snapshot =
                    new PlayerStateSnapshotPayload(
                        new[]
                        {
                    new PlayerStateRecord(
                        22u,
                        1f,
                        2f,
                        1f,
                        0f,
                        true)
                        });

                MethodInfo applySnapshotMethod =
                    interpolatorType.GetMethod(
                        "ApplySnapshot",
                        new[]
                        {
                    typeof(PlayerStateSnapshotPayload)
                        });

                Assert.That(
                    applySnapshotMethod,
                    Is.Not.Null,
                    "ApplySnapshot must exist.");

                applySnapshotMethod.Invoke(
                    interpolator,
                    new object[]
                    {
                snapshot
                    });

                PropertyInfo fireHeldProperty =
                    interpolatorType.GetProperty(
                        "IsFireHeld");

                Assert.That(
                    fireHeldProperty,
                    Is.Not.Null,
                    "RemotePlayerInterpolator must expose IsFireHeld.");

                Assert.That(
                    (bool)fireHeldProperty.GetValue(
                        interpolator),
                    Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(
                    remotePlayer);
            }
        }

        [Test]
        public void RemotePlayerInterpolator_ReceivesFireHeldState()
        {
            Type interpolatorType =
                FindType(
                    "TopDownRoguelike.Gameplay.Networking." +
                    "RemotePlayerInterpolator");

            Assert.That(
                interpolatorType,
                Is.Not.Null);

            PropertyInfo fireHeldProperty =
                interpolatorType.GetProperty(
                    "IsFireHeld");

            Assert.That(
                fireHeldProperty,
                Is.Not.Null,
                "RemotePlayerInterpolator must expose IsFireHeld.");
        }

        private static void AssertPlayerState(
            PlayerStateRecord state,
            uint playerId,
            float positionX,
            float positionY,
            float aimX,
            float aimY)
        {
            Assert.That(state.PlayerId, Is.EqualTo(playerId));

            Assert.That(
                state.PositionX,
                Is.EqualTo(positionX).Within(0.001f));

            Assert.That(
                state.PositionY,
                Is.EqualTo(positionY).Within(0.001f));

            Assert.That(
                state.AimX,
                Is.EqualTo(aimX).Within(0.001f));

            Assert.That(
                state.AimY,
                Is.EqualTo(aimY).Within(0.001f));
        }

        private static void InvokeAdvance(
            Component publisher,
            float deltaTime)
        {
            MethodInfo advanceMethod =
                publisher.GetType().GetMethod(
                    "Advance",
                    BindingFlags.Instance |
                    BindingFlags.NonPublic);

            Assert.That(
                advanceMethod,
                Is.Not.Null,
                "Advance must exist.");

            advanceMethod.Invoke(
                publisher,
                new object[] { deltaTime });
        }

        private static void SetPrivateField(
            Component target,
            string fieldName,
            object value)
        {
            FieldInfo field =
                target.GetType().GetField(
                    fieldName,
                    BindingFlags.Instance |
                    BindingFlags.NonPublic);

            Assert.That(field, Is.Not.Null);

            field.SetValue(target, value);
        }

        private static Type FindType(
            string fullTypeName)
        {
            foreach (Assembly assembly in
                AppDomain.CurrentDomain.GetAssemblies())
            {
                Type result =
                    assembly.GetType(
                        fullTypeName,
                        false);

                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }
    }
}