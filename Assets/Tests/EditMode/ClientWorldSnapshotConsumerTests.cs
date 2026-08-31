using System;
using System.Reflection;
using System.Threading;
using NUnit.Framework;
using TopDownRoguelike.Infrastructure;
using TopDownRoguelike.Networking.Gameplay;
using TopDownRoguelike.Networking.Protocol;
using UnityEngine;

namespace TopDownRoguelike.Tests.EditMode
{
    public sealed class ClientWorldSnapshotConsumerTests
    {
        private const string ConsumerTypeName =
            "TopDownRoguelike.Gameplay.Networking." +
            "ClientWorldSnapshotConsumer";

        [TearDown]
        public void TearDown()
        {
            GameSession.Reset();
        }

        [Test]
        public void ClientWorldSnapshotConsumer_IsMonoBehaviourComponent()
        {
            Type consumerType =
                FindType(ConsumerTypeName);

            Assert.That(
                consumerType,
                Is.Not.Null,
                "ClientWorldSnapshotConsumer must exist.");

            Assert.That(
                typeof(MonoBehaviour).IsAssignableFrom(
                    consumerType),
                Is.True,
                "ClientWorldSnapshotConsumer must inherit MonoBehaviour.");
        }

        [Test]
        public void ValidateSnapshotSequence_AcceptsNewerAndWrappedValues()
        {
            var consumerObject =
                new GameObject(
                    "Client World Snapshot Consumer Test");

            try
            {
                Type consumerType =
                    FindType(ConsumerTypeName);

                Assert.That(
                    consumerType,
                    Is.Not.Null);

                Component consumer =
                    consumerObject.AddComponent(
                        consumerType);

                MethodInfo validateMethod =
                    consumerType.GetMethod(
                        "ValidateSnapshotSequence",
                        BindingFlags.Instance |
                        BindingFlags.Public,
                        null,
                        new[]
                        {
                            typeof(uint)
                        },
                        null);

                Assert.That(
                    validateMethod,
                    Is.Not.Null,
                    "ValidateSnapshotSequence must exist.");

                Assert.That(
                    (bool)validateMethod.Invoke(
                        consumer,
                        new object[]
                        {
                            0xFFFFFFFEu
                        }),
                    Is.True);

                Assert.That(
                    (bool)validateMethod.Invoke(
                        consumer,
                        new object[]
                        {
                            0xFFFFFFFFu
                        }),
                    Is.True);

                Assert.That(
                    (bool)validateMethod.Invoke(
                        consumer,
                        new object[]
                        {
                            0u
                        }),
                    Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(
                    consumerObject);
            }
        }

        [Test]
        public void TryConsumeSnapshot_DiscardsDuplicateAndExpiredSnapshots()
        {
            var consumerObject =
                new GameObject(
                    "Client World Snapshot Consume Test");

            try
            {
                Type consumerType =
                    FindType(ConsumerTypeName);

                Assert.That(
                    consumerType,
                    Is.Not.Null);

                Component consumer =
                    consumerObject.AddComponent(
                        consumerType);

                ConfigureAuthoritativeHost(
                    consumerType,
                    consumer,
                    7u);

                MethodInfo consumeMethod =
                    consumerType.GetMethod(
                        "TryConsumeSnapshot",
                        BindingFlags.Instance |
                        BindingFlags.Public,
                        null,
                        new[]
                        {
                            typeof(uint),
                            typeof(uint),
                            typeof(WorldStateSnapshotPayload)
                        },
                        null);

                Assert.That(
                    consumeMethod,
                    Is.Not.Null,
                    "TryConsumeSnapshot must exist.");

                var snapshot =
                    new WorldStateSnapshotPayload(
                        new[]
                        {
                    new WorldEntityRecord(
                        11u,
                        NetworkEntityType.Player,
                        WorldEntityLifecycle.Snapshot,
                        WorldEntityFlags.Active,
                        1f,
                        2f,
                        0f,
                        5,
                        5)
                        });

                Assert.That(
                    InvokeConsume(
                        consumeMethod,
                        consumer,
                        7u,
                        10u,
                        snapshot),
                    Is.True);

                Assert.That(
                    InvokeConsume(
                        consumeMethod,
                        consumer,
                        7u,
                        10u,
                        snapshot),
                    Is.False,
                    "A duplicate snapshot must be discarded.");

                Assert.That(
                    InvokeConsume(
                        consumeMethod,
                        consumer,
                        7u,
                        9u,
                        snapshot),
                    Is.False,
                    "An expired snapshot must be discarded.");

                Assert.That(
                    InvokeConsume(
                        consumeMethod,
                        consumer,
                        7u,
                        11u,
                        snapshot),
                    Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(
                    consumerObject);
            }
        }

        [Test]
        public void TryFindEntityObject_ReturnsRegisteredObjectById()
        {
            var consumerObject =
                new GameObject(
                    "Client World Entity Lookup Test");

            var entityObject =
                new GameObject(
                    "Registered Entity");

            try
            {
                Type consumerType =
                    FindType(ConsumerTypeName);

                Assert.That(
                    consumerType,
                    Is.Not.Null);

                Component consumer =
                    consumerObject.AddComponent(
                        consumerType);

                var registry =
                    new NetworkEntityRegistry();

                NetworkEntityId entityId =
                    entityObject.AddComponent<NetworkEntityId>();

                Assert.That(
                    entityId.TryAssign(
                        42u,
                        NetworkEntityType.Enemy),
                    Is.True);

                Assert.That(
                    registry.TryRegister(entityId),
                    Is.True);

                MethodInfo configureMethod =
                    consumerType.GetMethod(
                        "ConfigureEntityRegistry",
                        BindingFlags.Instance |
                        BindingFlags.Public,
                        null,
                        new[]
                        {
                            typeof(NetworkEntityRegistry)
                        },
                        null);

                Assert.That(
                    configureMethod,
                    Is.Not.Null,
                    "ConfigureEntityRegistry must exist.");

                configureMethod.Invoke(
                    consumer,
                    new object[]
                    {
                        registry
                    });

                MethodInfo findMethod =
                    consumerType.GetMethod(
                        "TryFindEntityObject",
                        BindingFlags.Instance |
                        BindingFlags.Public,
                        null,
                        new[]
                        {
                            typeof(uint),
                            typeof(GameObject).MakeByRefType()
                        },
                        null);

                Assert.That(
                    findMethod,
                    Is.Not.Null,
                    "TryFindEntityObject must exist.");

                object[] foundArguments =
                {
                    42u,
                    null
                };

                Assert.That(
                    (bool)findMethod.Invoke(
                        consumer,
                        foundArguments),
                    Is.True);

                Assert.That(
                    foundArguments[1],
                    Is.SameAs(entityObject));

                object[] missingArguments =
                {
                    99u,
                    null
                };

                Assert.That(
                    (bool)findMethod.Invoke(
                        consumer,
                        missingArguments),
                    Is.False);

                Assert.That(
                    missingArguments[1],
                    Is.Null);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(
                    consumerObject);

                UnityEngine.Object.DestroyImmediate(
                    entityObject);
            }
        }

        [Test]
        public void TryCreateMissingEntity_CreatesAssignsAndRegistersUnknownEntity()
        {
            var consumerObject =
                new GameObject(
                    "Client World Entity Creation Test");

            var registry =
                new NetworkEntityRegistry();

            GameObject createdObject =
                null;

            int factoryCalls =
                0;

            NetworkEnemyArchetype factoryArchetype =
                NetworkEnemyArchetype.Invalid;

            try
            {
                Type consumerType =
                    FindType(ConsumerTypeName);

                Assert.That(
                    consumerType,
                    Is.Not.Null);

                Component consumer =
                    consumerObject.AddComponent(
                        consumerType);

                ConfigureAuthoritativeHost(
                    consumerType,
                    consumer,
                    1u);

                MethodInfo configureRegistryMethod =
                    consumerType.GetMethod(
                        "ConfigureEntityRegistry",
                        BindingFlags.Instance |
                        BindingFlags.Public,
                        null,
                        new[]
                        {
                            typeof(NetworkEntityRegistry)
                        },
                        null);

                Assert.That(
                    configureRegistryMethod,
                    Is.Not.Null);

                configureRegistryMethod.Invoke(
                    consumer,
                    new object[]
                    {
                        registry
                    });

                MethodInfo configureFactoryMethod =
                    consumerType.GetMethod(
                        "ConfigureEntityFactory",
                        BindingFlags.Instance |
                        BindingFlags.Public,
                        null,
                        new[]
                        {
                            typeof(Func<WorldEntityRecord, GameObject>)
                        },
                        null);

                Assert.That(
                    configureFactoryMethod,
                    Is.Not.Null,
                    "ConfigureEntityFactory must exist.");

                Func<WorldEntityRecord, GameObject> factory =
                    record =>
                    {
                        factoryCalls++;
                        factoryArchetype =
                            record.EnemyArchetype;
                        createdObject =
                            new GameObject(
                                "Created World Entity");
                        return createdObject;
                    };

                configureFactoryMethod.Invoke(
                    consumer,
                    new object[]
                    {
                        factory
                    });

                MethodInfo consumeMethod =
                    consumerType.GetMethod(
                        "TryConsumeSnapshot",
                        BindingFlags.Instance |
                        BindingFlags.Public,
                        null,
                        new[]
                        {
                            typeof(uint),
                            typeof(uint),
                            typeof(WorldStateSnapshotPayload)
                        },
                        null);

                Assert.That(
                    consumeMethod,
                    Is.Not.Null,
                    "TryConsumeSnapshot must exist.");

                var record =
                    new WorldEntityRecord(
                        77u,
                        NetworkEntityType.Enemy,
                        WorldEntityLifecycle.Spawn,
                        WorldEntityFlags.Active,
                        4f,
                        5f,
                        90f,
                        3,
                        3,
                        0,
                        NetworkEnemyArchetype.Basic);

                var snapshot =
                    new WorldStateSnapshotPayload(
                        new[]
                        {
                            record
                        });

                Assert.That(
                    InvokeConsume(
                        consumeMethod,
                        consumer,
                        1u,
                        1u,
                        snapshot),
                    Is.True);

                Assert.That(
                    factoryCalls,
                    Is.EqualTo(1));

                Assert.That(
                    factoryArchetype,
                    Is.EqualTo(
                        NetworkEnemyArchetype.Basic));

                Assert.That(
                    createdObject,
                    Is.Not.Null);

                NetworkEntityId identifier =
                    createdObject.GetComponent<NetworkEntityId>();

                Assert.That(
                    identifier,
                    Is.Not.Null);

                Assert.That(
                    identifier.EntityId,
                    Is.EqualTo(77u));

                Assert.That(
                    identifier.EntityType,
                    Is.EqualTo(NetworkEntityType.Enemy));

                Assert.That(
                    registry.TryGet(
                        77u,
                        out NetworkEntityId registered),
                    Is.True);

                Assert.That(
                    registered,
                    Is.SameAs(identifier));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(
                    createdObject);

                UnityEngine.Object.DestroyImmediate(
                    consumerObject);
            }
        }

        [Test]
        public void TryConsumeSnapshot_UpdatesRegisteredEntityTransformAndActivity()
        {
            var consumerObject =
                new GameObject(
                    "Client World Entity Update Test");

            var entityObject =
                new GameObject(
                    "Existing World Entity");

            try
            {
                Type consumerType =
                    FindType(ConsumerTypeName);

                Assert.That(
                    consumerType,
                    Is.Not.Null);

                Component consumer =
                    consumerObject.AddComponent(
                        consumerType);

                ConfigureAuthoritativeHost(
                    consumerType,
                    consumer,
                    1u);

                var registry =
                    new NetworkEntityRegistry();

                NetworkEntityId identifier =
                    entityObject.AddComponent<NetworkEntityId>();

                Assert.That(
                    identifier.TryAssign(
                        88u,
                        NetworkEntityType.Enemy),
                    Is.True);

                Assert.That(
                    registry.TryRegister(identifier),
                    Is.True);

                MethodInfo configureRegistryMethod =
                    consumerType.GetMethod(
                        "ConfigureEntityRegistry",
                        BindingFlags.Instance |
                        BindingFlags.Public,
                        null,
                        new[]
                        {
                            typeof(NetworkEntityRegistry)
                        },
                        null);

                Assert.That(
                    configureRegistryMethod,
                    Is.Not.Null);

                configureRegistryMethod.Invoke(
                    consumer,
                    new object[]
                    {
                        registry
                    });

                entityObject.transform.position =
                    new Vector3(
                        -1f,
                        -2f,
                        6f);

                entityObject.transform.rotation =
                    Quaternion.identity;

                entityObject.SetActive(false);

                var snapshot =
                    new WorldStateSnapshotPayload(
                        new[]
                        {
                            new WorldEntityRecord(
                                88u,
                                NetworkEntityType.Enemy,
                                WorldEntityLifecycle.Update,
                                WorldEntityFlags.Active,
                                4f,
                                5f,
                                135f,
                                3,
                                3,
                                0,
                                NetworkEnemyArchetype.Basic)
                        });

                MethodInfo consumeMethod =
                    consumerType.GetMethod(
                        "TryConsumeSnapshot",
                        BindingFlags.Instance |
                        BindingFlags.Public,
                        null,
                        new[]
                        {
                            typeof(uint),
                            typeof(uint),
                            typeof(WorldStateSnapshotPayload)
                        },
                        null);

                Assert.That(
                    consumeMethod,
                    Is.Not.Null);

                Assert.That(
                    InvokeConsume(
                        consumeMethod,
                        consumer,
                        1u,
                        1u,
                        snapshot),
                    Is.True);

                Assert.That(
                    entityObject.transform.position,
                    Is.EqualTo(
                        new Vector3(
                            4f,
                            5f,
                            6f)));

                Assert.That(
                    Mathf.DeltaAngle(
                        entityObject.transform.eulerAngles.z,
                        135f),
                    Is.EqualTo(0f).Within(0.001f));

                Assert.That(
                    entityObject.activeSelf,
                    Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(
                    entityObject);

                UnityEngine.Object.DestroyImmediate(
                    consumerObject);
            }
        }

        [Test]
        public void TryConsumeSnapshot_AppliesEnemyHealthFromHostRecord()
        {
            GameSession.ConfigureMultiplayerClient();

            var consumerObject =
                new GameObject(
                    "Client Enemy Health Consumer Test");

            var entityObject =
                new GameObject(
                    "Client Enemy Health Entity");

            try
            {
                Type consumerType =
                    FindType(ConsumerTypeName);

                Type enemyHealthType =
                    FindType(
                        "TopDownRoguelike.Gameplay.Enemies." +
                        "EnemyHealth");

                Assert.That(consumerType, Is.Not.Null);
                Assert.That(enemyHealthType, Is.Not.Null);

                Component consumer =
                    consumerObject.AddComponent(consumerType);

                ConfigureAuthoritativeHost(
                    consumerType,
                    consumer,
                    1u);

                var registry =
                    new NetworkEntityRegistry();

                NetworkEntityId identifier =
                    entityObject.AddComponent<NetworkEntityId>();

                Component enemyHealth =
                    entityObject.AddComponent(enemyHealthType);

                Assert.That(
                    identifier.TryAssign(
                        89u,
                        NetworkEntityType.Enemy),
                    Is.True);

                Assert.That(
                    registry.TryRegister(identifier),
                    Is.True);

                consumerType.GetMethod(
                        "ConfigureEntityRegistry",
                        BindingFlags.Instance |
                        BindingFlags.Public)
                    .Invoke(
                        consumer,
                        new object[]
                        {
                            registry
                        });

                var snapshot =
                    new WorldStateSnapshotPayload(
                        new[]
                        {
                            new WorldEntityRecord(
                                89u,
                                NetworkEntityType.Enemy,
                                WorldEntityLifecycle.Update,
                                WorldEntityFlags.Active,
                                2f,
                                3f,
                                90f,
                                1,
                                7,
                                0,
                                NetworkEnemyArchetype.Basic)
                        });

                MethodInfo consumeMethod =
                    consumerType.GetMethod(
                        "TryConsumeSnapshot",
                        BindingFlags.Instance |
                        BindingFlags.Public,
                        null,
                        new[]
                        {
                            typeof(uint),
                            typeof(uint),
                            typeof(WorldStateSnapshotPayload)
                        },
                        null);

                Assert.That(consumeMethod, Is.Not.Null);

                Assert.That(
                    InvokeConsume(
                        consumeMethod,
                        consumer,
                        1u,
                        1u,
                        snapshot),
                    Is.True);

                Assert.That(
                    enemyHealthType.GetProperty("CurrentHealth")
                        .GetValue(enemyHealth, null),
                    Is.EqualTo(1));

                Assert.That(
                    enemyHealthType.GetProperty("MaxHealth")
                        .GetValue(enemyHealth, null),
                    Is.EqualTo(7));

                Assert.That(
                    enemyHealthType.GetProperty("IsDead")
                        .GetValue(enemyHealth, null),
                    Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(entityObject);
                UnityEngine.Object.DestroyImmediate(consumerObject);
            }
        }

        [Test]
        public void TryConsumeSnapshot_DeadEnemyIsHiddenAndUnregistered()
        {
            GameSession.ConfigureMultiplayerClient();

            var consumerObject = new GameObject("Dead Snapshot Consumer");
            var enemyObject = new GameObject("Dead Snapshot Enemy");

            try
            {
                Type consumerType = FindType(ConsumerTypeName);
                Type healthType = FindType(
                    "TopDownRoguelike.Gameplay.Enemies.EnemyHealth");
                Component consumer = consumerObject.AddComponent(consumerType);
                enemyObject.AddComponent(healthType);

                ConfigureAuthoritativeHost(consumerType, consumer, 1u);

                var registry = new NetworkEntityRegistry();
                NetworkEntityId identifier =
                    enemyObject.AddComponent<NetworkEntityId>();

                Assert.That(
                    identifier.TryAssign(90u, NetworkEntityType.Enemy),
                    Is.True);
                Assert.That(registry.TryRegister(identifier), Is.True);

                consumerType.GetMethod("ConfigureEntityRegistry")
                    .Invoke(consumer, new object[] { registry });

                var snapshot = new WorldStateSnapshotPayload(new[]
                {
                    new WorldEntityRecord(
                        90u,
                        NetworkEntityType.Enemy,
                        WorldEntityLifecycle.Dead,
                        WorldEntityFlags.Dead,
                        0f,
                        0f,
                        0f,
                        0,
                        3,
                        0,
                        NetworkEnemyArchetype.Basic)
                });

                MethodInfo consume = consumerType.GetMethod(
                    "TryConsumeSnapshot",
                    new[]
                    {
                        typeof(uint),
                        typeof(uint),
                        typeof(WorldStateSnapshotPayload)
                    });

                Assert.That(
                    InvokeConsume(consume, consumer, 1u, 1u, snapshot),
                    Is.True);
                Assert.That(enemyObject.activeSelf, Is.False);
                Assert.That(registry.TryGet(90u, out _), Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(enemyObject);
                UnityEngine.Object.DestroyImmediate(consumerObject);
            }
        }

        [Test]
        public void TryConsumeSnapshot_RemovesRegisteredEntityExactlyOnce()
        {
            var consumerObject =
                new GameObject(
                    "Client World Entity Removal Test");

            var entityObject =
                new GameObject(
                    "Removed World Entity");

            try
            {
                Type consumerType =
                    FindType(ConsumerTypeName);

                Assert.That(
                    consumerType,
                    Is.Not.Null);

                Component consumer =
                    consumerObject.AddComponent(
                        consumerType);

                ConfigureAuthoritativeHost(
                    consumerType,
                    consumer,
                    1u);

                var registry =
                    new NetworkEntityRegistry();

                NetworkEntityId identifier =
                    entityObject.AddComponent<NetworkEntityId>();

                Assert.That(
                    identifier.TryAssign(
                        99u,
                        NetworkEntityType.Enemy),
                    Is.True);

                Assert.That(
                    registry.TryRegister(identifier),
                    Is.True);

                MethodInfo configureRegistryMethod =
                    consumerType.GetMethod(
                        "ConfigureEntityRegistry",
                        BindingFlags.Instance |
                        BindingFlags.Public,
                        null,
                        new[]
                        {
                            typeof(NetworkEntityRegistry)
                        },
                        null);

                Assert.That(
                    configureRegistryMethod,
                    Is.Not.Null);

                configureRegistryMethod.Invoke(
                    consumer,
                    new object[]
                    {
                        registry
                    });

                int removeCalls =
                    0;

                GameObject removedObject =
                    null;

                Action<GameObject> remover =
                    target =>
                    {
                        removeCalls++;
                        removedObject = target;
                    };

                MethodInfo configureRemoverMethod =
                    consumerType.GetMethod(
                        "ConfigureEntityRemover",
                        BindingFlags.Instance |
                        BindingFlags.Public,
                        null,
                        new[]
                        {
                            typeof(Action<GameObject>)
                        },
                        null);

                Assert.That(
                    configureRemoverMethod,
                    Is.Not.Null,
                    "ConfigureEntityRemover must exist.");

                configureRemoverMethod.Invoke(
                    consumer,
                    new object[]
                    {
                        remover
                    });

                var snapshot =
                    new WorldStateSnapshotPayload(
                        new[]
                        {
                            new WorldEntityRecord(
                                99u,
                                NetworkEntityType.Enemy,
                                WorldEntityLifecycle.Removed,
                                WorldEntityFlags.None,
                                0f,
                                0f,
                                0f,
                                3,
                                3,
                                0,
                                NetworkEnemyArchetype.Basic)
                        });

                MethodInfo consumeMethod =
                    consumerType.GetMethod(
                        "TryConsumeSnapshot",
                        BindingFlags.Instance |
                        BindingFlags.Public,
                        null,
                        new[]
                        {
                            typeof(uint),
                            typeof(uint),
                            typeof(WorldStateSnapshotPayload)
                        },
                        null);

                Assert.That(
                    consumeMethod,
                    Is.Not.Null);

                Assert.That(
                    InvokeConsume(
                        consumeMethod,
                        consumer,
                        1u,
                        1u,
                        snapshot),
                    Is.True);

                Assert.That(
                    registry.TryGet(
                        99u,
                        out _),
                    Is.False);

                Assert.That(
                    identifier.IsAssigned,
                    Is.False);

                Assert.That(
                    removeCalls,
                    Is.EqualTo(1));

                Assert.That(
                    removedObject,
                    Is.SameAs(entityObject));

                Assert.That(
                    InvokeConsume(
                        consumeMethod,
                        consumer,
                        1u,
                        2u,
                        snapshot),
                    Is.True,
                    "Repeated removal records must be harmless.");

                Assert.That(
                    removeCalls,
                    Is.EqualTo(1));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(
                    entityObject);

                UnityEngine.Object.DestroyImmediate(
                    consumerObject);
            }
        }

        [Test]
        public void TryCreateMissingEntity_RejectsCallsOutsideHostSnapshot()
        {
            var consumerObject =
                new GameObject(
                    "Client Authority Creation Guard Test");

            GameObject createdObject =
                null;

            try
            {
                Type consumerType =
                    FindType(ConsumerTypeName);

                Assert.That(
                    consumerType,
                    Is.Not.Null);

                Component consumer =
                    consumerObject.AddComponent(
                        consumerType);

                ConfigureAuthoritativeHost(
                    consumerType,
                    consumer,
                    1u);

                var registry =
                    new NetworkEntityRegistry();

                MethodInfo configureRegistryMethod =
                    consumerType.GetMethod(
                        "ConfigureEntityRegistry",
                        BindingFlags.Instance |
                        BindingFlags.Public,
                        null,
                        new[]
                        {
                            typeof(NetworkEntityRegistry)
                        },
                        null);

                Assert.That(
                    configureRegistryMethod,
                    Is.Not.Null);

                configureRegistryMethod.Invoke(
                    consumer,
                    new object[]
                    {
                        registry
                    });

                int factoryCalls =
                    0;

                Func<WorldEntityRecord, GameObject> factory =
                    record =>
                    {
                        factoryCalls++;
                        createdObject =
                            new GameObject(
                                "Unauthorized World Entity");
                        return createdObject;
                    };

                MethodInfo configureFactoryMethod =
                    consumerType.GetMethod(
                        "ConfigureEntityFactory",
                        BindingFlags.Instance |
                        BindingFlags.Public,
                        null,
                        new[]
                        {
                            typeof(Func<WorldEntityRecord, GameObject>)
                        },
                        null);

                Assert.That(
                    configureFactoryMethod,
                    Is.Not.Null);

                configureFactoryMethod.Invoke(
                    consumer,
                    new object[]
                    {
                        factory
                    });

                MethodInfo createMethod =
                    consumerType.GetMethod(
                        "TryCreateMissingEntity",
                        BindingFlags.Instance |
                        BindingFlags.Public,
                        null,
                        new[]
                        {
                            typeof(WorldEntityRecord),
                            typeof(GameObject).MakeByRefType()
                        },
                        null);

                Assert.That(
                    createMethod,
                    Is.Not.Null);

                var record =
                    new WorldEntityRecord(
                        100u,
                        NetworkEntityType.Enemy,
                        WorldEntityLifecycle.Spawn,
                        WorldEntityFlags.Active,
                        0f,
                        0f,
                        0f,
                        3,
                        3,
                        0,
                        NetworkEnemyArchetype.Basic);

                object[] arguments =
                {
                    record,
                    null
                };

                Assert.That(
                    (bool)createMethod.Invoke(
                        consumer,
                        arguments),
                    Is.False);

                Assert.That(
                    factoryCalls,
                    Is.Zero);

                Assert.That(
                    arguments[1],
                    Is.Null);

                Assert.That(
                    registry.Count,
                    Is.Zero);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(
                    createdObject);

                UnityEngine.Object.DestroyImmediate(
                    consumerObject);
            }
        }

        [Test]
        public void TryConsumeSnapshot_RejectsNonHostSender()
        {
            var consumerObject =
                new GameObject(
                    "Client World Snapshot Authority Test");

            try
            {
                Type consumerType =
                    FindType(ConsumerTypeName);

                Assert.That(
                    consumerType,
                    Is.Not.Null);

                Component consumer =
                    consumerObject.AddComponent(
                        consumerType);

                ConfigureAuthoritativeHost(
                    consumerType,
                    consumer,
                    1u);

                MethodInfo consumeMethod =
                    consumerType.GetMethod(
                        "TryConsumeSnapshot",
                        BindingFlags.Instance |
                        BindingFlags.Public,
                        null,
                        new[]
                        {
                            typeof(uint),
                            typeof(uint),
                            typeof(WorldStateSnapshotPayload)
                        },
                        null);

                Assert.That(
                    consumeMethod,
                    Is.Not.Null);

                var snapshot =
                    new WorldStateSnapshotPayload(
                        new[]
                        {
                            new WorldEntityRecord(
                                101u,
                                NetworkEntityType.Enemy,
                                WorldEntityLifecycle.Spawn,
                                WorldEntityFlags.Active,
                                0f,
                                0f,
                                0f,
                                3,
                                3,
                                0,
                                NetworkEnemyArchetype.Basic)
                        });

                Assert.That(
                    InvokeConsume(
                        consumeMethod,
                        consumer,
                        2u,
                        1u,
                        snapshot),
                    Is.False);

                Assert.That(
                    InvokeConsume(
                        consumeMethod,
                        consumer,
                        1u,
                        1u,
                        snapshot),
                    Is.True,
                    "Rejected senders must not consume the sequence.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(
                    consumerObject);
            }
        }

        [Test]
        public void NetworkSnapshot_IsAppliedOnlyByMainThreadDrain()
        {
            var consumerObject =
                new GameObject(
                    "Client Main Thread Handoff Test");

            var entityObject =
                new GameObject(
                    "Main Thread World Entity");

            try
            {
                Type consumerType =
                    FindType(ConsumerTypeName);

                Assert.That(
                    consumerType,
                    Is.Not.Null);

                Component consumer =
                    consumerObject.AddComponent(
                        consumerType);

                ConfigureAuthoritativeHost(
                    consumerType,
                    consumer,
                    1u);

                var registry =
                    new NetworkEntityRegistry();

                NetworkEntityId identifier =
                    entityObject.AddComponent<NetworkEntityId>();

                Assert.That(
                    identifier.TryAssign(
                        111u,
                        NetworkEntityType.Enemy),
                    Is.True);

                Assert.That(
                    registry.TryRegister(identifier),
                    Is.True);

                MethodInfo configureRegistryMethod =
                    consumerType.GetMethod(
                        "ConfigureEntityRegistry",
                        BindingFlags.Instance |
                        BindingFlags.Public,
                        null,
                        new[]
                        {
                            typeof(NetworkEntityRegistry)
                        },
                        null);

                Assert.That(
                    configureRegistryMethod,
                    Is.Not.Null);

                configureRegistryMethod.Invoke(
                    consumer,
                    new object[]
                    {
                        registry
                    });

                entityObject.transform.position =
                    new Vector3(
                        -4f,
                        -5f,
                        2f);

                var snapshot =
                    new WorldStateSnapshotPayload(
                        new[]
                        {
                            new WorldEntityRecord(
                                111u,
                                NetworkEntityType.Enemy,
                                WorldEntityLifecycle.Update,
                                WorldEntityFlags.Active,
                                4f,
                                5f,
                                45f,
                                3,
                                3,
                                0,
                                NetworkEnemyArchetype.Basic)
                        });

                MethodInfo consumeMethod =
                    consumerType.GetMethod(
                        "TryConsumeSnapshot",
                        BindingFlags.Instance |
                        BindingFlags.Public,
                        null,
                        new[]
                        {
                            typeof(uint),
                            typeof(uint),
                            typeof(WorldStateSnapshotPayload)
                        },
                        null);

                MethodInfo enqueueMethod =
                    consumerType.GetMethod(
                        "EnqueueSnapshot",
                        BindingFlags.Instance |
                        BindingFlags.Public,
                        null,
                        new[]
                        {
                            typeof(uint),
                            typeof(uint),
                            typeof(WorldStateSnapshotPayload)
                        },
                        null);

                MethodInfo processMethod =
                    consumerType.GetMethod(
                        "ProcessPendingSnapshots",
                        BindingFlags.Instance |
                        BindingFlags.Public,
                        null,
                        Type.EmptyTypes,
                        null);

                Assert.That(
                    consumeMethod,
                    Is.Not.Null);

                Assert.That(
                    enqueueMethod,
                    Is.Not.Null,
                    "EnqueueSnapshot must exist.");

                Assert.That(
                    processMethod,
                    Is.Not.Null,
                    "ProcessPendingSnapshots must exist.");

                bool directConsumeResult =
                    true;

                bool enqueueResult =
                    false;

                Exception backgroundException =
                    null;

                var backgroundThread =
                    new Thread(
                        () =>
                        {
                            try
                            {
                                directConsumeResult =
                                    InvokeConsume(
                                        consumeMethod,
                                        consumer,
                                        1u,
                                        1u,
                                        snapshot);

                                enqueueResult =
                                    (bool)enqueueMethod.Invoke(
                                        consumer,
                                        new object[]
                                        {
                                            1u,
                                            1u,
                                            snapshot
                                        });
                            }
                            catch (Exception exception)
                            {
                                backgroundException =
                                    exception;
                            }
                        });

                backgroundThread.Start();
                backgroundThread.Join();

                Assert.That(
                    backgroundException,
                    Is.Null);

                Assert.That(
                    directConsumeResult,
                    Is.False);

                Assert.That(
                    enqueueResult,
                    Is.True);

                Assert.That(
                    entityObject.transform.position,
                    Is.EqualTo(
                        new Vector3(
                            -4f,
                            -5f,
                            2f)));

                Assert.That(
                    (int)processMethod.Invoke(
                        consumer,
                        null),
                    Is.EqualTo(1));

                Assert.That(
                    entityObject.transform.position,
                    Is.EqualTo(
                        new Vector3(
                            4f,
                            5f,
                            2f)));

                Assert.That(
                    Mathf.DeltaAngle(
                        entityObject.transform.eulerAngles.z,
                        45f),
                    Is.EqualTo(0f).Within(0.001f));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(
                    entityObject);

                UnityEngine.Object.DestroyImmediate(
                    consumerObject);
            }
        }

        [Test]
        public void EnqueueSpawn_BackgroundThreadCreatesEntityOnMainThread()
        {
            var consumerObject =
                new GameObject(
                    "Reliable Enemy Spawn Consumer Test");

            GameObject createdObject =
                null;

            try
            {
                Type consumerType =
                    FindType(ConsumerTypeName);

                Assert.That(consumerType, Is.Not.Null);

                Component consumer =
                    consumerObject.AddComponent(
                        consumerType);

                var registry =
                    new NetworkEntityRegistry();

                consumerType.GetMethod(
                    "ConfigureEntityRegistry")
                    .Invoke(
                        consumer,
                        new object[]
                        {
                            registry
                        });

                Func<WorldEntityRecord, GameObject> factory =
                    _ =>
                    {
                        createdObject =
                            new GameObject(
                                "Reliably Spawned Enemy");

                        return createdObject;
                    };

                consumerType.GetMethod(
                    "ConfigureEntityFactory")
                    .Invoke(
                        consumer,
                        new object[]
                        {
                            factory
                        });

                MethodInfo enqueueMethod =
                    consumerType.GetMethod(
                        "EnqueueSpawn",
                        BindingFlags.Instance |
                        BindingFlags.Public,
                        null,
                        new[]
                        {
                            typeof(WorldEntityRecord)
                        },
                        null);

                Assert.That(
                    enqueueMethod,
                    Is.Not.Null,
                    "EnqueueSpawn must expose a thread-safe " +
                    "TCP event entry point.");

                var record =
                    new WorldEntityRecord(
                        0x10000011u,
                        NetworkEntityType.Enemy,
                        WorldEntityLifecycle.Spawn,
                        WorldEntityFlags.Active,
                        7f,
                        -3f,
                        45f,
                        3,
                        3,
                        0,
                        NetworkEnemyArchetype.Fast);

                bool wasQueued =
                    false;

                var enqueueThread =
                    new Thread(
                        () =>
                        {
                            wasQueued =
                                (bool)enqueueMethod.Invoke(
                                    consumer,
                                    new object[]
                                    {
                                        record
                                    });
                        });

                enqueueThread.Start();
                enqueueThread.Join();

                Assert.That(wasQueued, Is.True);
                Assert.That(createdObject, Is.Null);

                MethodInfo processMethod =
                    consumerType.GetMethod(
                        "ProcessPendingSnapshots",
                        BindingFlags.Instance |
                        BindingFlags.Public);

                Assert.That(processMethod, Is.Not.Null);

                int processedCount =
                    (int)processMethod.Invoke(
                        consumer,
                        null);

                Assert.That(processedCount, Is.EqualTo(1));
                Assert.That(createdObject, Is.Not.Null);
                Assert.That(
                    createdObject.transform.position,
                    Is.EqualTo(new Vector3(7f, -3f, 0f)));
                Assert.That(
                    registry.TryGet(
                        0x10000011u,
                        out _),
                    Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(
                    createdObject);

                UnityEngine.Object.DestroyImmediate(
                    consumerObject);
            }
        }

        [Test]
        public void EnqueueSpawn_RejectsNonSpawnLifecycle()
        {
            var consumerObject =
                new GameObject(
                    "Reliable Spawn Validation Test");

            try
            {
                Type consumerType =
                    FindType(ConsumerTypeName);

                Component consumer =
                    consumerObject.AddComponent(
                        consumerType);

                MethodInfo enqueueMethod =
                    consumerType.GetMethod(
                        "EnqueueSpawn",
                        BindingFlags.Instance |
                        BindingFlags.Public);

                Assert.That(enqueueMethod, Is.Not.Null);

                var updateRecord =
                    new WorldEntityRecord(
                        0x10000012u,
                        NetworkEntityType.Enemy,
                        WorldEntityLifecycle.Update,
                        WorldEntityFlags.Active,
                        0f,
                        0f,
                        0f,
                        3,
                        3,
                        0,
                        NetworkEnemyArchetype.Basic);

                bool wasQueued =
                    (bool)enqueueMethod.Invoke(
                        consumer,
                        new object[]
                        {
                            updateRecord
                        });

                Assert.That(wasQueued, Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(
                    consumerObject);
            }
        }

        [Test]
        public void EnqueueRemoval_HidesAndUnregistersEnemyExactlyOnce()
        {
            GameSession.ConfigureMultiplayerClient();

            var consumerObject = new GameObject("Removal Consumer");
            var enemyObject = new GameObject("Removed Enemy");

            try
            {
                Type consumerType = FindType(ConsumerTypeName);
                Component consumer = consumerObject.AddComponent(consumerType);
                var registry = new NetworkEntityRegistry();
                NetworkEntityId identifier =
                    enemyObject.AddComponent<NetworkEntityId>();

                Assert.That(
                    identifier.TryAssign(0x10000033u, NetworkEntityType.Enemy),
                    Is.True);
                Assert.That(registry.TryRegister(identifier), Is.True);

                consumerType.GetMethod("ConfigureEntityRegistry")
                    .Invoke(consumer, new object[] { registry });

                int removalCount = 0;
                Action<GameObject> remover = entity =>
                {
                    removalCount++;
                    entity.SetActive(false);
                };

                consumerType.GetMethod("ConfigureEntityRemover")
                    .Invoke(consumer, new object[] { remover });

                var removed = new WorldEntityRemovedPayload(
                    0x10000033u,
                    NetworkEntityType.Enemy,
                    WorldEntityRemovalReason.Died);

                MethodInfo enqueue = consumerType.GetMethod(
                    "EnqueueRemoval",
                    new[] { typeof(WorldEntityRemovedPayload) });

                Assert.That(enqueue, Is.Not.Null);
                Assert.That((bool)enqueue.Invoke(consumer, new object[] { removed }), Is.True);
                Assert.That(
                    (int)consumerType.GetMethod("ProcessPendingSnapshots")
                        .Invoke(consumer, null),
                    Is.EqualTo(1));

                Assert.That(enemyObject.activeSelf, Is.False);
                Assert.That(registry.TryGet(0x10000033u, out _), Is.False);

                Assert.That((bool)enqueue.Invoke(consumer, new object[] { removed }), Is.True);
                consumerType.GetMethod("ProcessPendingSnapshots")
                    .Invoke(consumer, null);

                Assert.That(removalCount, Is.EqualTo(1));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(enemyObject);
                UnityEngine.Object.DestroyImmediate(consumerObject);
            }
        }

        private static bool InvokeConsume(
            MethodInfo consumeMethod,
            Component consumer,
            uint senderPlayerId,
            uint sequence,
            WorldStateSnapshotPayload snapshot)
        {
            return (bool)consumeMethod.Invoke(
                consumer,
                new object[]
                {
                    senderPlayerId,
                    sequence,
                    snapshot
                });
        }

        private static void ConfigureAuthoritativeHost(
            Type consumerType,
            Component consumer,
            uint hostPlayerId)
        {
            MethodInfo configureHostMethod =
                consumerType.GetMethod(
                    "ConfigureAuthoritativeHost",
                    BindingFlags.Instance |
                    BindingFlags.Public,
                    null,
                    new[]
                    {
                        typeof(uint)
                    },
                    null);

            Assert.That(
                configureHostMethod,
                Is.Not.Null,
                "ConfigureAuthoritativeHost must exist.");

            configureHostMethod.Invoke(
                consumer,
                new object[]
                {
                    hostPlayerId
                });
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
