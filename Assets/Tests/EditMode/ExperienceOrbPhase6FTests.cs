using System;
using System.Reflection;
using NUnit.Framework;
using TopDownRoguelike.Infrastructure;
using TopDownRoguelike.Networking.Protocol;
using UnityEngine;

namespace TopDownRoguelike.Tests.EditMode
{
    public sealed class ExperienceOrbPhase6FTests
    {
        [Test]
        public void Initialize_ResetsCollectedState()
        {
            var owner = new GameObject("Experience Orb Test");
            try
            {
                Type orbType = FindType(
                    "TopDownRoguelike.Gameplay.Experience.ExperienceOrb");
                Component orb = owner.AddComponent(orbType);
                orbType.GetMethod("Initialize").Invoke(
                    orb,
                    new object[] { 3 });

                Assert.That(
                    orbType.GetProperty("IsCollected").GetValue(orb),
                    Is.False);
                Assert.That(
                    orbType.GetProperty("ExperienceAmount").GetValue(orb),
                    Is.EqualTo(3));

                Assert.That(
                    orbType.GetMethod("TryCollect").Invoke(orb, null),
                    Is.True);
                Assert.That(
                    orbType.GetProperty("IsCollected").GetValue(orb),
                    Is.True);
                Assert.That(
                    orbType.GetMethod("TryCollect").Invoke(orb, null),
                    Is.False);

                orbType.GetMethod("Initialize").Invoke(
                    orb,
                    new object[] { 5 });
                Assert.That(
                    orbType.GetProperty("IsCollected").GetValue(orb),
                    Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(owner);
            }
        }

        [Test]
        public void SpawnRecord_PreservesExperienceAmount()
        {
            var record = new WorldEntityRecord(
                0x40000001u,
                NetworkEntityType.ExperienceOrb,
                WorldEntityLifecycle.Spawn,
                WorldEntityFlags.Active,
                1f,
                2f,
                0f,
                0,
                0,
                experienceAmount: 7);

            var decoded = WorldEntitySpawnedCodec.Decode(
                WorldEntitySpawnedCodec.Encode(record));

            Assert.That(decoded.ExperienceAmount, Is.EqualTo(7));
        }

        [Test]
        public void SpawnRecord_RejectsMissingOrbExperienceAmount()
        {
            var record = new WorldEntityRecord(
                0x40000002u,
                NetworkEntityType.ExperienceOrb,
                WorldEntityLifecycle.Spawn,
                WorldEntityFlags.Active,
                0f,
                0f,
                0f,
                0,
                0);

            Assert.Throws<ArgumentException>(
                () => WorldEntitySpawnedCodec.Encode(record));
        }

        [Test]
        public void Pool_DoesNotGenerateExperienceOrbForClient()
        {
            GameSession.ConfigureMultiplayerClient();
            var owner = new GameObject("Client Orb Pool Test");
            try
            {
                Type poolType = FindType(
                    "TopDownRoguelike.Gameplay.Experience.ExperienceOrbPool");
                Component pool = owner.AddComponent(poolType);
                Assert.That(
                    poolType.GetMethod("GetOrb").Invoke(
                        pool,
                        new object[] { Vector3.zero, 1 }),
                    Is.Null);
            }
            finally
            {
                GameSession.Reset();
                UnityEngine.Object.DestroyImmediate(owner);
            }
        }

        [Test]
        public void Pool_AssignsExperienceOrbEntityIdForHost()
        {
            GameSession.ConfigureMultiplayerHost();
            var root = new GameObject("Host Orb Pool Test");
            root.SetActive(false);
            var prefabObject = new GameObject("Experience Orb Prefab");
            prefabObject.SetActive(false);
            try
            {
                Type poolType = FindType(
                    "TopDownRoguelike.Gameplay.Experience.ExperienceOrbPool");
                Type orbType = FindType(
                    "TopDownRoguelike.Gameplay.Experience.ExperienceOrb");
                Component prefab = prefabObject.AddComponent(orbType);
                Component pool = root.AddComponent(poolType);
                poolType
                    .GetField(
                        "experienceOrbPrefab",
                    BindingFlags.Instance | BindingFlags.NonPublic)
                    .SetValue(pool, prefab);
                poolType
                    .GetField(
                        "initialSize",
                        BindingFlags.Instance | BindingFlags.NonPublic)
                    .SetValue(pool, 0);
                root.SetActive(true);
                prefabObject.SetActive(true);

                Component orb = (Component)poolType.GetMethod("GetOrb")
                    .Invoke(pool, new object[] { Vector3.one, 2 });
                Assert.That(orb, Is.Not.Null);
                Assert.That(
                    orb.GetComponent<
                        TopDownRoguelike.Networking.Gameplay.NetworkEntityId>(),
                    Is.Not.Null);
                Assert.That(
                    orb.GetComponent<
                        TopDownRoguelike.Networking.Gameplay.NetworkEntityId>()
                        .EntityId,
                    Is.GreaterThan(0u));
            }
            finally
            {
                GameSession.Reset();
                UnityEngine.Object.DestroyImmediate(root);
                UnityEngine.Object.DestroyImmediate(prefabObject);
            }
        }

        [Test]
        public void ClientOrb_CannotBeCollected()
        {
            GameSession.ConfigureMultiplayerClient();
            var owner = new GameObject("Client Orb Collection Test");
            try
            {
                Type orbType = FindType(
                    "TopDownRoguelike.Gameplay.Experience.ExperienceOrb");
                Component orb = owner.AddComponent(orbType);
                orbType.GetMethod("Initialize").Invoke(
                    orb,
                    new object[] { 3 });

                Assert.That(
                    orbType.GetMethod("TryCollect").Invoke(orb, null),
                    Is.False);
                Assert.That(
                    orbType.GetProperty("IsCollected").GetValue(orb),
                    Is.False);
            }
            finally
            {
                GameSession.Reset();
                UnityEngine.Object.DestroyImmediate(owner);
            }
        }

        [Test]
        public void ClientConsumer_AcceptsExperienceOrbSpawnRecord()
        {
            Type consumerType = FindType(
                "TopDownRoguelike.Gameplay.Networking.ClientWorldSnapshotConsumer");
            var owner = new GameObject("Client Orb Spawn Consumer Test");
            try
            {
                Component consumer = owner.AddComponent(consumerType);
                consumerType.GetMethod("ConfigureAuthoritativeHost")
                    .Invoke(consumer, new object[] { 1u });
                consumerType.GetMethod("ConfigureEntityRegistry")
                    .Invoke(consumer, new object[] {
                        new TopDownRoguelike.Networking.Gameplay.NetworkEntityRegistry()
                    });

                var record = new WorldEntityRecord(
                    0x40000010u,
                    NetworkEntityType.ExperienceOrb,
                    WorldEntityLifecycle.Spawn,
                    WorldEntityFlags.Active,
                    2f,
                    3f,
                    0f,
                    0,
                    0,
                    experienceAmount: 5);

                Assert.That(
                    consumerType.GetMethod("EnqueueSpawn")
                        .Invoke(consumer, new object[] { record }),
                    Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(owner);
            }
        }

        [Test]
        public void LevelSystem_AppliesAuthoritativeExperienceState()
        {
            Type levelType = FindType(
                "TopDownRoguelike.Gameplay.Experience.LevelSystem");
            var owner = new GameObject("Level System Authority Test");
            try
            {
                Component level = owner.AddComponent(levelType);
                var method = levelType.GetMethod(
                    "ApplyAuthoritativeState");

                Assert.That(method, Is.Not.Null);
                method.Invoke(level, new object[] { 3, 4, 20 });

                Assert.That(
                    levelType.GetProperty("CurrentLevel").GetValue(level),
                    Is.EqualTo(3));
                Assert.That(
                    levelType.GetProperty("CurrentExperience").GetValue(level),
                    Is.EqualTo(4));
                Assert.That(
                    levelType.GetProperty("ExperienceToNextLevel").GetValue(level),
                    Is.EqualTo(20));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(owner);
            }
        }

        [Test]
        public void SharedExperience_HostAccumulatesAcrossLevels()
        {
            GameSession.ConfigureMultiplayerHost();
            Type stateType = FindType(
                "TopDownRoguelike.Gameplay.Experience.SharedExperienceState");
            var owner = new GameObject("Shared Experience Test");
            try
            {
                Component state = owner.AddComponent(stateType);
                Assert.That(
                    stateType.GetMethod("AddExperience")
                        .Invoke(state, new object[] { 25 }),
                    Is.True);
                Assert.That(
                    stateType.GetProperty("CurrentLevel").GetValue(state),
                    Is.EqualTo(3));
                Assert.That(
                    stateType.GetProperty("CurrentExperience").GetValue(state),
                    Is.EqualTo(2));
            }
            finally
            {
                GameSession.Reset();
                UnityEngine.Object.DestroyImmediate(owner);
            }
        }

        private static Type FindType(string fullName)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type type = assembly.GetType(fullName);
                if (type != null)
                {
                    return type;
                }
            }

            Assert.Fail($"Type not found: {fullName}");
            return null;
        }
    }
}
