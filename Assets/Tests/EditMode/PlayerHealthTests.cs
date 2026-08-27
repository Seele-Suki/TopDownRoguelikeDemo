using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace TopDownRoguelike.Tests.EditMode
{
    public sealed class PlayerHealthTests
    {
        private const string PlayerHealthTypeName =
            "TopDownRoguelike.Gameplay.Characters.PlayerHealth";

        [Test]
        public void Awake_ExposesInitialHealthState()
        {
            Type playerHealthType =
                FindType(PlayerHealthTypeName);

            Assert.That(playerHealthType, Is.Not.Null);

            var playerObject =
                new GameObject("Player Health Test");

            playerObject.SetActive(false);

            try
            {
                Component playerHealth =
                    playerObject.AddComponent(
                        playerHealthType);

                MethodInfo awakeMethod =
                    playerHealthType.GetMethod(
                        "Awake",
                        BindingFlags.Instance |
                        BindingFlags.NonPublic);

                Assert.That(awakeMethod, Is.Not.Null);

                awakeMethod.Invoke(playerHealth, null);

                PropertyInfo currentProperty =
                    playerHealthType.GetProperty(
                        "CurrentHealth");

                PropertyInfo maximumProperty =
                    playerHealthType.GetProperty(
                        "MaxHealth");

                Assert.That(
                    currentProperty,
                    Is.Not.Null,
                    "PlayerHealth must expose CurrentHealth.");

                Assert.That(
                    maximumProperty,
                    Is.Not.Null,
                    "PlayerHealth must expose MaxHealth.");

                Assert.That(
                    currentProperty.GetValue(playerHealth),
                    Is.EqualTo(10));

                Assert.That(
                    maximumProperty.GetValue(playerHealth),
                    Is.EqualTo(10));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(
                    playerObject);
            }
        }

        [Test]
        public void OnHealthChanged_UsesCurrentAndMaximumHealth()
        {
            Type playerHealthType =
                FindType(PlayerHealthTypeName);

            Assert.That(playerHealthType, Is.Not.Null);

            EventInfo healthChangedEvent =
                playerHealthType.GetEvent(
                    "OnHealthChanged");

            Assert.That(
                healthChangedEvent,
                Is.Not.Null,
                "PlayerHealth must expose OnHealthChanged.");

            Assert.That(
                healthChangedEvent.EventHandlerType,
                Is.EqualTo(typeof(Action<int, int>)));
        }

        [Test]
        public void TakeDamage_RaisesHealthChangedWithRemainingHealth()
        {
            Component playerHealth =
                CreateInitializedPlayerHealth(
                    out GameObject playerObject);

            try
            {
                int eventCount = 0;
                int reportedCurrent = -1;
                int reportedMaximum = -1;

                Action<int, int> handler =
                    (current, maximum) =>
                    {
                        eventCount++;
                        reportedCurrent = current;
                        reportedMaximum = maximum;
                    };

                playerHealth.GetType()
                    .GetEvent("OnHealthChanged")
                    .AddEventHandler(playerHealth, handler);

                InvokeTakeDamage(playerHealth, 3);

                Assert.That(eventCount, Is.EqualTo(1));
                Assert.That(reportedCurrent, Is.EqualTo(7));
                Assert.That(reportedMaximum, Is.EqualTo(10));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(
                    playerObject);
            }
        }

        [Test]
        public void FatalDamage_RaisesOneHealthChangedWithZeroHealth()
        {
            Component playerHealth =
                CreateInitializedPlayerHealth(
                    out GameObject playerObject);

            try
            {
                int eventCount = 0;
                int reportedCurrent = -1;

                Action<int, int> handler =
                    (current, maximum) =>
                    {
                        eventCount++;
                        reportedCurrent = current;
                    };

                playerHealth.GetType()
                    .GetEvent("OnHealthChanged")
                    .AddEventHandler(playerHealth, handler);

                InvokeTakeDamage(playerHealth, 20);
                InvokeTakeDamage(playerHealth, 1);

                Assert.That(eventCount, Is.EqualTo(1));
                Assert.That(reportedCurrent, Is.EqualTo(0));

                PropertyInfo isDeadProperty =
                    playerHealth.GetType().GetProperty("IsDead");

                Assert.That(
                    isDeadProperty.GetValue(playerHealth),
                    Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(
                    playerObject);
            }
        }

        [Test]
        public void AddMaxHealth_RaisesHealthChangedWithNewValues()
        {
            Component playerHealth =
                CreateInitializedPlayerHealth(
                    out GameObject playerObject);

            try
            {
                int eventCount = 0;
                int reportedCurrent = -1;
                int reportedMaximum = -1;

                Action<int, int> handler =
                    (current, maximum) =>
                    {
                        eventCount++;
                        reportedCurrent = current;
                        reportedMaximum = maximum;
                    };

                playerHealth.GetType()
                    .GetEvent("OnHealthChanged")
                    .AddEventHandler(playerHealth, handler);

                MethodInfo addMaxHealthMethod =
                    playerHealth.GetType().GetMethod(
                        "AddMaxHealth");

                addMaxHealthMethod.Invoke(
                    playerHealth,
                    new object[] { 5 });

                Assert.That(eventCount, Is.EqualTo(1));
                Assert.That(reportedCurrent, Is.EqualTo(15));
                Assert.That(reportedMaximum, Is.EqualTo(15));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(
                    playerObject);
            }
        }

        private static Component CreateInitializedPlayerHealth(
            out GameObject playerObject)
        {
            Type playerHealthType =
                FindType(PlayerHealthTypeName);

            Assert.That(playerHealthType, Is.Not.Null);

            playerObject =
                new GameObject("Player Health Test");

            playerObject.SetActive(false);

            Component playerHealth =
                playerObject.AddComponent(
                    playerHealthType);

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
                playerHealth.GetType().GetMethod(
                    "TakeDamage");

            Assert.That(takeDamageMethod, Is.Not.Null);

            Type damageInfoType =
                takeDamageMethod.GetParameters()[0]
                    .ParameterType;

            object damageInfo =
                Activator.CreateInstance(
                    damageInfoType);

            FieldInfo damageField =
                damageInfoType.GetField("Damage");

            Assert.That(damageField, Is.Not.Null);

            damageField.SetValue(
                damageInfo,
                damage);

            takeDamageMethod.Invoke(
                playerHealth,
                new[] { damageInfo });
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