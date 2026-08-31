using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TopDownRoguelike.Networking.Gameplay;
using UnityEngine;

namespace TopDownRoguelike.Tests.EditMode
{
    public sealed class HostWorldSnapshotWorldEntityTests
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
        public void CollectCurrentWorldEntities_ReturnsPlayersEnemiesAndBoss()
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

            Assert.That(publisherType, Is.Not.Null);
            Assert.That(spawnerType, Is.Not.Null);
            Assert.That(encounterType, Is.Not.Null);

            var playerRegistry =
                new NetworkPlayerRegistry();

            GameObject player =
                CreateObject("Host Player");

            playerRegistry.TryRegister(
                1u,
                player);

            GameObject spawnerObject =
                CreateObject("Enemy Spawner");

            GameObject enemy =
                CreateObject("Regular Enemy");

            spawnerObject.SetActive(false);

            Component spawner =
                spawnerObject.AddComponent(
                    spawnerType);

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

            GameObject boss =
                CreateObject("Boss");

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
                boss);

            GameObject publisherObject =
                CreateObject("World Snapshot Publisher");

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
                Is.Not.Null,
                "ConfigureWorldSources must exist.");

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
                    "CollectCurrentWorldEntities",
                    BindingFlags.Instance |
                    BindingFlags.Public);

            Assert.That(
                collectMethod,
                Is.Not.Null,
                "CollectCurrentWorldEntities must exist.");

            object result =
                collectMethod.Invoke(
                    publisher,
                    null);

            var entities =
                new List<GameObject>(
                    (IEnumerable<GameObject>)result);

            Assert.That(
                entities,
                Has.Count.EqualTo(3));

            Assert.That(
                entities.Contains(player),
                Is.True);

            Assert.That(
                entities.Contains(enemy),
                Is.True);

            Assert.That(
                entities.Contains(boss),
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