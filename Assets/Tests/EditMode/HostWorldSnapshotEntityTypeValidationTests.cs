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
    public sealed class HostWorldSnapshotEntityTypeValidationTests
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
        public void CollectCurrentWorldEntityIds_RejectsEnemyWithPlayerType()
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

            GameObject spawnerObject =
                CreateObject("Enemy Spawner");

            GameObject enemy =
                CreateObject("Wrongly Typed Enemy");

            NetworkEntityId enemyIdentifier =
                enemy.AddComponent<NetworkEntityId>();

            Assert.That(
                enemyIdentifier.TryAssign(
                    22u,
                    NetworkEntityType.Player),
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
                    "CollectCurrentWorldEntityIds",
                    BindingFlags.Instance |
                    BindingFlags.Public);

            Assert.That(
                collectMethod,
                Is.Not.Null);

            TargetInvocationException exception =
                Assert.Throws<TargetInvocationException>(
                    () =>
                        collectMethod.Invoke(
                            publisher,
                            null));

            Assert.That(
                exception.InnerException,
                Is.TypeOf<InvalidOperationException>());

            StringAssert.Contains(
                "expected Enemy",
                exception.InnerException.Message);
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