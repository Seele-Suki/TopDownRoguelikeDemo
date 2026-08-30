using System;
using System.Reflection;
using NUnit.Framework;
using TopDownRoguelike.Gameplay.Networking;
using TopDownRoguelike.Networking.Gameplay;
using TopDownRoguelike.Networking.Protocol;
using UnityEngine;

namespace TopDownRoguelike.Tests.EditMode
{
    public sealed class ClientLocalShotgunRoutingTests
    {
        private const string BootstrapTypeName =
            "TopDownRoguelike.Gameplay.Networking." +
            "NetworkGameBootstrap";

        [Test]
        public void HandleRemotePlayerShotgunEvent_CreatesLocalReceiverAndSpawner()
        {
            Type bootstrapType =
                FindType(BootstrapTypeName);

            Assert.That(
                bootstrapType,
                Is.Not.Null,
                "NetworkGameBootstrap type was not found.");

            GameObject bootstrapObject =
                new GameObject("Bootstrap Test");

            GameObject localPlayer =
                new GameObject("Local Client Player");

            GameObject visualPrefab =
                new GameObject("Remote Visual Prefab");

            visualPrefab.AddComponent<
                RemoteProjectileVisual>();

            bootstrapObject.SetActive(false);

            try
            {
                Component bootstrap =
                    bootstrapObject.AddComponent(
                        bootstrapType);

                InvokePrivate(
                    bootstrap,
                    "Awake");

                SetPrivateField(
                    bootstrap,
                    "localPlayer",
                    localPlayer);

                SetPrivateField(
                    bootstrap,
                    "remoteProjectileVisualPrefab",
                    visualPrefab);

                PropertyInfo registryProperty =
                    bootstrapType.GetProperty(
                        "Registry",
                        BindingFlags.Instance |
                        BindingFlags.Public);

                Assert.That(
                    registryProperty,
                    Is.Not.Null);

                NetworkPlayerRegistry registry =
                    registryProperty.GetValue(
                        bootstrap)
                    as NetworkPlayerRegistry;

                Assert.That(
                    registry,
                    Is.Not.Null);

                Assert.That(
                    registry.TryRegister(
                        2u,
                        localPlayer),
                    Is.True);

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

                RemotePlayerShotgunEventReceiver receiver =
                    localPlayer.GetComponent<
                        RemotePlayerShotgunEventReceiver>();

                RemoteShotgunVisualSpawner spawner =
                    localPlayer.GetComponent<
                        RemoteShotgunVisualSpawner>();

                Assert.That(
                    receiver,
                    Is.Not.Null,
                    "Local player needs a shotgun receiver.");

                Assert.That(
                    spawner,
                    Is.Not.Null,
                    "Local player needs a shotgun visual spawner.");

                Assert.That(
                    receiver.PendingCount,
                    Is.EqualTo(1));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(
                    bootstrapObject);

                UnityEngine.Object.DestroyImmediate(
                    localPlayer);

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
                        typeName);

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
                $"Private method {methodName} was not found.");

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
                $"Private field {fieldName} was not found.");

            field.SetValue(
                target,
                value);
        }
    }
}