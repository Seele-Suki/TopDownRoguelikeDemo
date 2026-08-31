using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TopDownRoguelike.Networking.Gameplay;
using TopDownRoguelike.Networking.Protocol;
using UnityEngine;

namespace TopDownRoguelike.Tests.EditMode
{
    public sealed class HostBossHealthCollectionTests
    {
        private readonly List<UnityEngine.Object> createdObjects =
            new List<UnityEngine.Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (UnityEngine.Object createdObject
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
        public void CollectCurrentBossHealth_ReturnsPhaseAndHealth()
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

            Type bossHealthType =
                FindType(
                    "TopDownRoguelike.Gameplay.Bosses." +
                    "BossHealth");

            Type bossControllerType =
                FindType(
                    "TopDownRoguelike.Gameplay.Bosses." +
                    "BossController");

            Type bossDataType =
                FindType(
                    "TopDownRoguelike.Gameplay.Bosses." +
                    "BossData");

            Assert.That(publisherType, Is.Not.Null);
            Assert.That(spawnerType, Is.Not.Null);
            Assert.That(encounterType, Is.Not.Null);
            Assert.That(bossHealthType, Is.Not.Null);
            Assert.That(bossControllerType, Is.Not.Null);
            Assert.That(bossDataType, Is.Not.Null);

            var playerRegistry =
                new NetworkPlayerRegistry();

            GameObject spawnerObject =
                CreateGameObject("Enemy Spawner");

            spawnerObject.SetActive(false);

            Component spawner =
                spawnerObject.AddComponent(
                    spawnerType);

            GameObject bossObject =
                CreateGameObject("Boss");

            bossObject.SetActive(false);

            NetworkEntityId identifier =
                bossObject.AddComponent<NetworkEntityId>();

            Assert.That(
                identifier.TryAssign(
                    99u,
                    NetworkEntityType.Boss),
                Is.True);

            Component bossHealth =
                bossObject.AddComponent(
                    bossHealthType);

            ScriptableObject bossData =
                ScriptableObject.CreateInstance(
                    bossDataType);

            createdObjects.Add(bossData);

            FieldInfo maxHealthField =
                bossDataType.GetField(
                    "maxHealth",
                    BindingFlags.Instance |
                    BindingFlags.NonPublic);

            Assert.That(
                maxHealthField,
                Is.Not.Null);

            maxHealthField.SetValue(
                bossData,
                100);

            FieldInfo bossDataReferenceField =
                bossHealthType.GetField(
                    "bossData",
                    BindingFlags.Instance |
                    BindingFlags.NonPublic);

            Assert.That(
                bossDataReferenceField,
                Is.Not.Null);

            bossDataReferenceField.SetValue(
                bossHealth,
                bossData);

            MethodInfo healthAwakeMethod =
                bossHealthType.GetMethod(
                    "Awake",
                    BindingFlags.Instance |
                    BindingFlags.NonPublic);

            Assert.That(
                healthAwakeMethod,
                Is.Not.Null);

            healthAwakeMethod.Invoke(
                bossHealth,
                null);

            FieldInfo currentHealthField =
                bossHealthType.GetField(
                    "currentHealth",
                    BindingFlags.Instance |
                    BindingFlags.NonPublic);

            Assert.That(
                currentHealthField,
                Is.Not.Null);

            currentHealthField.SetValue(
                bossHealth,
                40);

            Component bossController =
                bossObject.AddComponent(
                    bossControllerType);

            FieldInfo isPhaseTwoField =
                bossControllerType.GetField(
                    "isPhaseTwo",
                    BindingFlags.Instance |
                    BindingFlags.NonPublic);

            Assert.That(
                isPhaseTwoField,
                Is.Not.Null);

            isPhaseTwoField.SetValue(
                bossController,
                true);

            GameObject encounterObject =
                CreateGameObject("Boss Encounter");

            encounterObject.SetActive(false);

            Component encounter =
                encounterObject.AddComponent(
                    encounterType);

            FieldInfo currentBossField =
                encounterType.GetField(
                    "currentBoss",
                    BindingFlags.Instance |
                    BindingFlags.NonPublic);

            Assert.That(
                currentBossField,
                Is.Not.Null);

            currentBossField.SetValue(
                encounter,
                bossObject);

            GameObject publisherObject =
                CreateGameObject("Publisher");

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
                    "CollectCurrentBossHealth",
                    BindingFlags.Instance |
                    BindingFlags.Public);

            Assert.That(
                collectMethod,
                Is.Not.Null,
                "CollectCurrentBossHealth must exist.");

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

            object stateValue =
                states[0];

            Type stateType =
                stateValue.GetType();

            Assert.That(
                stateType.GetProperty("EntityId"),
                Is.Not.Null);

            Assert.That(
                stateType.GetProperty("Phase"),
                Is.Not.Null);

            Assert.That(
                stateType.GetProperty("CurrentHealth"),
                Is.Not.Null);

            Assert.That(
                stateType.GetProperty("MaxHealth"),
                Is.Not.Null);

            Assert.That(
                stateType.GetProperty("IsDead"),
                Is.Not.Null);

            Assert.That(
                stateType.GetProperty("EntityId")
                    .GetValue(stateValue, null),
                Is.EqualTo(99u));

            Assert.That(
                stateType.GetProperty("Phase")
                    .GetValue(stateValue, null),
                Is.EqualTo((byte)2));

            Assert.That(
                stateType.GetProperty("CurrentHealth")
                    .GetValue(stateValue, null),
                Is.EqualTo((ushort)40));

            Assert.That(
                stateType.GetProperty("MaxHealth")
                    .GetValue(stateValue, null),
                Is.EqualTo((ushort)100));

            Assert.That(
                stateType.GetProperty("IsDead")
                    .GetValue(stateValue, null),
                Is.False);
        }

        private GameObject CreateGameObject(
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