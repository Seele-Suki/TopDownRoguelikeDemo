using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace TopDownRoguelike.Tests.EditMode
{
    public sealed class ShotgunAuthorityTests
    {
        private const string BootstrapTypeName =
            "TopDownRoguelike.Gameplay.Networking." +
            "NetworkGameBootstrap";

        private const string ShotgunSkillTypeName =
            "TopDownRoguelike.Gameplay.Characters." +
            "ShotgunSkill";

        private const string RemoteInputTypeName =
            "TopDownRoguelike.Gameplay.Networking." +
            "RemotePlayerInputSource";

        private const string LocalInputTypeName =
            "TopDownRoguelike.Gameplay.Characters." +
            "LocalPlayerInputSource";

        private const string InputContractTypeName =
            "TopDownRoguelike.Gameplay.Characters." +
            "IPlayerInputSource";

        [Test]
        public void TryEnableRemoteSimulation_ConfiguresShotgunAuthority()
        {
            Type bootstrapType =
                FindType(BootstrapTypeName);

            Type shotgunSkillType =
                FindType(ShotgunSkillTypeName);

            GameObject player =
                new GameObject(
                    "Host Remote Shotgun Authority");

            player.SetActive(false);

            try
            {
                player.AddComponent<Rigidbody2D>();

                Component inputSource =
                    AddComponent(
                        player,
                        RemoteInputTypeName);

                AddComponent(
                    player,
                    "PlayerController");

                AddComponent(
                    player,
                    "TopDownRoguelike.Gameplay.Weapons." +
                    "PlayerShooter");

                Component shotgunSkill =
                    player.AddComponent(
                        shotgunSkillType);

                ((Behaviour)shotgunSkill).enabled =
                    false;

                bool configured =
                    InvokePrivateStatic<bool>(
                        bootstrapType,
                        "TryEnableRemoteSimulation",
                        player);

                Assert.That(
                    configured,
                    Is.True);

                Assert.That(
                    ((Behaviour)shotgunSkill).enabled,
                    Is.True,
                    "The host must enable ShotgunSkill " +
                    "for its remote client player.");

                FieldInfo inputSourceField =
                    shotgunSkillType.GetField(
                        "inputSource",
                        BindingFlags.Instance |
                        BindingFlags.NonPublic);

                Assert.That(
                    inputSourceField,
                    Is.Not.Null);

                Assert.That(
                    inputSourceField.GetValue(
                        shotgunSkill),
                    Is.SameAs(inputSource),
                    "The authoritative ShotgunSkill must " +
                    "use RemotePlayerInputSource.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(
                    player);
            }
        }

        [Test]
        public void ConfigureClientPlayers_DisablesLocalShotgunAuthority()
        {
            Type bootstrapType =
                FindType(BootstrapTypeName);

            Type shotgunSkillType =
                FindType(ShotgunSkillTypeName);

            Type shotgunDataType =
                FindType(
                    "TopDownRoguelike.Gameplay.Skills." +
                    "ShotgunData");

            var bootstrapObject =
                new GameObject(
                    "Client Shotgun Authority Bootstrap");

            var scenePlayer =
                new GameObject(
                    "Client Scene Player");

            var hostSpawnObject =
                new GameObject(
                    "Host Spawn Point");

            var clientSpawnObject =
                new GameObject(
                    "Client Spawn Point");

            var poolObject =
                new GameObject(
                    "Shotgun Pool Test");

            var firePointObject =
                new GameObject(
                    "Shotgun Fire Point Test");

            GameObject remotePlayer = null;
            ScriptableObject shotgunData = null;

            bootstrapObject.SetActive(false);
            scenePlayer.SetActive(false);
            poolObject.SetActive(false);

            try
            {
                scenePlayer.AddComponent<Rigidbody2D>();

                AddComponent(
                    scenePlayer,
                    LocalInputTypeName);

                AddComponent(
                    scenePlayer,
                    "PlayerController");

                AddComponent(
                    scenePlayer,
                    "TopDownRoguelike.Gameplay.Characters." +
                    "PlayerHealth");

                Component shotgunSkill =
                    scenePlayer.AddComponent(
                        shotgunSkillType);

                Component projectilePool =
                    AddComponent(
                        poolObject,
                        "TopDownRoguelike.Gameplay.Weapons." +
                        "ProjectilePool");

                shotgunData =
                    ScriptableObject.CreateInstance(
                        shotgunDataType);

                SetPrivateField(
                    shotgunSkill,
                    "shotgunData",
                    shotgunData);

                SetPrivateField(
                    shotgunSkill,
                    "projectilePool",
                    projectilePool);

                SetPrivateField(
                    shotgunSkill,
                    "firePoint",
                    firePointObject.transform);

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

                remotePlayer =
                    bootstrapType
                        .GetProperty("RemotePlayer")
                        .GetValue(bootstrap)
                        as GameObject;

                Assert.That(
                    remotePlayer,
                    Is.Not.Null,
                    "Client player configuration must succeed.");

                Assert.That(
                    ((Behaviour)shotgunSkill).enabled,
                    Is.False,
                    "A joining client must not execute " +
                    "ShotgunSkill locally.");
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
                    poolObject);

                UnityEngine.Object.DestroyImmediate(
                    firePointObject);

                if (shotgunData != null)
                {
                    UnityEngine.Object.DestroyImmediate(
                        shotgunData);
                }
            }
        }

        [Test]
        public void CooldownRejectedRequest_DoesNotReplayWhenReady()
        {
            var player =
                new GameObject(
                    "Shotgun Cooldown Authority");

            player.SetActive(false);

            try
            {
                Component inputSource =
                    AddComponent(
                        player,
                        RemoteInputTypeName);

                Component shotgunSkill =
                    AddComponent(
                        player,
                        ShotgunSkillTypeName);

                ApplyInputState(
                    inputSource,
                    Vector2.zero,
                    Vector2.right,
                    0u,
                    100u);

                SetInputSource(
                    shotgunSkill,
                    inputSource);

                SetPrivateField(
                    shotgunSkill,
                    "cooldownRemaining",
                    100f);

                ApplyInputState(
                    inputSource,
                    Vector2.zero,
                    Vector2.right,
                    0u,
                    101u);

                InvokePrivate(
                    shotgunSkill,
                    "Update");

                Assert.That(
                    ReadPrivateUInt(
                        shotgunSkill,
                        "lastProcessedShotgunRequestSequence"),
                    Is.EqualTo(101u),
                    "A request received during cooldown " +
                    "must be consumed immediately.");

                SetPrivateField(
                    shotgunSkill,
                    "cooldownRemaining",
                    0f);

                InvokePrivate(
                    shotgunSkill,
                    "Update");

                Assert.That(
                    ReadPrivateUInt(
                        shotgunSkill,
                        "lastProcessedShotgunRequestSequence"),
                    Is.EqualTo(101u),
                    "The consumed request must not replay " +
                    "after cooldown.");

                Assert.That(
                    ReadPrivateFloat(
                        shotgunSkill,
                        "cooldownRemaining"),
                    Is.EqualTo(0f),
                    "No new request means no delayed volley.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(
                    player);
            }
        }

        private static Component AddComponent(
            GameObject gameObject,
            string typeName)
        {
            Type componentType =
                FindType(typeName);

            Assert.That(
                componentType,
                Is.Not.Null,
                $"{typeName} must exist.");

            return gameObject.AddComponent(
                componentType);
        }

        private static void ApplyInputState(
            Component inputSource,
            Vector2 moveDirection,
            Vector2 aimDirection,
            uint dashRequestSequence,
            uint shotgunRequestSequence)
        {
            MethodInfo method =
                inputSource.GetType().GetMethod(
                    "ApplyInputState",
                    new[]
                    {
                        typeof(Vector2),
                        typeof(Vector2),
                        typeof(bool),
                        typeof(uint),
                        typeof(uint)
                    });

            Assert.That(
                method,
                Is.Not.Null);

            method.Invoke(
                inputSource,
                new object[]
                {
                    moveDirection,
                    aimDirection,
                    false,
                    dashRequestSequence,
                    shotgunRequestSequence
                });
        }

        private static void SetInputSource(
            Component shotgunSkill,
            Component inputSource)
        {
            Type inputContractType =
                FindType(InputContractTypeName);

            MethodInfo method =
                shotgunSkill.GetType().GetMethod(
                    "SetInputSource",
                    BindingFlags.Instance |
                    BindingFlags.Public,
                    null,
                    new[]
                    {
                        inputContractType
                    },
                    null);

            Assert.That(
                method,
                Is.Not.Null);

            method.Invoke(
                shotgunSkill,
                new object[]
                {
                    inputSource
                });
        }

        private static uint ReadPrivateUInt(
            object target,
            string fieldName)
        {
            return (uint)GetPrivateField(
                target,
                fieldName).GetValue(target);
        }

        private static float ReadPrivateFloat(
            object target,
            string fieldName)
        {
            return (float)GetPrivateField(
                target,
                fieldName).GetValue(target);
        }

        private static void SetPrivateField(
            object target,
            string fieldName,
            object value)
        {
            GetPrivateField(
                target,
                fieldName).SetValue(
                    target,
                    value);
        }

        private static FieldInfo GetPrivateField(
            object target,
            string fieldName)
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

            return field;
        }

        private static void InvokePrivate(
            object target,
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

        private static T InvokePrivateStatic<T>(
            Type targetType,
            string methodName,
            params object[] arguments)
        {
            MethodInfo method =
                targetType.GetMethod(
                    methodName,
                    BindingFlags.Static |
                    BindingFlags.NonPublic);

            Assert.That(
                method,
                Is.Not.Null,
                $"{methodName} must exist.");

            return (T)method.Invoke(
                null,
                arguments);
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