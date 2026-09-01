using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TopDownRoguelike.Infrastructure;
using TopDownRoguelike.Networking.Gameplay;
using TopDownRoguelike.Networking.Protocol;
using UnityEngine;

namespace TopDownRoguelike.Tests.EditMode
{
    public sealed class ClientPlayerHealthSnapshotTests
    {
        private readonly List<GameObject> createdObjects =
            new List<GameObject>();

        [SetUp]
        public void SetUp()
        {
            GameSession.Reset();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject createdObject in createdObjects)
            {
                if (createdObject != null)
                {
                    UnityEngine.Object.DestroyImmediate(createdObject);
                }
            }

            createdObjects.Clear();
            GameSession.Reset();
        }

        [Test]
        public void ClientSnapshot_AppliesAuthorityToRegisteredPlayers()
        {
            GameSession.ConfigureMultiplayerClient();

            Type bootstrapType = FindType(
                "TopDownRoguelike.Gameplay.Networking." +
                "NetworkGameBootstrap");
            Type playerHealthType = FindType(
                "TopDownRoguelike.Gameplay.Characters." +
                "PlayerHealth");

            Assert.That(bootstrapType, Is.Not.Null);
            Assert.That(playerHealthType, Is.Not.Null);

            GameObject bootstrapObject = CreateObject(
                "Client Health Snapshot Bootstrap");
            bootstrapObject.SetActive(false);
            Component bootstrap = bootstrapObject.AddComponent(
                bootstrapType);

            GameObject localPlayer = CreateObject("Local Player");
            localPlayer.SetActive(false);
            Component localHealth = localPlayer.AddComponent(
                playerHealthType);

            GameObject remotePlayer = CreateObject("Remote Player");
            remotePlayer.SetActive(false);
            Component remoteHealth = remotePlayer.AddComponent(
                playerHealthType);

            InvokePrivate(bootstrap, "Awake");

            var registry = new NetworkPlayerRegistry();
            Assert.That(registry.TryRegister(1u, localPlayer), Is.True);
            Assert.That(registry.TryRegister(2u, remotePlayer), Is.True);

            SetPrivateField(bootstrap, "registry", registry);
            InvokePrivate(
                bootstrap,
                "ApplyAuthoritativePlayerHealth",
                new PlayerStateSnapshotPayload(
                    new[]
                    {
                        new PlayerStateRecord(
                            1u,
                            0f,
                            0f,
                            1f,
                            0f,
                            false,
                            false,
                            4,
                            12),
                        new PlayerStateRecord(
                            2u,
                            0f,
                            0f,
                            1f,
                            0f,
                            false,
                            false,
                            7,
                            10)
                    }));

            Assert.That(
                GetPropertyValue<int>(localHealth, "CurrentHealth"),
                Is.EqualTo(4));
            Assert.That(
                GetPropertyValue<int>(localHealth, "MaxHealth"),
                Is.EqualTo(12));
            Assert.That(
                GetPropertyValue<int>(remoteHealth, "CurrentHealth"),
                Is.EqualTo(7));
            Assert.That(
                GetPropertyValue<int>(remoteHealth, "MaxHealth"),
                Is.EqualTo(10));
        }

        private GameObject CreateObject(string name)
        {
            GameObject result = new GameObject(name);
            createdObjects.Add(result);
            return result;
        }

        private static void InvokePrivate(
            Component target,
            string methodName,
            params object[] arguments)
        {
            MethodInfo method = target.GetType().GetMethod(
                methodName,
                BindingFlags.Instance |
                BindingFlags.NonPublic);

            Assert.That(method, Is.Not.Null);
            method.Invoke(target, arguments);
        }

        private static void SetPrivateField(
            Component target,
            string fieldName,
            object value)
        {
            FieldInfo field = target.GetType().GetField(
                fieldName,
                BindingFlags.Instance |
                BindingFlags.NonPublic);

            Assert.That(field, Is.Not.Null);
            field.SetValue(target, value);
        }

        private static T GetPropertyValue<T>(
            Component target,
            string propertyName)
        {
            PropertyInfo property = target.GetType().GetProperty(
                propertyName);

            Assert.That(property, Is.Not.Null);
            return (T)property.GetValue(target, null);
        }

        private static Type FindType(string fullTypeName)
        {
            foreach (Assembly assembly in
                AppDomain.CurrentDomain.GetAssemblies())
            {
                Type result = assembly.GetType(fullTypeName, false);

                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }
    }
}
