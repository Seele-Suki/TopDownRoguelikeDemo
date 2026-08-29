using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TopDownRoguelike.Networking.Gameplay;
using TopDownRoguelike.Networking.Protocol;
using UnityEngine;

namespace TopDownRoguelike.Tests.EditMode
{
    public sealed class DashStateSynchronizationTests
    {
        private const string PublisherTypeName =
            "TopDownRoguelike.Gameplay.Networking." +
            "HostPlayerStatePublisher";

        private const string InterpolatorTypeName =
            "TopDownRoguelike.Gameplay.Networking." +
            "RemotePlayerInterpolator";

        private const string ReconcilerTypeName =
            "TopDownRoguelike.Gameplay.Networking." +
            "LocalPlayerDashReconciler";

        private const string BootstrapTypeName =
            "TopDownRoguelike.Gameplay.Networking." +
            "NetworkGameBootstrap";

        [Test]
        public void PlayerStateRecord_EncodesAuthoritativeDashFlag()
        {
            PlayerStateRecord state =
                CreateState(
                    22u,
                    1.5f,
                    -2f,
                    0f,
                    1f,
                    false,
                    true);

            byte[] encoded =
                PlayerStateSnapshotCodec.Encode(
                    new PlayerStateSnapshotPayload(
                        new[]
                        {
                            state
                        }));

            Assert.That(
                encoded.Length,
                Is.EqualTo(28));

            Assert.That(
                encoded[27],
                Is.EqualTo(0x02),
                "State flag bit 1 must represent IsDashing.");

            PlayerStateSnapshotPayload decoded =
                PlayerStateSnapshotCodec.Decode(
                    encoded);

            Assert.That(
                ReadIsDashing(
                    decoded.Players[0]),
                Is.True);
        }

        [Test]
        public void HostPublisher_IncludesDashSkillState()
        {
            Type publisherType =
                FindType(PublisherTypeName);

            Type controllerType =
                FindType("PlayerController");

            Type dashSkillType =
                FindType("DashSkill");

            Assert.That(publisherType, Is.Not.Null);
            Assert.That(controllerType, Is.Not.Null);
            Assert.That(dashSkillType, Is.Not.Null);

            var publisherObject =
                new GameObject(
                    "Dash State Publisher Test");

            var player =
                new GameObject(
                    "Authoritative Dashing Player");

            publisherObject.SetActive(false);
            player.SetActive(false);

            try
            {
                player.transform.position =
                    new Vector3(6f, -3f, 0f);

                player.AddComponent(
                    controllerType);

                Component dashSkill =
                    player.AddComponent(
                        dashSkillType);

                SetDashSkillState(
                    dashSkill,
                    true);

                var registry =
                    new NetworkPlayerRegistry();

                Assert.That(
                    registry.TryRegister(
                        22u,
                        player),
                    Is.True);

                Component publisher =
                    publisherObject.AddComponent(
                        publisherType);

                var snapshots =
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
                        new uint[]
                        {
                            22u
                        },
                        new Action<PlayerStateSnapshotPayload>(
                            snapshots.Add)
                    });

                InvokePrivate(
                    publisher,
                    "Advance",
                    0.051f);

                Assert.That(
                    snapshots.Count,
                    Is.EqualTo(1));

                Assert.That(
                    ReadIsDashing(
                        snapshots[0].Players[0]),
                    Is.True,
                    "Host snapshot must include " +
                    "DashSkill.IsDashing.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(
                    publisherObject);

