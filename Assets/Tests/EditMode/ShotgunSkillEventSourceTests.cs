using System;
using System.Reflection;
using NUnit.Framework;
using TopDownRoguelike.Gameplay.Networking;
using TopDownRoguelike.Networking.Protocol;
using UnityEngine;

namespace TopDownRoguelike.Tests.EditMode
{
    public sealed class ShotgunSkillEventSourceTests
    {
        private const string ShotgunSkillTypeName =
            "TopDownRoguelike.Gameplay.Characters.ShotgunSkill";

        private const string BootstrapTypeName =
            "TopDownRoguelike.Gameplay.Networking." +
            "NetworkGameBootstrap";

        [Test]
        public void ShotgunSkill_ExposesShotgunEventSourceBinding()
        {
            Type skillType =
                FindType(ShotgunSkillTypeName);

            Assert.That(
                skillType,
                Is.Not.Null,
                "ShotgunSkill must exist.");

            FieldInfo field =
                skillType.GetField(
                    "shotgunEventSource",
                    BindingFlags.Instance |
                    BindingFlags.NonPublic);

            Assert.That(
                field,
                Is.Not.Null,
                "ShotgunSkill must keep a shotgunEventSource field.");

            Assert.That(
                field.FieldType,
                Is.EqualTo(
                    typeof(PlayerShotgunEventSource)));

            MethodInfo method =
                skillType.GetMethod(
                    "SetShotgunEventSource",
                    BindingFlags.Instance |
                    BindingFlags.Public,
                    null,
                    new[]
                    {
                        typeof(PlayerShotgunEventSource)
                    },
                    null);

            Assert.That(
                method,
                Is.Not.Null,
                "ShotgunSkill must expose " +
                "SetShotgunEventSource(PlayerShotgunEventSource).");
        }

        [Test]
        public void
    TryConfigurePlayerShotgunPublisher_BindsEventSourceToShotgunSkill()
        {
            Type skillType =
                FindType(ShotgunSkillTypeName);

            Type bootstrapType =
                FindType(BootstrapTypeName);

            Assert.That(
                skillType,
                Is.Not.Null,
                "ShotgunSkill must exist.");

            Assert.That(
                bootstrapType,
                Is.Not.Null,
                "NetworkGameBootstrap must exist.");

            MethodInfo configureMethod =
                bootstrapType.GetMethod(
                    "TryConfigurePlayerShotgunPublisher",
                    BindingFlags.Static |
                    BindingFlags.NonPublic);

            Assert.That(
                configureMethod,
                Is.Not.Null,
                "NetworkGameBootstrap must expose its " +
                "player shotgun publisher configuration.");

            GameObject player =
                new GameObject(
                    "Shotgun Publisher Binding Test");

            player.SetActive(false);

            try
            {
                Component shotgunSkill =
                    player.AddComponent(
                        skillType);

                Action<PlayerShotgunEvent>
                    sendShotgunEvent =
                        shotgunEvent =>
                        {
                        };

                object result =
                    configureMethod.Invoke(
                        null,
                        new object[]
                        {
                    player,
                    7u,
                    sendShotgunEvent
                        });

                Assert.That(
                    result,
                    Is.EqualTo(true),
                    "Shotgun publisher configuration " +
                    "must succeed.");

                PlayerShotgunEventSource eventSource =
                    player.GetComponent<
                        PlayerShotgunEventSource>();

                Assert.That(
                    eventSource,
                    Is.Not.Null,
                    "The player must receive a shotgun " +
                    "event source.");

                FieldInfo eventSourceField =
                    skillType.GetField(
                        "shotgunEventSource",
                        BindingFlags.Instance |
                        BindingFlags.NonPublic);

                Assert.That(
                    eventSourceField,
                    Is.Not.Null);

                Assert.That(
                    eventSourceField.GetValue(
                        shotgunSkill),
                    Is.SameAs(eventSource),
                    "NetworkGameBootstrap must bind the " +
                    "created event source to ShotgunSkill.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(
                    player);
            }
        }

        [Test]
        public void
    FireShotgun_UsesUpgradedCountAndCooldownForVolleyAndEvent()
        {
            Type skillType =
                FindType(ShotgunSkillTypeName);

            Type dataType =
                FindType(
                    "TopDownRoguelike.Gameplay.Skills." +
                    "ShotgunData");

            Assert.That(
                skillType,
                Is.Not.Null);

            Assert.That(
                dataType,
                Is.Not.Null);

            GameObject player =
                new GameObject(
                    "Upgraded Shotgun Event Test");

            GameObject poolObject =
                new GameObject(
                    "Upgraded Shotgun Pool Test");

            GameObject projectilePrefabObject =
                new GameObject(
                    "Shotgun Projectile Prefab Test");

            player.SetActive(false);
            poolObject.SetActive(false);
            projectilePrefabObject.SetActive(false);

            ScriptableObject shotgunData =
                ScriptableObject.CreateInstance(
                    dataType);

            try
            {
                SetPrivateField(
                    shotgunData,
                    "maxProjectileCount",
                    11);

                SetPrivateField(
                    shotgunData,
                    "minCooldown",
                    1.5f);

                Component projectilePrefab =
                    AddComponent(
                        projectilePrefabObject,
                        "TopDownRoguelike.Gameplay.Weapons." +
                        "Projectile");

                Component projectilePool =
                    AddComponent(
                        poolObject,
                        "TopDownRoguelike.Gameplay.Weapons." +
                        "ProjectilePool");

                SetPrivateField(
                    projectilePool,
                    "projectilePrefab",
                    projectilePrefab);

                SetPrivateField(
                    projectilePool,
                    "initialSize",
                    0);

                Component inputSource =
                    AddComponent(
                        player,
                        "TopDownRoguelike.Gameplay.Networking." +
                        "RemotePlayerInputSource");

                Component shotgunSkill =
                    player.AddComponent(
                        skillType);

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
                    player.transform);

                SetPrivateField(
                    shotgunSkill,
                    "projectileCount",
                    5);

                SetPrivateField(
                    shotgunSkill,
                    "spreadAngle",
                    40f);

                SetPrivateField(
                    shotgunSkill,
                    "projectileDamage",
                    1);

                SetPrivateField(
                    shotgunSkill,
                    "penetrationCount",
                    0);

                SetPrivateField(
                    shotgunSkill,
                    "cooldown",
                    4f);

                SetPrivateField(
                    shotgunSkill,
                    "cooldownRemaining",
                    0f);

                MethodInfo applyInputState =
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
                    applyInputState,
                    Is.Not.Null);

