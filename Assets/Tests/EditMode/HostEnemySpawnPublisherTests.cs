using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TopDownRoguelike.Infrastructure;
using TopDownRoguelike.Networking.Protocol;
using UnityEngine;

namespace TopDownRoguelike.Tests.EditMode
{
    public sealed class HostEnemySpawnPublisherTests
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
        public void SpawnedEnemy_PublishesCompleteSpawnRecord()
        {
            GameSession.ConfigureMultiplayerHost();

            Type spawnerType =
                FindType(
                    "TopDownRoguelike.Gameplay.Enemies." +
                    "EnemySpawner");

            Type enemyHealthType =
                FindType(
                    "TopDownRoguelike.Gameplay.Enemies." +
                    "EnemyHealth");

            Type publisherType =
                FindType(
                    "TopDownRoguelike.Gameplay.Networking." +
                    "HostEnemySpawnPublisher");

            Assert.That(spawnerType, Is.Not.Null);
            Assert.That(enemyHealthType, Is.Not.Null);
            Assert.That(
                publisherType,
                Is.Not.Null,
                "HostEnemySpawnPublisher must exist.");

            GameObject spawnerObject =
                CreateObject("Enemy Spawner Test");

            GameObject publisherObject =
                CreateObject("Enemy Spawn Publisher Test");

            GameObject enemyPrefab =
                CreateObject("Enemy Prefab Test");

            spawnerObject.SetActive(false);
            publisherObject.SetActive(false);

            enemyPrefab.AddComponent(
                enemyHealthType);

            Component spawner =
                spawnerObject.AddComponent(
                    spawnerType);

            Component publisher =
                publisherObject.AddComponent(
                    publisherType);

            WorldEntityRecord publishedRecord =
                null;

            Action<WorldEntityRecord> sender =
                record => publishedRecord = record;

            MethodInfo configureMethod =
                publisherType.GetMethod(
                    "Configure",
                    BindingFlags.Instance |
                    BindingFlags.Public);

            Assert.That(configureMethod, Is.Not.Null);

            configureMethod.Invoke(
                publisher,
                new object[]
                {
                    spawner,
                    sender
                });

            MethodInfo createMethod =
                spawnerType.GetMethod(
                    "TryCreateSpawnedEnemy",
                    BindingFlags.Instance |
                    BindingFlags.NonPublic);

            Assert.That(createMethod, Is.Not.Null);

            Vector3 spawnPosition =
                new Vector3(2f, -4f, 0f);

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
            Assert.That(publishedRecord, Is.Not.Null);
            Assert.That(
                publishedRecord.EntityId,
                Is.EqualTo(0x10000001u));
            Assert.That(
                publishedRecord.EntityType,
                Is.EqualTo(NetworkEntityType.Enemy));
            Assert.That(
                publishedRecord.Lifecycle,
                Is.EqualTo(WorldEntityLifecycle.Spawn));
            Assert.That(
                publishedRecord.Flags,
                Is.EqualTo(WorldEntityFlags.Active));
            Assert.That(
                publishedRecord.PositionX,
                Is.EqualTo(spawnPosition.x));
            Assert.That(
                publishedRecord.PositionY,
                Is.EqualTo(spawnPosition.y));
            Assert.That(publishedRecord.CurrentHealth, Is.EqualTo(3));
            Assert.That(publishedRecord.MaxHealth, Is.EqualTo(3));
            Assert.That(
                publishedRecord.EnemyArchetype,
                Is.EqualTo(NetworkEnemyArchetype.Basic));
        }

        private GameObject CreateObject(
            string objectName)
        {
            var result =
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