                UnityEngine.Object.DestroyImmediate(
                    player);
            }
        }

        [Test]
        public void RemoteInterpolator_StoresDashState()
        {
            Type interpolatorType =
                FindType(InterpolatorTypeName);

            Assert.That(
                interpolatorType,
                Is.Not.Null);

            var remotePlayer =
                new GameObject(
                    "Remote Dash State Test");

            remotePlayer.SetActive(false);

            try
            {
                Component interpolator =
                    remotePlayer.AddComponent(
                        interpolatorType);

                InvokePublic(
                    interpolator,
                    "Configure",
                    11u);

                var snapshot =
                    new PlayerStateSnapshotPayload(
                        new[]
                        {
                            CreateState(
                                11u,
                                4f,
                                2f,
                                1f,
                                0f,
                                false,
                                true)
                        });

                InvokePublic(
                    interpolator,
                    "ApplySnapshot",
                    snapshot);

                PropertyInfo isDashingProperty =
                    interpolatorType.GetProperty(
                        "IsDashing",
                        BindingFlags.Instance |
                        BindingFlags.Public);

                Assert.That(
                    isDashingProperty,
                    Is.Not.Null,
                    "RemotePlayerInterpolator must expose " +
                    "the authoritative dash state.");

                Assert.That(
                    (bool)isDashingProperty.GetValue(
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
        public void LocalReconciler_AppliesDashAndFinalState()
        {
            Type reconcilerType =
                FindType(ReconcilerTypeName);

            Type controllerType =
                FindType("PlayerController");

            Assert.That(
                reconcilerType,
                Is.Not.Null,
                "LocalPlayerDashReconciler must exist.");

            Assert.That(
                controllerType,
                Is.Not.Null);

            var player =
                new GameObject(
                    "Local Dash Reconciliation Test");

            player.SetActive(false);

            try
            {
                player.AddComponent<Rigidbody2D>();

                Behaviour controller =
                    player.AddComponent(
                        controllerType) as Behaviour;

                Component reconciler =
                    player.AddComponent(
                        reconcilerType);

                InvokePublic(
                    reconciler,
                    "Configure",
                    22u);

                InvokePublic(
                    reconciler,
                    "ApplySnapshot",
                    new PlayerStateSnapshotPayload(
                        new[]
                        {
                            CreateState(
                                22u,
                                10f,
                                4f,
                                0f,
                                1f,
                                false,
                                true)
                        }));

                Assert.That(
                    controller.enabled,
                    Is.False,
                    "Local movement must pause while the " +
                    "Host performs the authoritative dash.");

                Assert.That(
                    ReadPublicBool(
                        reconciler,
                        "IsDashing"),
                    Is.True);

                InvokePrivate(
                    reconciler,
                    "Advance",
                    0.05f);

                Assert.That(
                    player.transform.position.x,
                    Is.EqualTo(10f).Within(0.01f));

                Assert.That(
                    player.transform.position.y,
                    Is.EqualTo(4f).Within(0.01f));

                Assert.That(
                    player.transform.eulerAngles.z,
                    Is.EqualTo(90f).Within(0.01f));

                InvokePublic(
                    reconciler,
                    "ApplySnapshot",
                    new PlayerStateSnapshotPayload(
                        new[]
                        {
                            CreateState(
                                22u,
                                12f,
                                5f,
                                1f,
                                0f,
                                false,
                                false)
                        }));

                Assert.That(
                    ReadPublicBool(
                        reconciler,
                        "IsDashing"),
                    Is.False);

                Assert.That(
                    controller.enabled,
                    Is.True,
                    "Local movement must resume after the " +
                    "authoritative dash finishes.");

                Assert.That(
                    player.transform.position.x,
                    Is.EqualTo(12f).Within(0.01f));

                Assert.That(
                    player.transform.position.y,
                    Is.EqualTo(5f).Within(0.01f));

                Assert.That(
                    player.transform.eulerAngles.z,
                    Is.EqualTo(0f).Within(0.01f));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(
                    player);
            }
        }

        [Test]
        public void Bootstrap_RoutesSnapshotToLocalReconciler()
        {
            Type bootstrapType =
                FindType(BootstrapTypeName);

            Type reconcilerType =
                FindType(ReconcilerTypeName);

            Type controllerType =
                FindType("PlayerController");

            Assert.That(bootstrapType, Is.Not.Null);

            Assert.That(
                reconcilerType,
                Is.Not.Null,
                "LocalPlayerDashReconciler must exist.");

            Assert.That(controllerType, Is.Not.Null);

            var bootstrapObject =
                new GameObject(
                    "Local Dash Snapshot Bridge Test");

            var localPlayer =
                new GameObject(
                    "Local Client Player");

            bootstrapObject.SetActive(false);
            localPlayer.SetActive(false);

            try
            {
                localPlayer.AddComponent<Rigidbody2D>();

                Behaviour controller =
                    localPlayer.AddComponent(
                        controllerType) as Behaviour;

                Component reconciler =
                    localPlayer.AddComponent(
                        reconcilerType);

                InvokePublic(
                    reconciler,
                    "Configure",
                    22u);

                Component bootstrap =
                    bootstrapObject.AddComponent(
                        bootstrapType);

                SetPrivateField(
                    bootstrap,
                    "localPlayer",
                    localPlayer);

                InvokePrivate(
                    bootstrap,
                    "Awake");

                InvokePrivate(
                    bootstrap,
                    "HandleRemotePlayerStateSnapshot",
                    11u,
                    new PlayerStateSnapshotPayload(
                        new[]
                        {
                            CreateState(
                                11u,
                                -2f,
                                1f,
                                1f,
                                0f,
                                false,
                                false),

                            CreateState(
                                22u,
                                7f,
                                3f,
                                0f,
                                1f,
                                false,
                                true)
                        }));

                Assert.That(
                    ReadPublicBool(
                        reconciler,
                        "IsDashing"),
                    Is.True);

                Assert.That(
                    controller.enabled,
                    Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(
                    bootstrapObject);

                UnityEngine.Object.DestroyImmediate(
                    localPlayer);
            }
        }

        private static PlayerStateRecord CreateState(
            uint playerId,
            float positionX,
            float positionY,
            float aimX,
            float aimY,
            bool fireHeld,
            bool isDashing)
        {
            ConstructorInfo constructor =
                typeof(PlayerStateRecord).GetConstructor(
                    new[]
                    {
                        typeof(uint),
                        typeof(float),
                        typeof(float),
                        typeof(float),
                        typeof(float),
                        typeof(bool),
                        typeof(bool)
                    });

            Assert.That(
                constructor,
                Is.Not.Null,
                "PlayerStateRecord must accept IsDashing.");

            return (PlayerStateRecord)constructor.Invoke(
                new object[]
                {
                    playerId,
                    positionX,
                    positionY,
                    aimX,
                    aimY,
                    fireHeld,
                    isDashing
                });
        }

        private static bool ReadIsDashing(
            PlayerStateRecord state)
        {
            PropertyInfo property =
                typeof(PlayerStateRecord).GetProperty(
                    "IsDashing");

            Assert.That(
                property,
                Is.Not.Null,
                "PlayerStateRecord must expose IsDashing.");

            return (bool)property.GetValue(
                state);
        }

        private static bool ReadPublicBool(
            Component target,
            string propertyName)
        {
            PropertyInfo property =
                target.GetType().GetProperty(
                    propertyName,
                    BindingFlags.Instance |
                    BindingFlags.Public);

            Assert.That(
                property,
                Is.Not.Null,
                $"{propertyName} must exist.");

            return (bool)property.GetValue(
                target);
        }

        private static void SetDashSkillState(
            Component dashSkill,
            bool value)
        {
            PropertyInfo property =
                dashSkill.GetType().GetProperty(
                    "IsDashing",
                    BindingFlags.Instance |
                    BindingFlags.Public);

            MethodInfo setter =
                property?.GetSetMethod(true);

            Assert.That(
                setter,
                Is.Not.Null,
                "DashSkill.IsDashing must retain " +
                "its private setter.");

            setter.Invoke(
                dashSkill,
                new object[]
                {
                    value
                });
        }

        private static void InvokePublic(
            Component target,
            string methodName,
            params object[] arguments)
        {
            MethodInfo method =
                target.GetType().GetMethod(
                    methodName,
                    BindingFlags.Instance |
                    BindingFlags.Public);

            Assert.That(
                method,
                Is.Not.Null,
                $"{methodName} must exist.");

            method.Invoke(
                target,
                arguments);
        }

        private static void InvokePrivate(
            Component target,
            string methodName,
            params object[] arguments)
        {
            MethodInfo method =
                target.GetType().GetMethod(
                    methodName,
                    BindingFlags.Instance |
                    BindingFlags.NonPublic);

            Assert.That(
                method,
                Is.Not.Null,
                $"{methodName} must exist.");

            method.Invoke(
                target,
                arguments);
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

            Assert.That(
                field,
                Is.Not.Null,
                $"{fieldName} must exist.");

            field.SetValue(
                target,
                value);
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