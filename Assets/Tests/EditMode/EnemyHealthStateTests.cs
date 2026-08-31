using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TopDownRoguelike.Infrastructure;
using UnityEngine;
using UnityEngine.TestTools;

namespace TopDownRoguelike.Tests.EditMode
{
    public sealed class EnemyHealthStateTests
    {
        private readonly List<GameObject> createdObjects =
            new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            GameSession.Reset();

            foreach (GameObject createdObject
                in createdObjects)
            {
                if (createdObject != null)
                {
                    UnityEngine.Object.DestroyImmediate(
                        createdObject);
                }
            }

            createdObjects.Clear();
        }

        [Test]
        public void EnemyHealth_ExposesReadOnlyCurrentMaximumAndDeathState()
        {
            Type enemyHealthType =
                FindType(
                    "TopDownRoguelike.Gameplay.Enemies." +
                    "EnemyHealth");

            Assert.That(
                enemyHealthType,
                Is.Not.Null,
                "EnemyHealth must exist.");

            GameObject enemyObject =
                new GameObject("Enemy Health Test");

            createdObjects.Add(
                enemyObject);

            enemyObject.SetActive(false);

            Component enemyHealth =
                enemyObject.AddComponent(
                    enemyHealthType);

            MethodInfo awakeMethod =
                enemyHealthType.GetMethod(
                    "Awake",
                    BindingFlags.Instance |
                    BindingFlags.NonPublic);

            Assert.That(
                awakeMethod,
                Is.Not.Null,
                "EnemyHealth.Awake must exist.");

            awakeMethod.Invoke(
                enemyHealth,
                null);

            PropertyInfo currentHealthProperty =
                enemyHealthType.GetProperty(
                    "CurrentHealth",
                    BindingFlags.Instance |
                    BindingFlags.Public);

            PropertyInfo maxHealthProperty =
                enemyHealthType.GetProperty(
                    "MaxHealth",
                    BindingFlags.Instance |
                    BindingFlags.Public);

            PropertyInfo isDeadProperty =
                enemyHealthType.GetProperty(
                    "IsDead",
                    BindingFlags.Instance |
                    BindingFlags.Public);

            Assert.That(
                currentHealthProperty,
                Is.Not.Null,
                "CurrentHealth must exist.");

            Assert.That(
                maxHealthProperty,
                Is.Not.Null,
                "MaxHealth must exist.");

            Assert.That(
                isDeadProperty,
                Is.Not.Null,
                "IsDead must exist.");

            Assert.That(
                currentHealthProperty.CanWrite,
                Is.False);

            Assert.That(
                maxHealthProperty.CanWrite,
                Is.False);

            Assert.That(
                isDeadProperty.CanWrite,
                Is.False);

            Assert.That(
                currentHealthProperty.GetValue(
                    enemyHealth,
                    null),
                Is.EqualTo(3));

            Assert.That(
                maxHealthProperty.GetValue(
                    enemyHealth,
                    null),
                Is.EqualTo(3));

            Assert.That(
                isDeadProperty.GetValue(
                    enemyHealth,
                    null),
                Is.False);

            MethodInfo applyDifficultyMethod =
                enemyHealthType.GetMethod(
                    "ApplyDifficulty",
                    new Type[]
                    {
                        typeof(float)
                    });

            Assert.That(
                applyDifficultyMethod,
                Is.Not.Null);

            applyDifficultyMethod.Invoke(
                enemyHealth,
                new object[]
                {
                    2f
                });

            Assert.That(
                currentHealthProperty.GetValue(
                    enemyHealth,
                    null),
                Is.EqualTo(6));

            Assert.That(
                maxHealthProperty.GetValue(
                    enemyHealth,
                    null),
                Is.EqualTo(6));
        }

        [Test]
        public void TakeDamage_ClampsHealthAndMarksDead()
        {
            Type enemyHealthType =
                FindType(
                    "TopDownRoguelike.Gameplay.Enemies." +
                    "EnemyHealth");

            Assert.That(
                enemyHealthType,
                Is.Not.Null);

            GameObject enemyObject =
                new GameObject("Enemy Damage Test");

            createdObjects.Add(
                enemyObject);

            enemyObject.SetActive(false);

            Component enemyHealth =
                enemyObject.AddComponent(
                    enemyHealthType);

            MethodInfo awakeMethod =
                enemyHealthType.GetMethod(
                    "Awake",
                    BindingFlags.Instance |
                    BindingFlags.NonPublic);

            Assert.That(
                awakeMethod,
                Is.Not.Null);

            awakeMethod.Invoke(
                enemyHealth,
                null);

            PropertyInfo currentHealthProperty =
                enemyHealthType.GetProperty(
                    "CurrentHealth");

            PropertyInfo isDeadProperty =
                enemyHealthType.GetProperty(
                    "IsDead");

            Assert.That(
                currentHealthProperty,
                Is.Not.Null);

            Assert.That(
                isDeadProperty,
                Is.Not.Null);

            InvokeTakeDamage(
                enemyHealth,
                2);

            Assert.That(
                currentHealthProperty.GetValue(
                    enemyHealth,
                    null),
                Is.EqualTo(1));

            Assert.That(
                isDeadProperty.GetValue(
                    enemyHealth,
                    null),
                Is.False);

            LogAssert.Expect(
                LogType.Error,
                "Destroy may not be called from edit mode! " +
                "Use DestroyImmediate instead.\n" +
                "Destroying an object in edit mode destroys it permanently.");

            InvokeTakeDamage(
                enemyHealth,
                5);

            Assert.That(
                currentHealthProperty.GetValue(
                    enemyHealth,
                    null),
                Is.EqualTo(0));

            Assert.That(
                isDeadProperty.GetValue(
                    enemyHealth,
                    null),
                Is.True);
        }

