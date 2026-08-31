using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TopDownRoguelike.Networking.Gameplay;
using TopDownRoguelike.Networking.Protocol;
using UnityEngine;
using UnityEngine.TestTools;

namespace TopDownRoguelike.Tests.EditMode
{
    public sealed class HostEnemyHealthCollectionTests
    {
        private readonly List<GameObject> createdObjects =
            new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
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
        public void CollectCurrentEnemyHealth_ReturnsEnemyHealthState()
        {
            Type publisherType =
                FindType(
                    "TopDownRoguelike.Gameplay.Networking." +
                    "HostWorldSnapshotPublisher");

            Type spawnerType =
                FindType(
                    "TopDownRoguelike.Gameplay.Enemies." +
                    "EnemySpawner");

            Type encounterType =
                FindType(
                    "TopDownRoguelike.Gameplay.Bosses." +
                    "BossEncounterController");

            Type enemyHealthType =
                FindType(
                    "TopDownRoguelike.Gameplay.Enemies." +
                    "EnemyHealth");

            Assert.That(publisherType, Is.Not.Null);
            Assert.That(spawnerType, Is.Not.Null);
            Assert.That(encounterType, Is.Not.Null);
            Assert.That(enemyHealthType, Is.Not.Null);

            var playerRegistry =
                new NetworkPlayerRegistry();

            GameObject spawnerObject =
                CreateObject("Enemy Spawner");

            spawnerObject.SetActive(false);

            Component spawner =
                spawnerObject.AddComponent(
                    spawnerType);

            GameObject enemy =
                CreateObject("Enemy");

            enemy.SetActive(false);

            NetworkEntityId identifier =
                enemy.AddComponent<NetworkEntityId>();

            Assert.That(
                identifier.TryAssign(
                    22u,
                    NetworkEntityType.Enemy),
                Is.True);

            Component enemyHealth =
                enemy.AddComponent(
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

            FieldInfo spawnedEnemiesField =
                spawnerType.GetField(
                    "spawnedEnemies",
                    BindingFlags.Instance |
                    BindingFlags.NonPublic);

            Assert.That(
                spawnedEnemiesField,
                Is.Not.Null);

            var spawnedEnemies =
                (IList)spawnedEnemiesField.GetValue(
                    spawner);

            spawnedEnemies.Add(enemy);

            GameObject encounterObject =
                CreateObject("Boss Encounter");

            encounterObject.SetActive(false);

            Component encounter =
                encounterObject.AddComponent(
                    encounterType);

            GameObject publisherObject =
                CreateObject("Publisher");

            publisherObject.SetActive(false);

            Component publisher =
                publisherObject.AddComponent(
                    publisherType);

            MethodInfo configureMethod =
                publisherType.GetMethod(
                    "ConfigureWorldSources",
                    BindingFlags.Instance |
                    BindingFlags.Public);

            Assert.That(
                configureMethod,
                Is.Not.Null);

            configureMethod.Invoke(
                publisher,
                new object[]
                {
                    playerRegistry,
                    spawner,
                    encounter
                });

            MethodInfo collectMethod =
                publisherType.GetMethod(
                    "CollectCurrentEnemyHealth",
                    BindingFlags.Instance |
                    BindingFlags.Public);

            Assert.That(
                collectMethod,
                Is.Not.Null,
                "CollectCurrentEnemyHealth must exist.");

            object result =
                collectMethod.Invoke(
                    publisher,
                    null);

            var states =
                new List<object>();

            foreach (object state
                in (IEnumerable)result)
            {
                states.Add(state);
            }

            Assert.That(
                states,
                Has.Count.EqualTo(1));

            object enemyState =
                states[0];

            Type stateType =
                enemyState.GetType();

            PropertyInfo entityIdProperty =
                stateType.GetProperty(
                    "EntityId");

            PropertyInfo currentHealthProperty =
                stateType.GetProperty(
                    "CurrentHealth");

            PropertyInfo maxHealthProperty =
                stateType.GetProperty(
                    "MaxHealth");

            PropertyInfo isDeadProperty =
                stateType.GetProperty(
                    "IsDead");

            PropertyInfo networkArchetypeProperty =
                stateType.GetProperty(
                    "NetworkArchetype");

            Assert.That(
                entityIdProperty,
                Is.Not.Null);

            Assert.That(
                currentHealthProperty,
                Is.Not.Null);

            Assert.That(
                maxHealthProperty,
                Is.Not.Null);

            Assert.That(
                isDeadProperty,
                Is.Not.Null);

            Assert.That(
                networkArchetypeProperty,
                Is.Not.Null);

            Assert.That(
                entityIdProperty.GetValue(
                    enemyState,
                    null),
                Is.EqualTo(22u));

            Assert.That(
                currentHealthProperty.GetValue(
                    enemyState,
                    null),
                Is.EqualTo((ushort)3));

            Assert.That(
                maxHealthProperty.GetValue(
                    enemyState,
                    null),
                Is.EqualTo((ushort)3));

            Assert.That(
                isDeadProperty.GetValue(
                    enemyState,
                    null),
                Is.False);

            Assert.That(
                networkArchetypeProperty.GetValue(
                    enemyState,
                    null),
                Is.EqualTo(
                    NetworkEnemyArchetype.Basic));

            MethodInfo takeDamageMethod =
                enemyHealthType.GetMethod(
                    "TakeDamage");

            Assert.That(
                takeDamageMethod,
                Is.Not.Null,
                "EnemyHealth.TakeDamage must exist.");

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
                Is.Not.Null,
                "DamageInfo.Damage must exist.");

            damageField.SetValue(
                damageInfo,
                99);

            LogAssert.Expect(
                LogType.Warning,
                "No ExperienceOrbPool found in the scene.");

            LogAssert.Expect(
                LogType.Error,
                "Destroy may not be called from edit mode! " +
                "Use DestroyImmediate instead.\n" +
                "Destroying an object in edit mode destroys it permanently.");

            takeDamageMethod.Invoke(
                enemyHealth,
                new object[]
                {
                    damageInfo
                });

            object deadResult =
                collectMethod.Invoke(
                    publisher,
                    null);

            var deadStates =
                new List<object>();

            foreach (object state
                in (IEnumerable)deadResult)
            {
                deadStates.Add(state);
            }

            Assert.That(
                deadStates,
                Has.Count.EqualTo(1));

            object deadEnemyState =
                deadStates[0];

            Assert.That(
                currentHealthProperty.GetValue(
                    deadEnemyState,
                    null),
                Is.EqualTo((ushort)0));

            Assert.That(
                maxHealthProperty.GetValue(
                    deadEnemyState,
                    null),
                Is.EqualTo((ushort)3));

            Assert.That(
                isDeadProperty.GetValue(
                    deadEnemyState,
                    null),
                Is.True);
        }

        private GameObject CreateObject(
            string objectName)
        {
            GameObject result =
                new GameObject(objectName);

            createdObjects.Add(result);
            return result;
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
