using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TopDownRoguelike.Networking.Gameplay;
using UnityEngine;

namespace TopDownRoguelike.Tests.EditMode
{
    public sealed class NetworkEntityRegistryTests
    {
        private NetworkEntityRegistry registry;
        private List<GameObject> createdObjects;

        [SetUp]
        public void SetUp()
        {
            registry =
                new NetworkEntityRegistry();

            createdObjects =
                new List<GameObject>();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject createdObject in createdObjects)
            {
                UnityEngine.Object.DestroyImmediate(
                    createdObject);
            }
        }

        [Test]
        public void NewRegistry_IsEmpty()
        {
            Assert.That(
                registry.Count,
                Is.EqualTo(0));
        }

        [Test]
        public void TryRegister_StoresAssignedEntityById()
        {
            NetworkEntityId entity =
                CreateEntity(
                    "Enemy Entity",
                    100u);

            bool registered =
                registry.TryRegister(entity);

            bool found =
                registry.TryGet(
                    100u,
                    out NetworkEntityId registeredEntity);

            Assert.That(
                registered,
                Is.True);

            Assert.That(
                found,
                Is.True);

            Assert.That(
                registeredEntity,
                Is.SameAs(entity));

            Assert.That(
                registry.Count,
                Is.EqualTo(1));
        }

        [Test]
        public void TryRegister_RejectsUnassignedNullAndDuplicateEntities()
        {
            GameObject unassignedObject =
                new GameObject(
                    "Unassigned Entity");

            createdObjects.Add(
                unassignedObject);

            NetworkEntityId unassignedEntity =
                unassignedObject.AddComponent<
                    NetworkEntityId>();

            NetworkEntityId firstEntity =
                CreateEntity(
                    "First Entity",
                    101u);

            NetworkEntityId duplicateEntity =
                CreateEntity(
                    "Duplicate Entity",
                    101u);

            Assert.That(
                registry.TryRegister(
                    null),
                Is.False);

            Assert.That(
                registry.TryRegister(
                    unassignedEntity),
                Is.False);

            Assert.That(
                registry.TryRegister(
                    firstEntity),
                Is.True);

            Assert.That(
                registry.TryRegister(
                    duplicateEntity),
                Is.False);

            Assert.That(
                registry.Count,
                Is.EqualTo(1));
        }

        [Test]
        public void RemoveAndClear_RemoveEntities()
        {
            NetworkEntityId firstEntity =
                CreateEntity(
                    "First Entity",
                    201u);

            NetworkEntityId secondEntity =
                CreateEntity(
                    "Second Entity",
                    202u);

            registry.TryRegister(
                firstEntity);

            registry.TryRegister(
                secondEntity);

            Assert.That(
                registry.Remove(201u),
                Is.True);

            Assert.That(
                registry.TryGet(
                    201u,
                    out _),
                Is.False);

            registry.Clear();

            Assert.That(
                registry.Count,
                Is.EqualTo(0));

            Assert.That(
                registry.TryGet(
                    202u,
                    out _),
                Is.False);
        }

        [Test]
        public void EnumerateEntities_ReturnsRegisteredEntities()
        {
            NetworkEntityId firstEntity =
                CreateEntity(
                    "First Registered Entity",
                    301u);

            NetworkEntityId secondEntity =
                CreateEntity(
                    "Second Registered Entity",
                    302u);

            registry.TryRegister(
                firstEntity);

            registry.TryRegister(
                secondEntity);

            MethodInfo enumerateMethod =
                typeof(NetworkEntityRegistry).GetMethod(
                    "EnumerateEntities",
                    BindingFlags.Instance |
                    BindingFlags.Public);

            Assert.That(
                enumerateMethod,
                Is.Not.Null,
                "NetworkEntityRegistry.EnumerateEntities must exist.");

            object result =
                enumerateMethod.Invoke(
                    registry,
                    null);

            var entries =
                new List<NetworkEntityId>(
                    (IEnumerable<NetworkEntityId>)result);

            Assert.That(
                entries,
                Has.Count.EqualTo(2));

            Assert.That(
                entries.Contains(firstEntity),
                Is.True);

            Assert.That(
                entries.Contains(secondEntity),
                Is.True);
        }

        private NetworkEntityId CreateEntity(
            string objectName,
            uint entityId)
        {
            GameObject entityObject =
                new GameObject(
                    objectName);

            createdObjects.Add(
                entityObject);

            NetworkEntityId entity =
                entityObject.AddComponent<
                    NetworkEntityId>();

            Assert.That(
                entity.TryAssign(
                    entityId),
                Is.True);

            return entity;
        }
    }
}