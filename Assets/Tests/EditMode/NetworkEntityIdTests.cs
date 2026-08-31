using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace TopDownRoguelike.Tests.EditMode
{
    public sealed class NetworkEntityIdTests
    {
        private const string TypeName =
            "TopDownRoguelike.Networking.Gameplay.NetworkEntityId";

        [Test]
        public void NewIdentifier_StartsUnassigned()
        {
            Component identifier =
                CreateIdentifier(
                    out GameObject owner,
                    out Type identifierType);

            try
            {
                PropertyInfo entityIdProperty =
                    identifierType.GetProperty("EntityId");

                PropertyInfo assignedProperty =
                    identifierType.GetProperty("IsAssigned");

                Assert.That(entityIdProperty, Is.Not.Null);
                Assert.That(assignedProperty, Is.Not.Null);

                Assert.That(
                    entityIdProperty.GetValue(identifier),
                    Is.EqualTo(0u));

                Assert.That(
                    assignedProperty.GetValue(identifier),
                    Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(owner);
            }
        }

        [Test]
        public void AssignmentLifecycle_PreservesStableIdUntilClear()
        {
            Component identifier =
                CreateIdentifier(
                    out GameObject owner,
                    out Type identifierType);

            try
            {
                MethodInfo tryAssignMethod =
                    identifierType.GetMethod(
                        "TryAssign",
                        new Type[]
                        {
                            typeof(uint)
                        });

                MethodInfo clearMethod =
                    identifierType.GetMethod("Clear");

                PropertyInfo entityIdProperty =
                    identifierType.GetProperty("EntityId");

                PropertyInfo assignedProperty =
                    identifierType.GetProperty("IsAssigned");

                Assert.That(tryAssignMethod, Is.Not.Null);
                Assert.That(clearMethod, Is.Not.Null);

                Assert.That(
                    InvokeTryAssign(
                        tryAssignMethod,
                        identifier,
                        0u),
                    Is.False);

                Assert.That(
                    InvokeTryAssign(
                        tryAssignMethod,
                        identifier,
                        42u),
                    Is.True);

                Assert.That(
                    entityIdProperty.GetValue(identifier),
                    Is.EqualTo(42u));

                Assert.That(
                    InvokeTryAssign(
                        tryAssignMethod,
                        identifier,
                        99u),
                    Is.False);

                Assert.That(
                    entityIdProperty.GetValue(identifier),
                    Is.EqualTo(42u));

                clearMethod.Invoke(identifier, null);

                Assert.That(
                    assignedProperty.GetValue(identifier),
                    Is.False);

                Assert.That(
                    entityIdProperty.GetValue(identifier),
                    Is.EqualTo(0u));

                Assert.That(
                    InvokeTryAssign(
                        tryAssignMethod,
                        identifier,
                        99u),
                    Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(owner);
            }
        }

        [Test]
        public void TypedAssignment_StoresEntityTypeAndClearsIt()
        {
            Component identifier =
                CreateIdentifier(
                    out GameObject owner,
                    out Type identifierType);

            try
            {
                Type entityType =
                    FindType(
                        "TopDownRoguelike.Networking.Protocol." +
                        "NetworkEntityType");

                Assert.That(
                    entityType,
                    Is.Not.Null,
                    "NetworkEntityType must exist.");

                Assert.That(
                    entityType.IsEnum,
                    Is.True,
                    "NetworkEntityType must be an enum.");

                Assert.That(
                    Convert.ToInt32(
                        Enum.Parse(
                            entityType,
                            "Invalid")),
                    Is.EqualTo(0));

                Assert.That(
                    Convert.ToInt32(
                        Enum.Parse(
                            entityType,
                            "Player")),
                    Is.EqualTo(1));

                Assert.That(
                    Convert.ToInt32(
                        Enum.Parse(
                            entityType,
                            "Enemy")),
                    Is.EqualTo(2));

                Assert.That(
                    Convert.ToInt32(
                        Enum.Parse(
                            entityType,
                            "Boss")),
                    Is.EqualTo(3));

                Assert.That(
                    Convert.ToInt32(
                        Enum.Parse(
                            entityType,
                            "ExperienceOrb")),
                    Is.EqualTo(4));

                MethodInfo typedAssignMethod =
                    identifierType.GetMethod(
                        "TryAssign",
                        new Type[]
                        {
                            typeof(uint),
                            entityType
                        });

                Assert.That(
                    typedAssignMethod,
                    Is.Not.Null,
                    "Typed TryAssign must exist.");

                PropertyInfo entityTypeProperty =
                    identifierType.GetProperty(
                        "EntityType");

                Assert.That(
                    entityTypeProperty,
                    Is.Not.Null,
                    "EntityType property must exist.");

                Assert.That(
                    entityTypeProperty.CanWrite,
                    Is.False,
                    "EntityType must be read-only.");

                object enemyType =
                    Enum.Parse(
                        entityType,
                        "Enemy");

                bool assigned =
                    (bool)typedAssignMethod.Invoke(
                        identifier,
                        new object[]
                        {
                            42u,
                            enemyType
                        });

                Assert.That(
                    assigned,
                    Is.True);

                Assert.That(
                    entityTypeProperty.GetValue(
                        identifier),
                    Is.EqualTo(enemyType));

                MethodInfo clearMethod =
                    identifierType.GetMethod(
                        "Clear");

                Assert.That(
                    clearMethod,
                    Is.Not.Null);

                clearMethod.Invoke(
                    identifier,
                    null);

                object invalidType =
                    Enum.Parse(
                        entityType,
                        "Invalid");

                Assert.That(
                    entityTypeProperty.GetValue(
                        identifier),
                    Is.EqualTo(invalidType));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(
                    owner);
            }
        }

        private static bool InvokeTryAssign(
            MethodInfo method,
            Component identifier,
            uint entityId)
        {
            return (bool)method.Invoke(
                identifier,
                new object[] { entityId });
        }

        private static Component CreateIdentifier(
            out GameObject owner,
            out Type identifierType)
        {
            identifierType = FindType(TypeName);

            Assert.That(
                identifierType,
                Is.Not.Null,
                "NetworkEntityId type must exist.");

            owner = new GameObject(
                "Network Entity Id Test");

            return owner.AddComponent(identifierType);
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