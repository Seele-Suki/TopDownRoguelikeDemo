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
    public sealed class HostWorldSnapshotEntityIdentifierTests
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
        public void CollectCurrentWorldEntityIds_ReturnsIdsAndTypes()
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
                CreateObject("Player");

            NetworkEntityId playerIdentifier =
                player.AddComponent<NetworkEntityId>();

            Assert.That(
                playerIdentifier.TryAssign(
                    11u,
                    NetworkEntityType.Player),
                Is.True);

            playerRegistry.TryRegister(
                1u,
                player);

            GameObject remotePlayer =
                CreateObject("Remote Player");

            NetworkEntityId remotePlayerIdentifier =
                remotePlayer.AddComponent<NetworkEntityId>();

            Assert.That(
                remotePlayerIdentifier.TryAssign(
                    12u,
                    NetworkEntityType.Player),
                Is.True);

            Assert.That(
                playerRegistry.TryRegister(
                    2u,
                    remotePlayer),
                Is.True);

            GameObject spawnerObject =
                CreateObject("Enemy Spawner");

            GameObject enemy =
                CreateObject("Enemy");

            NetworkEntityId enemyIdentifier =
                enemy.AddComponent<NetworkEntityId>();

            Assert.That(
                enemyIdentifier.TryAssign(
                    22u,
                    NetworkEntityType.Enemy),
                Is.True);

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

            NetworkEntityId bossIdentifier =
                boss.AddComponent<NetworkEntityId>();

            Assert.That(
                bossIdentifier.TryAssign(
                    33u,
                    NetworkEntityType.Boss),
                Is.True);

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
                    "CollectCurrentWorldEntityIds",
                    BindingFlags.Instance |
                    BindingFlags.Public);

            Assert.That(
                collectMethod,
                Is.Not.Null,
                "CollectCurrentWorldEntityIds must exist.");

            object result =
                collectMethod.Invoke(
                    publisher,
                    null);

            var identifiers =
                new List<NetworkEntityId>(
                    (IEnumerable<NetworkEntityId>)result);

            Assert.That(
                identifiers,
                Has.Count.EqualTo(4));

            Assert.That(
                identifiers.Exists(
                    identifier =>
                        identifier.EntityId == 11u &&
                        identifier.EntityType ==
                        NetworkEntityType.Player),
                Is.True);

            Assert.That(
                identifiers.Exists(
                    identifier =>
                        identifier.EntityId == 12u &&
                        identifier.EntityType ==
                        NetworkEntityType.Player),
                Is.True);

            Assert.That(
                identifiers.Exists(
                    identifier =>
                        identifier.EntityId == 22u &&
                        identifier.EntityType ==
                        NetworkEntityType.Enemy),
                Is.True);

            Assert.That(
                identifiers.Exists(
                    identifier =>
                        identifier.EntityId == 33u &&
                        identifier.EntityType ==
                        NetworkEntityType.Boss),
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
