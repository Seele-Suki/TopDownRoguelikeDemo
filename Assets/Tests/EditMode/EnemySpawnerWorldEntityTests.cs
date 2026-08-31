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
    public sealed class EnemySpawnerWorldEntityTests
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
            GameSession.Reset();
        }

        [Test]
        public void EnumerateSpawnedEnemies_ReturnsRegisteredEnemies()
        {
            GameObject spawnerObject =
                new GameObject("Enemy Spawner Test");

            GameObject firstEnemy =
                new GameObject("First Enemy");

            GameObject secondEnemy =
                new GameObject("Second Enemy");

            createdObjects.Add(spawnerObject);
            createdObjects.Add(firstEnemy);
            createdObjects.Add(secondEnemy);

            spawnerObject.SetActive(false);

            Type enemySpawnerType =
                FindType(
                    "TopDownRoguelike.Gameplay.Enemies." +
                    "EnemySpawner");

            Assert.That(
                enemySpawnerType,
                Is.Not.Null,
                "EnemySpawner must exist.");

            Component spawner =
                spawnerObject.AddComponent(
                    enemySpawnerType);

            FieldInfo spawnedEnemiesField =
                enemySpawnerType.GetField(
                    "spawnedEnemies",
                    BindingFlags.Instance |
                    BindingFlags.NonPublic);

            Assert.That(
                spawnedEnemiesField,
                Is.Not.Null,
                "EnemySpawner.spawnedEnemies must exist.");

            var spawnedEnemies =
                (List<GameObject>)spawnedEnemiesField.GetValue(
                    spawner);

            spawnedEnemies.Add(firstEnemy);
            spawnedEnemies.Add(secondEnemy);

            MethodInfo enumerateMethod =
                enemySpawnerType.GetMethod(
                    "EnumerateSpawnedEnemies",
                    BindingFlags.Instance |
                    BindingFlags.Public);

            Assert.That(
                enumerateMethod,
                Is.Not.Null,
                "EnemySpawner.EnumerateSpawnedEnemies must exist.");

            object result =
                enumerateMethod.Invoke(
                    spawner,
                    null);

            var entries =
                new List<GameObject>(
                    (IEnumerable<GameObject>)result);

            Assert.That(
                entries,
                Has.Count.EqualTo(2));

            Assert.That(
                entries.Contains(firstEnemy),
                Is.True);

            Assert.That(
                entries.Contains(secondEnemy),
                Is.True);
        }

        [TestCase(GameMode.SinglePlayer)]
        [TestCase(GameMode.MultiplayerHost)]
        public void TryCreateSpawnedEnemy_AssignsUniqueEnemyIds(
            GameMode gameMode)
        {
            ConfigureGameMode(gameMode);

            GameObject spawnerObject =
                new GameObject("Enemy Spawner Test");

            GameObject enemyPrefab =
                new GameObject("Enemy Prefab Test");

            createdObjects.Add(spawnerObject);
            createdObjects.Add(enemyPrefab);

            spawnerObject.SetActive(false);

            Type enemySpawnerType =
                FindType(
                    "TopDownRoguelike.Gameplay.Enemies." +
                    "EnemySpawner");

            Assert.That(
                enemySpawnerType,
                Is.Not.Null,
                "EnemySpawner must exist.");

            Component spawner =
                spawnerObject.AddComponent(
                    enemySpawnerType);

            MethodInfo createMethod =
                enemySpawnerType.GetMethod(
                    "TryCreateSpawnedEnemy",
                    BindingFlags.Instance |
                    BindingFlags.NonPublic);

            Assert.That(
                createMethod,
                Is.Not.Null,
                "EnemySpawner must expose its internal " +
                "enemy creation step.");

            Vector3 spawnPosition =
                new Vector3(2f, 3f, 0f);

            object[] arguments =
            {
                enemyPrefab,
                spawnPosition,
                null
            };

            bool wasCreated =
                (bool)createMethod.Invoke(
                    spawner,
                    arguments);

            GameObject spawnedEnemy =
                arguments[2] as GameObject;

            createdObjects.Add(spawnedEnemy);

            Assert.That(wasCreated, Is.True);
            Assert.That(spawnedEnemy, Is.Not.Null);
            Assert.That(
                spawnedEnemy.transform.position,
                Is.EqualTo(spawnPosition));

            NetworkEntityId firstIdentifier =
                spawnedEnemy.GetComponent<NetworkEntityId>();

            Assert.That(firstIdentifier, Is.Not.Null);
            Assert.That(firstIdentifier.IsAssigned, Is.True);
            Assert.That(
                firstIdentifier.EntityId,
                Is.EqualTo(0x10000001u));
            Assert.That(
                firstIdentifier.EntityType,
                Is.EqualTo(NetworkEntityType.Enemy));

            object[] secondArguments =
            {
                enemyPrefab,
                new Vector3(4f, 5f, 0f),
                null
            };

            bool secondWasCreated =
                (bool)createMethod.Invoke(
                    spawner,
                    secondArguments);

            GameObject secondEnemy =
                secondArguments[2] as GameObject;

            createdObjects.Add(secondEnemy);

            Assert.That(secondWasCreated, Is.True);
            Assert.That(secondEnemy, Is.Not.Null);

            NetworkEntityId secondIdentifier =
                secondEnemy.GetComponent<NetworkEntityId>();

            Assert.That(secondIdentifier, Is.Not.Null);
            Assert.That(secondIdentifier.IsAssigned, Is.True);
            Assert.That(
                secondIdentifier.EntityId,
                Is.EqualTo(0x10000002u));
            Assert.That(
                secondIdentifier.EntityId,
                Is.Not.EqualTo(firstIdentifier.EntityId));
            Assert.That(
                secondIdentifier.EntityType,
                Is.EqualTo(NetworkEntityType.Enemy));

            MethodInfo enumerateMethod =
                enemySpawnerType.GetMethod(
                    "EnumerateSpawnedEnemies",
                    BindingFlags.Instance |
                    BindingFlags.Public);

            var entries =
                new List<GameObject>(
                    (IEnumerable<GameObject>)
                    enumerateMethod.Invoke(
                        spawner,
                        null));

            Assert.That(entries, Has.Count.EqualTo(2));
            Assert.That(entries[0], Is.SameAs(spawnedEnemy));
            Assert.That(entries[1], Is.SameAs(secondEnemy));
        }

        [Test]
        public void TryCreateSpawnedEnemy_RejectsMultiplayerClient()
        {
            GameSession.ConfigureMultiplayerClient();

            GameObject spawnerObject =
                new GameObject("Enemy Spawner Test");

            GameObject enemyPrefab =
                new GameObject("Enemy Prefab Test");

            createdObjects.Add(spawnerObject);
            createdObjects.Add(enemyPrefab);

            spawnerObject.SetActive(false);

            Type enemySpawnerType =
                FindType(
                    "TopDownRoguelike.Gameplay.Enemies." +
                    "EnemySpawner");

            Assert.That(
                enemySpawnerType,
                Is.Not.Null,
                "EnemySpawner must exist.");

            Component spawner =
                spawnerObject.AddComponent(
                    enemySpawnerType);

            MethodInfo createMethod =
                enemySpawnerType.GetMethod(
                    "TryCreateSpawnedEnemy",
                    BindingFlags.Instance |
                    BindingFlags.NonPublic);

            Assert.That(
                createMethod,
                Is.Not.Null,
                "EnemySpawner must expose its internal " +
                "enemy creation step.");

            object[] arguments =
            {
                enemyPrefab,
                Vector3.zero,
                null
            };

            bool wasCreated =
                (bool)createMethod.Invoke(
                    spawner,
                    arguments);

            GameObject spawnedEnemy =
                arguments[2] as GameObject;

            createdObjects.Add(spawnedEnemy);

            Assert.That(wasCreated, Is.False);
            Assert.That(spawnedEnemy, Is.Null);

            MethodInfo enumerateMethod =
                enemySpawnerType.GetMethod(
                    "EnumerateSpawnedEnemies",
                    BindingFlags.Instance |
                    BindingFlags.Public);

            var entries =
                new List<GameObject>(
                    (IEnumerable<GameObject>)
                    enumerateMethod.Invoke(
                        spawner,
                        null));

            Assert.That(entries, Is.Empty);
        }

        private static void ConfigureGameMode(
            GameMode gameMode)
        {
            switch (gameMode)
            {
                case GameMode.SinglePlayer:
                    GameSession.ConfigureSinglePlayer();
                    return;

                case GameMode.MultiplayerHost:
                    GameSession.ConfigureMultiplayerHost();
                    return;

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(gameMode),
                        gameMode,
                        "Unsupported authoritative game mode.");
            }
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
