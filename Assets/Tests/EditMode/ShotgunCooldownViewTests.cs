using System;
using System.Reflection;
using NUnit.Framework;
using TopDownRoguelike.Gameplay.Networking;
using TopDownRoguelike.Networking.Gameplay;
using TopDownRoguelike.Networking.Protocol;
using UnityEngine;
using UnityEngine.UI;

namespace TopDownRoguelike.Tests.EditMode
{
    public sealed class ShotgunCooldownViewTests
    {
        private const string BootstrapTypeName =
            "TopDownRoguelike.Gameplay.Networking." +
            "NetworkGameBootstrap";

        private const string ShotgunSkillTypeName =
            "TopDownRoguelike.Gameplay.Characters." +
            "ShotgunSkill";

        private const string CooldownViewTypeName =
            "TopDownRoguelike.Gameplay.UI." +
            "ShotgunCooldownView";

        [Test]
        public void
            LocalPlayerShotgunEventUpdatesCooldownView()
        {
            Type bootstrapType =
                FindType(BootstrapTypeName);

            Type shotgunSkillType =
                FindType(ShotgunSkillTypeName);

            Type cooldownViewType =
                FindType(CooldownViewTypeName);

            Assert.That(
                bootstrapType,
                Is.Not.Null,
                "NetworkGameBootstrap was not found.");

            Assert.That(
                shotgunSkillType,
                Is.Not.Null,
                "ShotgunSkill was not found.");

            Assert.That(
                cooldownViewType,
                Is.Not.Null,
                "ShotgunCooldownView was not found.");

            GameObject bootstrapObject =
                new GameObject(
                    "Cooldown Bootstrap Test");

            GameObject playerObject =
                new GameObject(
                    "Local Client Player");

            GameObject viewObject =
                new GameObject(
                    "Shotgun Cooldown View");

            GameObject maskObject =
                new GameObject(
                    "Shotgun Cooldown Mask");

            GameObject visualPrefab =
                new GameObject(
                    "Remote Projectile Visual Prefab");

            playerObject.SetActive(false);
            viewObject.SetActive(false);

            try
            {
                visualPrefab.AddComponent<
                    RemoteProjectileVisual>();

                Component bootstrap =
                    bootstrapObject.AddComponent(
                        bootstrapType);

                PropertyInfo registryProperty =
                    bootstrapType.GetProperty(
                        "Registry",
                        BindingFlags.Instance |
                        BindingFlags.Public);

                Assert.That(
                    registryProperty,
                    Is.Not.Null,
                    "NetworkGameBootstrap.Registry " +
                    "was not found.");

                NetworkPlayerRegistry registry =
                    registryProperty.GetValue(
                        bootstrap)
                    as NetworkPlayerRegistry;

                if (registry == null)
                {
                    InvokePrivate(
                        bootstrap,
                        "Awake");

                    registry =
                        registryProperty.GetValue(
                            bootstrap)
                        as NetworkPlayerRegistry;
                }

                Assert.That(
                    registry,
                    Is.Not.Null,
                    "Network player registry was not initialized.");

                SetPrivateField(
                    bootstrap,
                    "remoteProjectileVisualPrefab",
                    visualPrefab);

                Component shotgunSkill =
                    playerObject.AddComponent(
                        shotgunSkillType);

                Assert.That(
                    registry.TryRegister(
                        2u,
                        playerObject),
                    Is.True);

                Image cooldownMask =
                    maskObject.AddComponent<Image>();

                Component view =
                    viewObject.AddComponent(
                        cooldownViewType);

                SetPrivateField(
                    view,
                    "shotgunSkill",
                    shotgunSkill);

                SetPrivateField(
                    view,
                    "cooldownMask",
                    cooldownMask);

                InvokePrivate(
                    view,
                    "Awake");

                viewObject.SetActive(true);

                InvokePrivate(
                    view,
                    "Start");

                PlayerShotgunEvent shotgunEvent =
                    new PlayerShotgunEvent(
                        2u,
                        1u,
                        0f,
                        0f,
                        1f,
                        0f,
                        5u,
                        30f,
                        4f);

                InvokePrivate(
                    bootstrap,
                    "HandleRemotePlayerShotgunEvent",
                    1u,
                    shotgunEvent);

                InvokePrivate(
                    view,
                    "Update");

                Assert.That(
                    cooldownMask.fillAmount,
                    Is.EqualTo(1f)
                        .Within(0.0001f),
                    "The local Client cooldown must start " +
                    "from the authoritative duration.");

                Assert.That(
                    cooldownMask.enabled,
                    Is.True,
                    "The cooldown mask must be visible " +
                    "after an accepted shotgun event.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(
                    viewObject);

                UnityEngine.Object.DestroyImmediate(
                    maskObject);

                UnityEngine.Object.DestroyImmediate(
                    playerObject);

                UnityEngine.Object.DestroyImmediate(
                    bootstrapObject);

                UnityEngine.Object.DestroyImmediate(
                    visualPrefab);
            }
        }

        private static Type FindType(
            string typeName)
        {
            foreach (Assembly assembly
                in AppDomain.CurrentDomain
                    .GetAssemblies())
            {
                Type type =
                    assembly.GetType(
                        typeName,
                        false);

                if (type != null)
                {
                    return type;
                }
            }

            return null;
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
                $"Private method {methodName} " +
                "was not found.");

            method.Invoke(
                target,
                arguments);
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
                $"Private field {fieldName} " +
                "was not found.");

            field.SetValue(
                target,
                value);
        }
    }
}