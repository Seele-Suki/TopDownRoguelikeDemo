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
    public sealed class HostEnemyMovementSnapshotTests
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
        public void BuildCurrentWorldSnapshot_UsesEnemyPositionAndMoveDirection()
        {
            GameSession.ConfigureSinglePlayer();

            Type publisherType =
                FindType(
                    "TopDownRoguelike.Gameplay.Networking." +
                    "HostWorldSnapshotPublisher");

            Type spawnerType =
                FindType(
                    "TopDownRoguelike.Gameplay.Enemies." +
                    "EnemySpawner");

            Type movementType =
                FindType(
                    "TopDownRoguelike.Gameplay.Enemies." +
                    "EnemyMovement");

            Type healthType =
                FindType(
                    "TopDownRoguelike.Gameplay.Enemies." +
                    "EnemyHealth");

            Type encounterType =
                FindType(
                    "TopDownRoguelike.Gameplay.Bosses." +
                    "BossEncounterController");

            Assert.That(publisherType, Is.Not.Null);
            Assert.That(spawnerType, Is.Not.Null);
            Assert.That(movementType, Is.Not.Null);
            Assert.That(healthType, Is.Not.Null);
            Assert.That(encounterType, Is.Not.Null);

            GameObject publisherObject =
                CreateObject("Enemy Movement Snapshot Publisher");

            GameObject spawnerObject =
                CreateObject("Enemy Movement Snapshot Spawner");

            GameObject encounterObject =
                CreateObject("Enemy Movement Snapshot Encounter");

            GameObject enemyPrefab =
                CreateObject("Enemy Movement Snapshot Prefab");

            publisherObject.SetActive(false);
            spawnerObject.SetActive(false);
            encounterObject.SetActive(false);

            enemyPrefab.AddComponent<Rigidbody2D>();
            enemyPrefab.AddComponent(healthType);
            enemyPrefab.AddComponent(movementType);

            Component publisher =
                publisherObject.AddComponent(
                    publisherType);

            Component spawner =
                spawnerObject.AddComponent(
                    spawnerType);

            Component encounter =
                encounterObject.AddComponent(
                    encounterType);

            MethodInfo createEnemyMethod =
                spawnerType.GetMethod(
                    "TryCreateSpawnedEnemy",
                    BindingFlags.Instance |
                    BindingFlags.NonPublic);

            Assert.That(createEnemyMethod, Is.Not.Null);

            var expectedPosition =
                new Vector3(4.5f, -2.25f, 0f);

            object[] createArguments =
            {
                enemyPrefab,
                expectedPosition,
                null
            };

            bool wasCreated =
                (bool)createEnemyMethod.Invoke(
                    spawner,
                    createArguments);

            GameObject spawnedEnemy =
                createArguments[2] as GameObject;

            createdObjects.Add(spawnedEnemy);

            Assert.That(wasCreated, Is.True);
            Assert.That(spawnedEnemy, Is.Not.Null);

            spawnedEnemy.transform.rotation =
                Quaternion.Euler(0f, 0f, 17f);

            Component movement =
                spawnedEnemy.GetComponent(
                    movementType);

            Assert.That(movement, Is.Not.Null);

            SetPrivateField(
                movement,
                "moveDirection",
                Vector2.up);

            MethodInfo configureMethod =
                publisherType.GetMethod(
                    "ConfigureWorldSources",
                    BindingFlags.Instance |
                    BindingFlags.Public);

            Assert.That(configureMethod, Is.Not.Null);

            configureMethod.Invoke(
                publisher,
                new object[]
                {
                    new NetworkPlayerRegistry(),
                    spawner,
                    encounter
                });

            MethodInfo buildMethod =
                publisherType.GetMethod(
                    "BuildCurrentWorldSnapshot",
                    BindingFlags.Instance |
                    BindingFlags.Public);

            Assert.That(buildMethod, Is.Not.Null);

            var snapshot =
                (WorldStateSnapshotPayload)
                buildMethod.Invoke(
                    publisher,
                    null);

            Assert.That(snapshot.Entities, Has.Count.EqualTo(1));

            WorldEntityRecord record =
                snapshot.Entities[0];

            Assert.That(
                record.EntityType,
                Is.EqualTo(NetworkEntityType.Enemy));

            Assert.That(
                record.PositionX,
                Is.EqualTo(expectedPosition.x).Within(0.001f));

            Assert.That(
                record.PositionY,
                Is.EqualTo(expectedPosition.y).Within(0.001f));

            Assert.That(
                record.RotationDegrees,
                Is.EqualTo(90f).Within(0.001f),
                "Enemy snapshot facing must follow " +
                "EnemyMovement.MoveDirection.");
        }

        private GameObject CreateObject(
            string objectName)
        {
            var result =
                new GameObject(objectName);

            createdObjects.Add(result);
            return result;
        }

        private static void SetPrivateField(
            Component target,
            string fieldName,
            object value)
        {
            FieldInfo field =
                target.GetType().GetField(
                    fieldName,
                    BindingFlags.Instance |
                    BindingFlags.NonPublic);

            Assert.That(
                field,
                Is.Not.Null,
                $"{fieldName} must exist.");

            field.SetValue(target, value);
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
