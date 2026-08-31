using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TopDownRoguelike.Networking.Gameplay;
using UnityEngine;

namespace TopDownRoguelike.Tests.EditMode
{
    public sealed class NetworkPlayerRegistryTests
    {
        private NetworkPlayerRegistry registry;
        private List<GameObject> createdPlayers;

        [SetUp]
        public void SetUp()
        {
            registry = new NetworkPlayerRegistry();
            createdPlayers = new List<GameObject>();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject player in createdPlayers)
            {
                Object.DestroyImmediate(player);
            }
        }

        [Test]
        public void NewRegistry_IsEmpty()
        {
            Assert.That(registry.Count, Is.EqualTo(0));
        }

        [Test]
        public void TryRegister_StoresPlayerById()
        {
            GameObject player = CreatePlayer("HostPlayer");

            bool registered =
                registry.TryRegister(1u, player);

            bool found =
                registry.TryGetPlayer(
                    1u,
                    out GameObject registeredPlayer);

            Assert.That(registered, Is.True);
            Assert.That(found, Is.True);
            Assert.That(registeredPlayer, Is.SameAs(player));
            Assert.That(registry.Count, Is.EqualTo(1));
        }

        [Test]
        public void EnumeratePlayers_ReturnsAllRegisteredPlayers()
        {
            GameObject host =
                CreatePlayer("HostPlayer");

            GameObject client =
                CreatePlayer("ClientPlayer");

            registry.TryRegister(
                1u,
                host);

            registry.TryRegister(
                2u,
                client);

            MethodInfo enumerateMethod =
                typeof(NetworkPlayerRegistry).GetMethod(
                    "EnumeratePlayers",
                    BindingFlags.Instance |
                    BindingFlags.Public);

            Assert.That(
                enumerateMethod,
                Is.Not.Null,
                "NetworkPlayerRegistry.EnumeratePlayers must exist.");

            object result =
                enumerateMethod.Invoke(
                    registry,
                    null);

            var entries =
                new List<KeyValuePair<uint, GameObject>>(
                    (IEnumerable<KeyValuePair<uint, GameObject>>)result);

            Assert.That(
                entries,
                Has.Count.EqualTo(2));

            Assert.That(
                entries.Exists(
                    entry =>
                        entry.Key == 1u &&
                        entry.Value == host),
                Is.True);

            Assert.That(
                entries.Exists(
                    entry =>
                        entry.Key == 2u &&
                        entry.Value == client),
                Is.True);
        }

        [Test]
        public void TryRegister_RejectsInvalidAndDuplicateEntries()
        {
            GameObject firstPlayer =
                CreatePlayer("FirstPlayer");

            GameObject duplicatePlayer =
                CreatePlayer("DuplicatePlayer");

            Assert.That(
                registry.TryRegister(0u, firstPlayer),
                Is.False);

            Assert.That(
                registry.TryRegister(1u, null),
                Is.False);

            Assert.That(
                registry.TryRegister(1u, firstPlayer),
                Is.True);

            Assert.That(
                registry.TryRegister(1u, duplicatePlayer),
                Is.False);

            Assert.That(registry.Count, Is.EqualTo(1));
        }

        [Test]
        public void RemoveAndClear_RemoveRegisteredPlayers()
        {
            GameObject host =
                CreatePlayer("HostPlayer");

            GameObject client =
                CreatePlayer("ClientPlayer");

            registry.TryRegister(1u, host);
            registry.TryRegister(2u, client);

            Assert.That(registry.Remove(1u), Is.True);
            Assert.That(
                registry.TryGetPlayer(1u, out _),
                Is.False);

            registry.Clear();

            Assert.That(registry.Count, Is.EqualTo(0));
            Assert.That(
                registry.TryGetPlayer(2u, out _),
                Is.False);
        }

        private GameObject CreatePlayer(
            string playerName)
        {
            var player =
                new GameObject(playerName);

            createdPlayers.Add(player);
            return player;
        }
    }
}