using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace TopDownRoguelike.Tests.EditMode
{
    public sealed class DashAuthorityTests
    {
        private const string BootstrapTypeName =
            "TopDownRoguelike.Gameplay.Networking." +
            "NetworkGameBootstrap";

        private const string RemoteInputSourceTypeName =
            "TopDownRoguelike.Gameplay.Networking." +
            "RemotePlayerInputSource";

        private const string LocalInputSourceTypeName =
            "TopDownRoguelike.Gameplay.Characters." +
            "LocalPlayerInputSource";

        private const string PlayerHealthTypeName =
            "TopDownRoguelike.Gameplay.Characters." +
            "PlayerHealth";

        private const string PlayerShooterTypeName =
            "TopDownRoguelike.Gameplay.Weapons." +
            "PlayerShooter";

        private const string DashDataTypeName =
            "TopDownRoguelike.Gameplay.Skills." +
            "DashData";

        [Test]
        public void TryEnableRemoteSimulation_EnablesHostDashAuthority()
        {
            Type bootstrapType =
                FindType(BootstrapTypeName);

            Type dashSkillType =
                FindType("DashSkill");

            CreatePlayer(
                "Host Remote Client Test",
                out GameObject player,
                out Component dashSkill,
                out ScriptableObject dashData);

            try
            {
                InvokePrivateStatic(
                    bootstrapType,
                    "DisableLocalControl",
                    player);

                Assert.That(
                    ((Behaviour)dashSkill).enabled,
                    Is.False,
                    "Remote control must start disabled.");

                bool configured =
                    (bool)InvokePrivateStatic(
                        bootstrapType,
                        "TryEnableRemoteSimulation",
                        player);

                Assert.That(
                    configured,
                    Is.True);

                Type remoteInputSourceType =
                    FindType(RemoteInputSourceTypeName);

                Component remoteInput =
                    player.GetComponent(
                        remoteInputSourceType);

                Assert.That(
                    remoteInput,
                    Is.Not.Null);

                Assert.That(
                    ((Behaviour)dashSkill).enabled,
                    Is.True,
                    "The host must enable DashSkill for " +
                    "the remote client player.");

                FieldInfo inputSourceField =
                    dashSkillType.GetField(
                        "inputSource",
                        BindingFlags.Instance |
                        BindingFlags.NonPublic);

                Assert.That(
                    inputSourceField,
                    Is.Not.Null);

                Assert.That(
                    inputSourceField.GetValue(
                        dashSkill),
                    Is.SameAs(remoteInput),
                    "Host DashSkill must consume the same " +
                    "RemotePlayerInputSource as movement " +
                    "and shooting.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(
                    player);

                UnityEngine.Object.DestroyImmediate(
                    dashData);
            }
        }

        [Test]
        public void ConfigureClientPlayers_DisablesLocalDashAuthority()
        {
            Type bootstrapType =
                FindType(BootstrapTypeName);

            Type reconcilerType =
                FindType(
                    "TopDownRoguelike.Gameplay.Networking." +
                    "LocalPlayerDashReconciler");

            Assert.That(
                reconcilerType,
                Is.Not.Null,
                "Client configuration must provide a " +
                "LocalPlayerDashReconciler.");

            CreatePlayer(
                            "Client Local Player Test",
                out GameObject scenePlayer,
                out Component localDashSkill,
                out ScriptableObject dashData);

            var bootstrapObject =
                new GameObject(
                    "Client Dash Bootstrap Test");

            var hostSpawnObject =
                new GameObject(
                    "Host Spawn Point");

            var clientSpawnObject =
                new GameObject(
                    "Client Spawn Point");

            bootstrapObject.SetActive(false);

            hostSpawnObject.transform.position =
                new Vector3(-2f, 0f, 0f);

            clientSpawnObject.transform.position =
                new Vector3(2f, 0f, 0f);

            GameObject remotePlayer = null;

            try
            {
                Component bootstrap =
                    bootstrapObject.AddComponent(
                        bootstrapType);

                SetPrivateField(
                    bootstrap,
                    "scenePlayer",
                    scenePlayer);

                SetPrivateField(
                    bootstrap,
                    "hostSpawnPoint",
                    hostSpawnObject.transform);

                SetPrivateField(
                    bootstrap,
                    "clientSpawnPoint",
                    clientSpawnObject.transform);

                InvokePrivate(
                    bootstrap,
                    "Awake");

                InvokePrivate(
                    bootstrap,
                    "ConfigureClientPlayers",
                    22u,
                    11u);

                PropertyInfo remotePlayerProperty =
                    bootstrapType.GetProperty(
                        "RemotePlayer",
                        BindingFlags.Instance |
                        BindingFlags.Public);

                Assert.That(
                    remotePlayerProperty,
                    Is.Not.Null);

                remotePlayer =
                    remotePlayerProperty.GetValue(
                        bootstrap) as GameObject;

                Assert.That(
                    ((Behaviour)localDashSkill).enabled,
                    Is.False,
                    "A multiplayer client may publish a " +
                    "dash request but must not execute " +
                    "authoritative DashSkill locally.");

                Component reconciler =
                    scenePlayer.GetComponent(
                        reconcilerType);

                Assert.That(
                    reconciler,
                    Is.Not.Null,
                    "ConfigureClientPlayers must attach the " +
                    "local authoritative dash reconciler.");

                Assert.That(
                    ((Behaviour)reconciler).enabled,
                    Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(
                    bootstrapObject);

                if (remotePlayer != null)
                {
                    UnityEngine.Object.DestroyImmediate(
                        remotePlayer);
                }

                UnityEngine.Object.DestroyImmediate(
                    scenePlayer);

                UnityEngine.Object.DestroyImmediate(
                    hostSpawnObject);

                UnityEngine.Object.DestroyImmediate(
                    clientSpawnObject);

                UnityEngine.Object.DestroyImmediate(
                    dashData);
            }
        }

        private static void CreatePlayer(
            string objectName,
            out GameObject player,
            out Component dashSkill,
            out ScriptableObject dashData)
        {
            Type localInputSourceType =
                FindType(LocalInputSourceTypeName);

            Type playerHealthType =
                FindType(PlayerHealthTypeName);

            Type playerControllerType =
                FindType("PlayerController");

            Type playerShooterType =
                FindType(PlayerShooterTypeName);

            Type dashSkillType =
                FindType("DashSkill");

            Type dashDataType =
                FindType(DashDataTypeName);

            Assert.That(
                localInputSourceType,
                Is.Not.Null);

            Assert.That(
                playerHealthType,
                Is.Not.Null);

            Assert.That(
                playerControllerType,
                Is.Not.Null);

            Assert.That(
                playerShooterType,
                Is.Not.Null);

            Assert.That(
                dashSkillType,
                Is.Not.Null);

            Assert.That(
                dashDataType,
                Is.Not.Null);

            player =
                new GameObject(
                    objectName);

            player.SetActive(false);

            player.AddComponent<Rigidbody2D>();

            player.AddComponent(
                localInputSourceType);

            player.AddComponent(
                playerHealthType);

            player.AddComponent(
                playerControllerType);

            player.AddComponent(
                playerShooterType);

            dashSkill =
                player.AddComponent(
                    dashSkillType);

            dashData =
                ScriptableObject.CreateInstance(
                    dashDataType);

            SetPrivateField(
                dashSkill,
                "dashData",
                dashData);
        }

        private static object InvokePrivateStatic(
            Type targetType,
            string methodName,
            params object[] arguments)
        {
            Assert.That(
                targetType,
                Is.Not.Null);

            MethodInfo method =
                targetType.GetMethod(
                    methodName,
                    BindingFlags.Static |
                    BindingFlags.NonPublic);

            Assert.That(
                method,
                Is.Not.Null,
                $"{methodName} must exist.");

            return method.Invoke(
                null,
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