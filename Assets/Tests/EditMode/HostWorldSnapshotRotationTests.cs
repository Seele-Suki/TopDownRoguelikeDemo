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
    public sealed class HostWorldSnapshotRotationTests
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
        public void CollectCurrentWorldEntityRotations_ReturnsRotationZ()
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

            player.transform.rotation =
                Quaternion.Euler(
                    0f,
                    0f,
                    135f);

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
                    "CollectCurrentWorldEntityRotations",
                    BindingFlags.Instance |
                    BindingFlags.Public);

            Assert.That(
                collectMethod,
                Is.Not.Null,
                "CollectCurrentWorldEntityRotations must exist.");

            object result =
                collectMethod.Invoke(
                    publisher,
                    null);

            var rotations =
                (IReadOnlyDictionary<
                    NetworkEntityId,
                    float>)result;

            Assert.That(
                rotations,
                Has.Count.EqualTo(1));

            Assert.That(
                rotations.ContainsKey(identifier),
                Is.True);

            Assert.That(
                rotations[identifier],
                Is.EqualTo(135f).Within(0.001f));
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