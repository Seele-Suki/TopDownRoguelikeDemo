using System;
using System.Reflection;
using NUnit.Framework;
using TopDownRoguelike.Infrastructure;
using UnityEngine;

namespace TopDownRoguelike.Tests.EditMode
{
    public sealed class PlayerHealthAuthorityTests
    {
        private const string PlayerHealthTypeName =
            "TopDownRoguelike.Gameplay.Characters.PlayerHealth";

        [SetUp]
        public void SetUp()
        {
            GameSession.Reset();
        }

        [TearDown]
        public void TearDown()
        {
            GameSession.Reset();
        }

        [Test]
        public void MultiplayerClient_TakeDamageDoesNotChangeHealthOrDie()
        {
            GameSession.ConfigureMultiplayerClient();
            Component playerHealth =
                CreateInitializedPlayerHealth(
                    out GameObject playerObject);

            try
            {
                int eventCount = 0;
                AddHealthChangedHandler(
                    playerHealth,
                    (current, maximum) => eventCount++);

                InvokeTakeDamage(playerHealth, 20);

                Assert.That(
                    GetPropertyValue<int>(
                        playerHealth,
                        "CurrentHealth"),
                    Is.EqualTo(10));
                Assert.That(
                    GetPropertyValue<bool>(
                        playerHealth,
                        "IsDead"),
                    Is.False);
                Assert.That(eventCount, Is.EqualTo(0));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(playerObject);
            }
        }

        [Test]
        public void MultiplayerClient_AddMaxHealthDoesNotChangeHealth()
        {
            GameSession.ConfigureMultiplayerClient();
            Component playerHealth =
                CreateInitializedPlayerHealth(
                    out GameObject playerObject);

            try
            {
                InvokePublicMethod(
                    playerHealth,
                    "AddMaxHealth",
                    5);

                Assert.That(
                    GetPropertyValue<int>(
                        playerHealth,
                        "CurrentHealth"),
                    Is.EqualTo(10));
                Assert.That(
                    GetPropertyValue<int>(
                        playerHealth,
                        "MaxHealth"),
                    Is.EqualTo(10));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(playerObject);
            }
        }

