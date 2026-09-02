using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using TopDownRoguelike.Networking.Protocol;

namespace TopDownRoguelike.Tests.EditMode
{
    public sealed class BossEncounterWorldEntityTests
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
        public void CurrentBoss_ReturnsCurrentBossObjectReadOnly()
        {
            Type encounterType =
                FindType(
                    "TopDownRoguelike.Gameplay.Bosses." +
                    "BossEncounterController");

            Assert.That(
                encounterType,
                Is.Not.Null,
                "BossEncounterController must exist.");

            GameObject encounterObject =
                new GameObject("Boss Encounter Test");

            GameObject bossObject =
                new GameObject("Current Boss");

            createdObjects.Add(encounterObject);
            createdObjects.Add(bossObject);

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
                Is.Not.Null,
                "currentBoss field must exist.");

            currentBossField.SetValue(
                encounter,
                bossObject);

            PropertyInfo currentBossProperty =
                encounterType.GetProperty(
                    "CurrentBoss",
                    BindingFlags.Instance |
                    BindingFlags.Public);

            Assert.That(
                currentBossProperty,
                Is.Not.Null,
                "CurrentBoss property must exist.");

            Assert.That(
                currentBossProperty.CanWrite,
                Is.False,
                "CurrentBoss must be read-only.");

            object currentBoss =
                currentBossProperty.GetValue(
                    encounter,
                    null);

            Assert.That(
                currentBoss,
                Is.SameAs(bossObject));
        }

        [Test]
        public void BossSpawnPublisher_ExistsForNetworkAppearance()
        {
            Type publisherType =
                FindType(
                    "TopDownRoguelike.Gameplay.Networking." +
                    "HostBossSpawnPublisher");

            Assert.That(
                publisherType,
                Is.Not.Null,
                "HostBossSpawnPublisher must exist.");

            Assert.That(
                publisherType.GetMethod(
                    "Configure",
                    BindingFlags.Instance |
                    BindingFlags.Public),
                Is.Not.Null,
                "HostBossSpawnPublisher must expose Configure.");
        }

        [Test]
        public void ClientWorldSnapshotConsumer_AcceptsBossSpawnRecords()
        {
            Type consumerType =
                FindType(
                    "TopDownRoguelike.Gameplay.Networking." +
                    "ClientWorldSnapshotConsumer");

            Assert.That(consumerType, Is.Not.Null);

            WorldEntityRecord record =
                new WorldEntityRecord(
                    0x20000001u,
                    NetworkEntityType.Boss,
                    WorldEntityLifecycle.Spawn,
                    WorldEntityFlags.Active,
                    1f,
                    2f,
                    0f,
                    100,
                    100,
                    1);

            GameObject consumerObject =
                new GameObject("Client World Consumer");
            createdObjects.Add(consumerObject);
            consumerObject.SetActive(false);

            Component consumer =
                consumerObject.AddComponent(consumerType);

            MethodInfo enqueueSpawn =
                consumerType.GetMethod(
                    "EnqueueSpawn",
                    BindingFlags.Instance |
                    BindingFlags.Public);

            Assert.That(enqueueSpawn, Is.Not.Null);
            Assert.That(
                enqueueSpawn.Invoke(
                    consumer,
                    new object[] { record }),
                Is.True);
        }

        [Test]
        public void BossHealth_ExposesAuthoritativeStateEntryPoint()
        {
            Type healthType = FindType(
                "TopDownRoguelike.Gameplay.Bosses.BossHealth");
            Type controllerType = FindType(
                "TopDownRoguelike.Gameplay.Bosses.BossController");

            Assert.That(healthType, Is.Not.Null);
            Assert.That(
                healthType.GetMethod(
                    "ApplyAuthoritativeState",
                    BindingFlags.Instance |
                    BindingFlags.Public),
                Is.Not.Null);
            Assert.That(controllerType, Is.Not.Null);
            Assert.That(
                controllerType.GetMethod(
                    "ApplyAuthoritativePhase",
                    BindingFlags.Instance |
                    BindingFlags.Public),
                Is.Not.Null);
        }

        [Test]
        public void ClientWorldSnapshotConsumer_AcceptsBossDeathRemoval()
        {
            Type consumerType = FindType(
                "TopDownRoguelike.Gameplay.Networking." +
                "ClientWorldSnapshotConsumer");
            Assert.That(consumerType, Is.Not.Null);

            GameObject consumerObject =
                new GameObject("Client World Consumer Death");
            createdObjects.Add(consumerObject);
            consumerObject.SetActive(false);

            Component consumer =
                consumerObject.AddComponent(consumerType);
            MethodInfo enqueueRemoval = consumerType.GetMethod(
                "EnqueueRemoval",
                BindingFlags.Instance |
                BindingFlags.Public,
                null,
                new[] { typeof(WorldEntityRemovedPayload) },
                null);

            Assert.That(enqueueRemoval, Is.Not.Null);
            Assert.That(
                enqueueRemoval.Invoke(
                    consumer,
                    new object[]
                    {
                        new WorldEntityRemovedPayload(
                            0x20000001u,
                            NetworkEntityType.Boss,
                            WorldEntityRemovalReason.Died)
                    }),
                Is.True);
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