        [Test]
        public void TakeDamage_MultiplayerClientIgnoresLocalDamage()
        {
            GameSession.ConfigureMultiplayerClient();

            Type enemyHealthType =
                FindType(
                    "TopDownRoguelike.Gameplay.Enemies." +
                    "EnemyHealth");

            Assert.That(enemyHealthType, Is.Not.Null);

            var enemyObject =
                new GameObject(
                    "Client Enemy Damage Authority Test");

            createdObjects.Add(enemyObject);
            enemyObject.SetActive(false);

            Component enemyHealth =
                enemyObject.AddComponent(enemyHealthType);

            enemyHealthType.GetMethod(
                    "Awake",
                    BindingFlags.Instance |
                    BindingFlags.NonPublic)
                .Invoke(enemyHealth, null);

            InvokeTakeDamage(enemyHealth, 2);

            Assert.That(
                enemyHealthType.GetProperty("CurrentHealth")
                    .GetValue(enemyHealth, null),
                Is.EqualTo(3),
                "A joining client must not apply local enemy damage.");

            Assert.That(
                enemyHealthType.GetProperty("IsDead")
                    .GetValue(enemyHealth, null),
                Is.False);
        }

        [Test]
        public void TakeDamage_MultiplayerHostAppliesDamage()
        {
            GameSession.ConfigureMultiplayerHost();

            Type enemyHealthType =
                FindType(
                    "TopDownRoguelike.Gameplay.Enemies." +
                    "EnemyHealth");

            Assert.That(enemyHealthType, Is.Not.Null);

            var enemyObject =
                new GameObject(
                    "Host Enemy Damage Authority Test");

            createdObjects.Add(enemyObject);
            enemyObject.SetActive(false);

            Component enemyHealth =
                enemyObject.AddComponent(enemyHealthType);

            enemyHealthType.GetMethod(
                    "Awake",
                    BindingFlags.Instance |
                    BindingFlags.NonPublic)
                .Invoke(enemyHealth, null);

            InvokeTakeDamage(enemyHealth, 2);

            Assert.That(
                enemyHealthType.GetProperty("CurrentHealth")
                    .GetValue(enemyHealth, null),
                Is.EqualTo(1),
                "The authoritative host must still apply enemy damage.");
        }

        [Test]
        public void ApplyAuthoritativeState_MultiplayerClientUpdatesHealth()
        {
            GameSession.ConfigureMultiplayerClient();

            Type enemyHealthType =
                FindType(
                    "TopDownRoguelike.Gameplay.Enemies." +
                    "EnemyHealth");

            Assert.That(enemyHealthType, Is.Not.Null);

            var enemyObject =
                new GameObject(
                    "Client Enemy Authoritative Health Test");

            createdObjects.Add(enemyObject);
            enemyObject.SetActive(false);

            Component enemyHealth =
                enemyObject.AddComponent(enemyHealthType);

            enemyHealthType.GetMethod(
                    "Awake",
                    BindingFlags.Instance |
                    BindingFlags.NonPublic)
                .Invoke(enemyHealth, null);

            MethodInfo applyMethod =
                enemyHealthType.GetMethod(
                    "ApplyAuthoritativeState",
                    BindingFlags.Instance |
                    BindingFlags.Public,
                    null,
                    new[]
                    {
                        typeof(int),
                        typeof(int),
                        typeof(bool)
                    },
                    null);

            Assert.That(
                applyMethod,
                Is.Not.Null,
                "EnemyHealth must expose the client snapshot entry point.");

            Assert.That(
                (bool)applyMethod.Invoke(
                    enemyHealth,
                    new object[]
                    {
                        2,
                        7,
                        false
                    }),
                Is.True);

            Assert.That(
                enemyHealthType.GetProperty("CurrentHealth")
                    .GetValue(enemyHealth, null),
                Is.EqualTo(2));

            Assert.That(
                enemyHealthType.GetProperty("MaxHealth")
                    .GetValue(enemyHealth, null),
                Is.EqualTo(7));

            Assert.That(
                enemyHealthType.GetProperty("IsDead")
                    .GetValue(enemyHealth, null),
                Is.False);

            Assert.That(
                enemyObject,
                Is.Not.Null,
                "Applying a snapshot must not destroy the enemy locally.");
        }

        private static void InvokeTakeDamage(
            Component enemyHealth,
            int damage)
        {
            MethodInfo takeDamageMethod =
                enemyHealth.GetType().GetMethod(
                    "TakeDamage");

            Assert.That(
                takeDamageMethod,
                Is.Not.Null);

            Type damageInfoType =
                takeDamageMethod.GetParameters()[0]
                    .ParameterType;

            object damageInfo =
                Activator.CreateInstance(
                    damageInfoType);

            FieldInfo damageField =
                damageInfoType.GetField(
                    "Damage");

            Assert.That(
                damageField,
                Is.Not.Null);

            damageField.SetValue(
                damageInfo,
                damage);

            takeDamageMethod.Invoke(
                enemyHealth,
                new object[]
                {
                    damageInfo
                });
        }

        private static Type FindType(
            string fullTypeName)
        {
            foreach (Assembly assembly
                in AppDomain.CurrentDomain.GetAssemblies())
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