        [Test]
        public void MultiplayerHost_TakeDamageCanCauseAuthoritativeDeath()
        {
            GameSession.ConfigureMultiplayerHost();
            Component playerHealth =
                CreateInitializedPlayerHealth(
                    out GameObject playerObject);

            try
            {
                InvokeTakeDamage(playerHealth, 20);

                Assert.That(
                    GetPropertyValue<int>(
                        playerHealth,
                        "CurrentHealth"),
                    Is.EqualTo(0));
                Assert.That(
                    GetPropertyValue<bool>(
                        playerHealth,
                        "IsDead"),
                    Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(playerObject);
            }
        }

        [Test]
        public void MultiplayerClient_ApplyAuthoritativeStateUpdatesHealth()
        {
            GameSession.ConfigureMultiplayerClient();
            Component playerHealth =
                CreateInitializedPlayerHealth(
                    out GameObject playerObject);

            try
            {
                int eventCount = 0;
                int reportedCurrent = -1;
                int reportedMaximum = -1;

                AddHealthChangedHandler(
                    playerHealth,
                    (current, maximum) =>
                    {
                        eventCount++;
                        reportedCurrent = current;
                        reportedMaximum = maximum;
                    });

                bool applied =
                    InvokeAuthoritativeState(
                        playerHealth,
                        4,
                        12);

                Assert.That(applied, Is.True);
                Assert.That(
                    GetPropertyValue<int>(
                        playerHealth,
                        "CurrentHealth"),
                    Is.EqualTo(4));
                Assert.That(
                    GetPropertyValue<int>(
                        playerHealth,
                        "MaxHealth"),
                    Is.EqualTo(12));
                Assert.That(
                    GetPropertyValue<bool>(
                        playerHealth,
                        "IsDead"),
                    Is.False);
                Assert.That(eventCount, Is.EqualTo(1));
                Assert.That(reportedCurrent, Is.EqualTo(4));
                Assert.That(reportedMaximum, Is.EqualTo(12));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(playerObject);
            }
        }

        [Test]
        public void MultiplayerClient_ApplyAuthoritativeDeathRaisesEvents()
        {
            GameSession.ConfigureMultiplayerClient();
            Component playerHealth =
                CreateInitializedPlayerHealth(
                    out GameObject playerObject);

            try
            {
                int healthChangedCount = 0;
                int diedCount = 0;

                AddHealthChangedHandler(
                    playerHealth,
                    (current, maximum) => healthChangedCount++);

                playerHealth.GetType()
                    .GetEvent("OnDied")
                    .AddEventHandler(
                        playerHealth,
                        (Action)(() => diedCount++));

                Assert.That(
                    InvokeAuthoritativeState(
                        playerHealth,
                        0,
                        10),
                    Is.True);
                Assert.That(
                    GetPropertyValue<bool>(
                        playerHealth,
                        "IsDead"),
                    Is.True);
                Assert.That(healthChangedCount, Is.EqualTo(1));
                Assert.That(diedCount, Is.EqualTo(1));

                Assert.That(
                    InvokeAuthoritativeState(
                        playerHealth,
                        0,
                        10),
                    Is.True);
                Assert.That(healthChangedCount, Is.EqualTo(1));
                Assert.That(diedCount, Is.EqualTo(1));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(playerObject);
            }
        }

        [Test]
        public void MultiplayerHost_ApplyAuthoritativeStateIsRejected()
        {
            GameSession.ConfigureMultiplayerHost();
            Component playerHealth =
                CreateInitializedPlayerHealth(
                    out GameObject playerObject);

            try
            {
                Assert.That(
                    InvokeAuthoritativeState(
                        playerHealth,
                        4,
                        12),
                    Is.False);
                Assert.That(
                    GetPropertyValue<int>(
                        playerHealth,
                        "CurrentHealth"),
                    Is.EqualTo(10));
                Assert.That(
                    GetPropertyValue<int>(
                        playerHealth,
                        "MaxHealth"),
                    Is.EqualTo(10));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(playerObject);
            }
        }

        private static Component CreateInitializedPlayerHealth(
            out GameObject playerObject)
        {
            Type playerHealthType =
                FindType(PlayerHealthTypeName);

            Assert.That(playerHealthType, Is.Not.Null);

            playerObject =
                new GameObject("Player Health Authority Test");

            playerObject.SetActive(false);

            Component playerHealth =
                playerObject.AddComponent(playerHealthType);

            MethodInfo awakeMethod =
                playerHealthType.GetMethod(
                    "Awake",
                    BindingFlags.Instance |
                    BindingFlags.NonPublic);

            Assert.That(awakeMethod, Is.Not.Null);
            awakeMethod.Invoke(playerHealth, null);

            return playerHealth;
        }

        private static void InvokeTakeDamage(
            Component playerHealth,
            int damage)
        {
            MethodInfo takeDamageMethod =
                playerHealth.GetType().GetMethod("TakeDamage");

            Assert.That(takeDamageMethod, Is.Not.Null);

            Type damageInfoType =
                takeDamageMethod.GetParameters()[0].ParameterType;

            object damageInfo =
                Activator.CreateInstance(damageInfoType);

            FieldInfo damageField =
                damageInfoType.GetField("Damage");

            Assert.That(damageField, Is.Not.Null);
            damageField.SetValue(damageInfo, damage);
            takeDamageMethod.Invoke(
                playerHealth,
                new[] { damageInfo });
        }

        private static void InvokePublicMethod(
            Component target,
            string methodName,
            params object[] arguments)
        {
            MethodInfo method =
                target.GetType().GetMethod(methodName);

            Assert.That(method, Is.Not.Null);
            method.Invoke(target, arguments);
        }

        private static bool InvokeAuthoritativeState(
            Component playerHealth,
            int currentHealth,
            int maxHealth)
        {
            MethodInfo method =
                playerHealth.GetType().GetMethod(
                    "ApplyAuthoritativeState");

            Assert.That(method, Is.Not.Null);

            return (bool)method.Invoke(
                playerHealth,
                new object[]
                {
                    currentHealth,
                    maxHealth
                });
        }

        private static void AddHealthChangedHandler(
            Component playerHealth,
            Action<int, int> handler)
        {
            playerHealth.GetType()
                .GetEvent("OnHealthChanged")
                .AddEventHandler(playerHealth, handler);
        }

        private static T GetPropertyValue<T>(
            Component target,
            string propertyName)
        {
            PropertyInfo property =
                target.GetType().GetProperty(propertyName);

            Assert.That(property, Is.Not.Null);
            return (T)property.GetValue(target, null);
        }

        private static Type FindType(string fullTypeName)
        {
            foreach (Assembly assembly in
                AppDomain.CurrentDomain.GetAssemblies())
            {
                Type result =
                    assembly.GetType(fullTypeName, false);

                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }
    }
}