                applyInputState.Invoke(
                    inputSource,
                    new object[]
                    {
                        Vector2.zero,
                        Vector2.right,
                        false,
                        0u,
                        0u
                    });

                MethodInfo setInputSource =
                    skillType.GetMethod(
                        "SetInputSource",
                        BindingFlags.Instance |
                        BindingFlags.Public);

                Assert.That(
                    setInputSource,
                    Is.Not.Null);

                setInputSource.Invoke(
                    shotgunSkill,
                    new object[]
                    {
                        inputSource
                    });

                PlayerShotgunEventSource eventSource =
                    player.AddComponent<
                        PlayerShotgunEventSource>();

                eventSource.Configure(7u);

                MethodInfo setEventSource =
                    skillType.GetMethod(
                        "SetShotgunEventSource",
                        BindingFlags.Instance |
                        BindingFlags.Public);

                Assert.That(
                    setEventSource,
                    Is.Not.Null);

                setEventSource.Invoke(
                    shotgunSkill,
                    new object[]
                    {
                        eventSource
                    });

                PlayerShotgunEvent receivedEvent =
                    null;

                eventSource.ShotgunGenerated +=
                    shotgunEvent =>
                    {
                        receivedEvent =
                            shotgunEvent;
                    };

                InvokePublic(
                    shotgunSkill,
                    "AddProjectileCount",
                    3);

                InvokePublic(
                    shotgunSkill,
                    "ReduceCooldown",
                    2f);

                InvokePrivate(
                    shotgunSkill,
                    "FireShotgun");

                Assert.That(
                    receivedEvent,
                    Is.Not.Null,
                    "A successful Host volley must publish " +
                    "one shotgun event.");

                Assert.That(
                    receivedEvent.ProjectileCount,
                    Is.EqualTo(8u),
                    "The event must contain the upgraded " +
                    "projectile count.");

                Assert.That(
                    receivedEvent.EffectiveCooldown,
                    Is.EqualTo(2f).Within(0.0001f),
                    "The event must contain the upgraded " +
                    "effective cooldown.");

                int activeProjectileCount =
                    0;

                foreach (Transform child
                    in poolObject.transform)
                {
                    if (child.gameObject.activeSelf)
                    {
                        activeProjectileCount++;
                    }
                }

                Assert.That(
                    activeProjectileCount,
                    Is.EqualTo(8),
                    "The Host Gameplay volley and network " +
                    "event must use the same projectile count.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(
                    player);

                UnityEngine.Object.DestroyImmediate(
                    poolObject);

                UnityEngine.Object.DestroyImmediate(
                    projectilePrefabObject);

                UnityEngine.Object.DestroyImmediate(
                    shotgunData);
            }
        }

        [Test]
        public void SetShotgunEventSource_RejectsNull()
        {
            Type skillType =
                FindType(ShotgunSkillTypeName);

            Assert.That(
                skillType,
                Is.Not.Null,
                "ShotgunSkill must exist.");

            GameObject player =
                new GameObject(
                    "Shotgun Skill Event Source Test");

            try
            {
                Component skill =
                    player.AddComponent(skillType);

                MethodInfo method =
                    skillType.GetMethod(
                        "SetShotgunEventSource",
                        BindingFlags.Instance |
                        BindingFlags.Public,
                        null,
                        new[]
                        {
                            typeof(PlayerShotgunEventSource)
                        },
                        null);

                Assert.That(
                    method,
                    Is.Not.Null);

                TargetInvocationException exception =
                    Assert.Throws<TargetInvocationException>(
                        () =>
                        {
                            method.Invoke(
                                skill,
                                new object[]
                                {
                                    null
                                });
                        });

                Assert.That(
                    exception.InnerException,
                    Is.TypeOf<ArgumentNullException>());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(
                    player);
            }
        }

        private static Component AddComponent(
    GameObject target,
    string typeName)
        {
            Type componentType =
                FindType(typeName);

            Assert.That(
                componentType,
                Is.Not.Null,
                $"{typeName} was not found.");

            return target.AddComponent(
                componentType);
        }

        private static void SetPrivateField(
            object target,
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
                $"{fieldName} was not found.");

            field.SetValue(
                target,
                value);
        }

        private static void InvokePublic(
            object target,
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
                $"{methodName} was not found.");

            method.Invoke(
                target,
                arguments);
        }

        private static void InvokePrivate(
            object target,
            string methodName)
        {
            MethodInfo method =
                target.GetType().GetMethod(
                    methodName,
                    BindingFlags.Instance |
                    BindingFlags.NonPublic);

            Assert.That(
                method,
                Is.Not.Null,
                $"{methodName} was not found.");

            method.Invoke(
                target,
                null);
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