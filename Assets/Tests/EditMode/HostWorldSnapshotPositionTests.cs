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
    public sealed class HostWorldSnapshotPositionTests
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
        public void CollectCurrentWorldEntityPositions_ReturnsEntityPosition()
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

            player.transform.position =
                new Vector3(
                    3.5f,
                    -2.25f,
                    7f);

            NetworkEntityId identifier =
                player.AddComponent<NetworkEntityId>();

            Assert.That(
                identifier.TryAssign(
                    11u,
                    NetworkEntityType.Player),
                Is.True);

            playerRegistry.TryRegister(
                1u,
                player);

            GameObject spawnerObject =
                CreateObject("Enemy Spawner");

            spawnerObject.SetActive(false);

            Component spawner =
                spawnerObject.AddComponent(
                    spawnerType);

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
                    "CollectCurrentWorldEntityPositions",
                    BindingFlags.Instance |
                    BindingFlags.Public);

            Assert.That(
                collectMethod,
                Is.Not.Null,
                "CollectCurrentWorldEntityPositions must exist.");

            object result =
                collectMethod.Invoke(
                    publisher,
                    null);

            var positions =
                (IReadOnlyDictionary<
                    NetworkEntityId,
                    Vector2>)result;

            Assert.That(
                positions,
                Has.Count.EqualTo(1));

            Assert.That(
                positions.ContainsKey(identifier),
                Is.True);

            Assert.That(
                positions[identifier].x,
                Is.EqualTo(3.5f).Within(0.001f));

            Assert.That(
                positions[identifier].y,
                Is.EqualTo(-2.25f).Within(0.001f));
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