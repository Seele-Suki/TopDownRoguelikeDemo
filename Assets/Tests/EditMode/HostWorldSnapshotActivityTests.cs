using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TopDownRoguelike.Networking.Gameplay;
using TopDownRoguelike.Networking.Protocol;
using UnityEngine;

namespace TopDownRoguelike.Tests.EditMode
{
    public sealed class HostWorldSnapshotActivityTests
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
        public void CollectCurrentWorldEntityActivity_ReturnsActiveState()
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

            GameObject activePlayer =
                CreateObject("Active Player");

            NetworkEntityId activeIdentifier =
                activePlayer.AddComponent<NetworkEntityId>();

            Assert.That(
                activeIdentifier.TryAssign(
                    11u,
                    NetworkEntityType.Player),
                Is.True);

            playerRegistry.TryRegister(
                1u,
                activePlayer);

            GameObject inactivePlayer =
                CreateObject("Inactive Player");

            NetworkEntityId inactiveIdentifier =
                inactivePlayer.AddComponent<NetworkEntityId>();

            Assert.That(
                inactiveIdentifier.TryAssign(
                    12u,
                    NetworkEntityType.Player),
                Is.True);

            inactivePlayer.SetActive(false);

            playerRegistry.TryRegister(
                2u,
                inactivePlayer);

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
                    "CollectCurrentWorldEntityActivity",
                    BindingFlags.Instance |
                    BindingFlags.Public);

            Assert.That(
                collectMethod,
                Is.Not.Null,
                "CollectCurrentWorldEntityActivity must exist.");

            object result =
                collectMethod.Invoke(
                    publisher,
                    null);

            var activity =
                (IReadOnlyDictionary<
                    NetworkEntityId,
                    bool>)result;

            Assert.That(
                activity,
                Has.Count.EqualTo(2));

            Assert.That(
                activity[activeIdentifier],
                Is.True);

            Assert.That(
                activity[inactiveIdentifier],
                Is.False);
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