using TMPro;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TopDownRoguelike.Networking.Gameplay;
using TopDownRoguelike.Infrastructure;
using TopDownRoguelike.Networking.Protocol;
using TopDownRoguelike.Networking.Client;
using TopDownRoguelike.Networking.Transport;
using UnityEngine;

namespace TopDownRoguelike.Tests.EditMode
{
    public sealed class NetworkGameBootstrapTests
    {
        private const string BootstrapTypeName =
            "TopDownRoguelike.Gameplay.Networking." +
            "NetworkGameBootstrap";

        [Test]
        public void Awake_CreatesEmptyPlayerRegistry()
        {
            Type bootstrapType =
                FindType(BootstrapTypeName);

            Assert.That(
                bootstrapType,
                Is.Not.Null,
                $"{BootstrapTypeName} must exist.");

            Assert.That(
                typeof(MonoBehaviour).IsAssignableFrom(
                    bootstrapType),
                Is.True);

            var bootstrapObject =
                new GameObject("Bootstrap Test");

            bootstrapObject.SetActive(false);

            try
            {
                Component bootstrap =
                    bootstrapObject.AddComponent(
                        bootstrapType);

                MethodInfo awakeMethod =
                    bootstrapType.GetMethod(
                        "Awake",
                        BindingFlags.Instance |
                        BindingFlags.NonPublic);

                Assert.That(awakeMethod, Is.Not.Null);

                awakeMethod.Invoke(bootstrap, null);

                PropertyInfo registryProperty =
                    bootstrapType.GetProperty(
                        "Registry",
                        BindingFlags.Instance |
                        BindingFlags.Public);

                Assert.That(registryProperty, Is.Not.Null);

                var registry =
                    registryProperty.GetValue(bootstrap)
                    as NetworkPlayerRegistry;

                Assert.That(registry, Is.Not.Null);
                Assert.That(registry.Count, Is.EqualTo(0));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(
                    bootstrapObject);
            }
        }

        [Test]
        public void StartInSinglePlayerMode_RegistersOnlyScenePlayer()
        {
            Type bootstrapType =
                FindType(BootstrapTypeName);

            Type cameraFollowType =
                FindType("CameraFollow");

            Type healthBarViewType =
                FindType(
                    "TopDownRoguelike.Gameplay.UI." +
                    "HealthBarView");

            Assert.That(bootstrapType, Is.Not.Null);
            Assert.That(cameraFollowType, Is.Not.Null);
            Assert.That(healthBarViewType,Is.Not.Null);

            var bootstrapObject =
                new GameObject("Bootstrap Test");

            var scenePlayer =
                new GameObject("Scene Player");

            var hostSpawnObject =
                new GameObject("Host Spawn Point");

            var cameraObject =
                new GameObject("Camera Test");

            GameObject healthBarObject = null;

            bootstrapObject.SetActive(false);
            scenePlayer.SetActive(false);
            cameraObject.SetActive(false);

            hostSpawnObject.transform.position =
                new Vector3(3f, -2f, 0f);

            try
            {
                Component healthBarView =
                    CreateHealthBarView(
                        healthBarViewType,
                        out healthBarObject);

                AddInitializedPlayerHealth(
                    scenePlayer);

                cameraObject.AddComponent<Camera>();

                Component cameraFollow =
                    cameraObject.AddComponent(
                        cameraFollowType);

                Component bootstrap =
                    bootstrapObject.AddComponent(
                        bootstrapType);

                SetPrivateField(
                    bootstrap,
                    "healthBarView",
                    healthBarView);

                SetPrivateField(
                    bootstrap,
                    "scenePlayer",
                    scenePlayer);

                SetPrivateField(
                    bootstrap,
                    "hostSpawnPoint",
                    hostSpawnObject.transform);

                SetPrivateField(
                    bootstrap,
                    "cameraFollow",
                    cameraFollow);

                GameSession.ConfigureSinglePlayer();

                InvokePrivate(bootstrap, "Awake");
                InvokePrivate(bootstrap, "Start");

                PropertyInfo registryProperty =
                    bootstrapType.GetProperty("Registry");

                var registry =
                    registryProperty.GetValue(bootstrap)
                        as NetworkPlayerRegistry;

                PropertyInfo localPlayerProperty =
                    bootstrapType.GetProperty("LocalPlayer");

                var localPlayer =
                    localPlayerProperty.GetValue(bootstrap)
                        as GameObject;

                Assert.That(registry, Is.Not.Null);
                Assert.That(registry.Count, Is.EqualTo(1));

                Assert.That(
                    registry.TryGetPlayer(
                        1u,
                        out GameObject registeredPlayer),
                    Is.True);

                Assert.That(
                    registeredPlayer,
                    Is.SameAs(scenePlayer));

                Assert.That(
                    localPlayer,
                    Is.SameAs(scenePlayer));

                Assert.That(scenePlayer.activeSelf, Is.True);

                Assert.That(
                    scenePlayer.transform.position,
                    Is.EqualTo(
                        hostSpawnObject.transform.position));
            }
            finally
            {
                GameSession.Reset();

                UnityEngine.Object.DestroyImmediate(
                    bootstrapObject);

                UnityEngine.Object.DestroyImmediate(
                    scenePlayer);

                UnityEngine.Object.DestroyImmediate(
                    hostSpawnObject);

                UnityEngine.Object.DestroyImmediate(
                    cameraObject);

                if (healthBarObject != null)
                {
                    UnityEngine.Object.DestroyImmediate(
                        healthBarObject);
                }
            }
        }

        [TestCase(GameMode.SinglePlayer)]
        [TestCase(GameMode.MultiplayerClient)]
        public void TryConfigureHostStatePublisher_NonHostModeDoesNotCreatePublisher(
            GameMode mode)
        {
            Type bootstrapType =
                FindType(BootstrapTypeName);

            Type statePublisherType =
                FindType(
                    "TopDownRoguelike.Gameplay.Networking." +
                    "HostPlayerStatePublisher");

            Assert.That(bootstrapType, Is.Not.Null);
            Assert.That(statePublisherType, Is.Not.Null);

            var bootstrapObject =
                new GameObject(
                    "Non-Host State Publisher Test");

            bootstrapObject.SetActive(false);

            try
            {
                if (mode == GameMode.SinglePlayer)
                {
                    GameSession.ConfigureSinglePlayer();
                }
                else
                {
                    GameSession.ConfigureMultiplayerClient();
                }

                Component bootstrap =
                    bootstrapObject.AddComponent(
                        bootstrapType);

                InvokePrivate(
                    bootstrap,
                    "Awake");

                Action<PlayerStateSnapshotPayload> sender =
                    _ => { };

                InvokePrivate(
                    bootstrap,
                    "TryConfigureHostStatePublisher",
                    11u,
                    22u,
                    sender);

                Component publisher =
                    bootstrapObject.GetComponent(
                        statePublisherType);

                Assert.That(
                    publisher,
                    Is.Null,
                    "Non-host modes must not create a " +
                    "HostPlayerStatePublisher.");
            }
            finally
            {
                GameSession.Reset();

                UnityEngine.Object.DestroyImmediate(
                    bootstrapObject);
            }
        }

        [TestCase(GameMode.SinglePlayer)]
        [TestCase(GameMode.MultiplayerClient)]
        public void TryConfigureHostWorldSnapshotPublisher_NonHostModeDoesNotCreatePublisher(
            GameMode mode)
        {
            Type bootstrapType =
                FindType(BootstrapTypeName);

            Type worldPublisherType =
                FindType(
                    "TopDownRoguelike.Gameplay.Networking." +
                    "HostWorldSnapshotPublisher");

            Assert.That(bootstrapType, Is.Not.Null);
            Assert.That(worldPublisherType, Is.Not.Null);

            var bootstrapObject =
                new GameObject(
                    "Non-Host World Snapshot Publisher Test");

            bootstrapObject.SetActive(false);

            try
            {
                if (mode == GameMode.SinglePlayer)
                {
                    GameSession.ConfigureSinglePlayer();
                }
                else
                {
                    GameSession.ConfigureMultiplayerClient();
                }

                Component bootstrap =
                    bootstrapObject.AddComponent(
                        bootstrapType);

                InvokePrivate(
                    bootstrap,
                    "Awake");

                Action<WorldStateSnapshotPayload> sender =
                    _ => { };

                InvokePrivate(
                    bootstrap,
                    "TryConfigureHostWorldSnapshotPublisher",
                    sender);

                Component publisher =
                    bootstrapObject.GetComponent(
                        worldPublisherType);

                Assert.That(
                    publisher,
                    Is.Null,
                    "Non-host modes must not create a " +
                    "HostWorldSnapshotPublisher.");
            }
            finally
            {
                GameSession.Reset();

                UnityEngine.Object.DestroyImmediate(
                    bootstrapObject);
            }
        }

        [Test]
        public void TryConfigureHostEnemySpawnPublisher_HostCreatesPublisher()
        {
            Type bootstrapType =
                FindType(BootstrapTypeName);

            Type spawnerType =
                FindType(
                    "TopDownRoguelike.Gameplay.Enemies." +
                    "EnemySpawner");

            Type publisherType =
                FindType(
                    "TopDownRoguelike.Gameplay.Networking." +
                    "HostEnemySpawnPublisher");

            Assert.That(bootstrapType, Is.Not.Null);
            Assert.That(spawnerType, Is.Not.Null);
            Assert.That(publisherType, Is.Not.Null);

            var bootstrapObject =
                new GameObject(
                    "Host Enemy Spawn Bootstrap Test");

            var spawnerObject =
                new GameObject(
                    "Host Enemy Spawn Spawner Test");

            bootstrapObject.SetActive(false);

            try
            {
                GameSession.ConfigureMultiplayerHost();

                Component bootstrap =
                    bootstrapObject.AddComponent(
                        bootstrapType);

                spawnerObject.AddComponent(
                    spawnerType);

                InvokePrivate(
                    bootstrap,
                    "Awake");

                Action<WorldEntityRecord> sender =
                    _ => { };

                MethodInfo configureMethod =
                    bootstrapType.GetMethod(
                        "TryConfigureHostEnemySpawnPublisher",
                        BindingFlags.Instance |
                        BindingFlags.NonPublic);

                Assert.That(
                    configureMethod,
                    Is.Not.Null,
                    "Host bootstrap must configure the " +
                    "reliable enemy spawn publisher.");

                bool wasConfigured =
                    (bool)configureMethod.Invoke(
                        bootstrap,
                        new object[]
                        {
                            sender
                        });

                Assert.That(wasConfigured, Is.True);

                Component publisher =
                    bootstrapObject.GetComponent(
                        publisherType);

                Assert.That(publisher, Is.Not.Null);
            }
            finally
            {
                GameSession.Reset();

                UnityEngine.Object.DestroyImmediate(
                    spawnerObject);

                UnityEngine.Object.DestroyImmediate(
                    bootstrapObject);
            }
        }

        [Test]
        public void ConfigureHostPlayers_CreatesTwoRegisteredPlayers()
        {
            Type bootstrapType =
                FindType(BootstrapTypeName);

            Type statePublisherType =
                FindType(
                    "TopDownRoguelike.Gameplay.Networking." +
                    "HostPlayerStatePublisher");

            Type localInputSourceType =
                FindType(
                    "TopDownRoguelike.Gameplay.Characters." +
                    "LocalPlayerInputSource");

            Type remoteInputSourceType =
                FindType(
                    "TopDownRoguelike.Gameplay.Networking." +
                    "RemotePlayerInputSource");

            Type playerControllerType =
                FindType("PlayerController");

            Type playerShooterType =
                FindType(
                    "TopDownRoguelike.Gameplay.Weapons." +
                    "PlayerShooter");

            Assert.That(
                localInputSourceType,
                Is.Not.Null);

            Assert.That(
                remoteInputSourceType,
                Is.Not.Null);

            Assert.That(
                playerControllerType,
                Is.Not.Null);

            Assert.That(
                playerShooterType,
                Is.Not.Null);

            Assert.That(bootstrapType, Is.Not.Null);

            Assert.That(
                statePublisherType,
                Is.Not.Null);

            var bootstrapObject =
                new GameObject("Bootstrap Test");

            var scenePlayer =
                new GameObject("Scene Player");

            scenePlayer.AddComponent<Rigidbody2D>();

            scenePlayer.AddComponent(
                localInputSourceType);

            scenePlayer.AddComponent(
                playerControllerType);

            scenePlayer.AddComponent(
                playerShooterType);
    
            var hostSpawnObject =
                new GameObject("Host Spawn Point");

            var clientSpawnObject =
                new GameObject("Client Spawn Point");

            bootstrapObject.SetActive(false);
            scenePlayer.SetActive(false);

            hostSpawnObject.transform.position =
                new Vector3(-1f, 2f, 0f);

            clientSpawnObject.transform.position =
                new Vector3(3f, -2f, 0f);

            GameObject remotePlayer = null;

            try
            {
                Component bootstrap =
                    bootstrapObject.AddComponent(
                        bootstrapType);

                SetPrivateField(
                    bootstrap,
                    "scenePlayer",
                    scenePlayer);

                SetPrivateField(
                    bootstrap,
                    "hostSpawnPoint",
                    hostSpawnObject.transform);

                SetPrivateField(
                    bootstrap,
                    "clientSpawnPoint",
                    clientSpawnObject.transform);

                InvokePrivate(bootstrap, "Awake");

                InvokePrivate(
                    bootstrap,
                    "ConfigureHostPlayers",
                    11u,
                    22u);

                var registry =
                    bootstrapType
                        .GetProperty("Registry")
                        .GetValue(bootstrap)
                        as NetworkPlayerRegistry;

                var localPlayer =
                    bootstrapType
                        .GetProperty("LocalPlayer")
                        .GetValue(bootstrap)
                        as GameObject;

                remotePlayer =
                    bootstrapType
                        .GetProperty("RemotePlayer")
                        .GetValue(bootstrap)
                        as GameObject;

                Assert.That(registry, Is.Not.Null);
                Assert.That(registry.Count, Is.EqualTo(2));

                Assert.That(
                    registry.TryGetPlayer(
                        11u,
                        out GameObject registeredHost),
                    Is.True);

                Assert.That(
                    registry.TryGetPlayer(
                        22u,
                        out GameObject registeredClient),
                    Is.True);

                Assert.That(
                    registeredHost,
                    Is.SameAs(scenePlayer));

                Assert.That(
                    registeredClient,
                    Is.SameAs(remotePlayer));

                Assert.That(
                    localPlayer,
                    Is.SameAs(scenePlayer));

                Assert.That(
                    remotePlayer,
                    Is.Not.Null);

                Assert.That(
                    remotePlayer,
                    Is.Not.SameAs(scenePlayer));

                Assert.That(
                    scenePlayer.transform.position,
                    Is.EqualTo(
                        hostSpawnObject.transform.position));

                Assert.That(
                    remotePlayer.transform.position,
                    Is.EqualTo(
                        clientSpawnObject.transform.position));

                Component remoteInput =
                    remotePlayer.GetComponent(
                        remoteInputSourceType);

                Behaviour remoteController =
                    remotePlayer.GetComponent(
                        playerControllerType)
                    as Behaviour;

                Behaviour remoteShooter =
                    remotePlayer.GetComponent(
                        playerShooterType)
                    as Behaviour;

                Assert.That(
                    remoteInput,
                    Is.Not.Null);

                Assert.That(
                    remoteController,
                    Is.Not.Null);

                Assert.That(
                    remoteController.enabled,
                    Is.True);

                Assert.That(
                    remoteShooter,
                    Is.Not.Null);

                Assert.That(
                    remoteShooter.enabled,
                    Is.True,
                    "The host must enable authoritative shooting " +
                    "for the remote client player.");

                FieldInfo shooterInputSourceField =
                    playerShooterType.GetField(
                        "inputSource",
                        BindingFlags.Instance |
                        BindingFlags.NonPublic);

                Assert.That(
                    shooterInputSourceField,
                    Is.Not.Null);

                Assert.That(
                    shooterInputSourceField.GetValue(
                        remoteShooter),
                    Is.SameAs(remoteInput),
                    "The remote shooter must use " +
                    "RemotePlayerInputSource.");

                var input =
                    new PlayerInputPayload(
                        0.6f,
                        0.8f,
                        -1f,
                        0.25f);

                InvokePrivate(
                    bootstrap,
                    "HandleRemotePlayerInput",
                    22u,
                    input);

                Assert.That(
                    ReadVector2Property(
                        remoteInput,
                        "MoveDirection"),
                    Is.EqualTo(
                        new Vector2(0.6f, 0.8f)));

                Assert.That(
                    ReadVector2Property(
                        remoteInput,
                        "AimDirection"),
                    Is.EqualTo(
                        new Vector2(-1f, 0.25f).normalized));

                GameSession.ConfigureMultiplayerHost();

                var sentSnapshots =
                    new List<PlayerStateSnapshotPayload>();

                Action<PlayerStateSnapshotPayload> sender =
                    sentSnapshots.Add;

                InvokePrivate(
                    bootstrap,
                    "TryConfigureHostStatePublisher",
                    11u,
                    22u,
                    sender);

                Component statePublisher =
                    bootstrapObject.GetComponent(
                        statePublisherType);

                Assert.That(
                    statePublisher,
                    Is.Not.Null,
                    "The host bootstrap should create a " +
                    "HostPlayerStatePublisher.");

                MethodInfo advanceMethod =
                    statePublisherType.GetMethod(
                        "Advance",
                        BindingFlags.Instance |
                        BindingFlags.NonPublic);

                Assert.That(
                    advanceMethod,
                    Is.Not.Null);

                advanceMethod.Invoke(
                    statePublisher,
                    new object[] { 0.051f });

                Assert.That(
                    sentSnapshots.Count,
                    Is.EqualTo(1));

                Assert.That(
                    sentSnapshots[0].Players.Count,
                    Is.EqualTo(2));

                Assert.That(
                    sentSnapshots[0].Players[0].PlayerId,
                    Is.EqualTo(11u));

                Assert.That(
                    sentSnapshots[0].Players[1].PlayerId,
                    Is.EqualTo(22u));
            }
            finally
            {
                GameSession.Reset();

                UnityEngine.Object.DestroyImmediate(
                    bootstrapObject);

                if (remotePlayer != null)
                {
                    UnityEngine.Object.DestroyImmediate(
                        remotePlayer);
                }

                UnityEngine.Object.DestroyImmediate(
                    scenePlayer);

                UnityEngine.Object.DestroyImmediate(
                    hostSpawnObject);

                UnityEngine.Object.DestroyImmediate(
                    clientSpawnObject);
            }
        }

        [Test]
        public void ConfigureClientPlayers_CreatesTwoRegisteredPlayers()
        {
            Type bootstrapType =
                FindType(BootstrapTypeName);

            Type interpolatorType =
                FindType(
                    "TopDownRoguelike.Gameplay.Networking." +
                    "RemotePlayerInterpolator");

            Type localInputSourceType =
                FindType(
                    "TopDownRoguelike.Gameplay.Characters." +
                    "LocalPlayerInputSource");

            Type playerControllerType =
                FindType(
                    "PlayerController");

            Assert.That(bootstrapType, Is.Not.Null);

            Assert.That(
                interpolatorType,
                Is.Not.Null);

            Assert.That(
                localInputSourceType,
                Is.Not.Null);

            Assert.That(
                playerControllerType,
                Is.Not.Null);

            var bootstrapObject =
                new GameObject("Bootstrap Test");

            var scenePlayer =
                new GameObject("Scene Player");

            scenePlayer.AddComponent<Rigidbody2D>();

            scenePlayer.AddComponent(
                localInputSourceType);

            scenePlayer.AddComponent(
                playerControllerType);

            var hostSpawnObject =
                new GameObject("Host Spawn Point");

            var clientSpawnObject =
                new GameObject("Client Spawn Point");

            bootstrapObject.SetActive(false);
            scenePlayer.SetActive(false);

            hostSpawnObject.transform.position =
                new Vector3(-2f, 1f, 0f);

            clientSpawnObject.transform.position =
                new Vector3(4f, -1f, 0f);

            GameObject remotePlayer = null;

            try
            {
                Component bootstrap =
                    bootstrapObject.AddComponent(
                        bootstrapType);

                SetPrivateField(
                    bootstrap,
                    "scenePlayer",
                    scenePlayer);

                SetPrivateField(
                    bootstrap,
                    "hostSpawnPoint",
                    hostSpawnObject.transform);

                SetPrivateField(
                    bootstrap,
                    "clientSpawnPoint",
                    clientSpawnObject.transform);

                InvokePrivate(bootstrap, "Awake");

                InvokePrivate(
                    bootstrap,
                    "ConfigureClientPlayers",
                    22u,
                    11u);

                var registry =
                    bootstrapType
                        .GetProperty("Registry")
                        .GetValue(bootstrap)
                        as NetworkPlayerRegistry;

                var localPlayer =
                    bootstrapType
                        .GetProperty("LocalPlayer")
                        .GetValue(bootstrap)
                        as GameObject;

                remotePlayer =
                    bootstrapType
                        .GetProperty("RemotePlayer")
                        .GetValue(bootstrap)
                        as GameObject;

                Assert.That(registry, Is.Not.Null);
                Assert.That(registry.Count, Is.EqualTo(2));

                Assert.That(
                    registry.TryGetPlayer(
                        22u,
                        out GameObject registeredClient),
                    Is.True);

                Assert.That(
                    registry.TryGetPlayer(
                        11u,
                        out GameObject registeredHost),
                    Is.True);

                Assert.That(
                    registeredClient,
                    Is.SameAs(scenePlayer));

                Assert.That(
                    registeredHost,
                    Is.SameAs(remotePlayer));

                Assert.That(
                    localPlayer,
                    Is.SameAs(scenePlayer));

                Assert.That(
                    remotePlayer,
                    Is.Not.Null);

                Assert.That(
                    scenePlayer.transform.position,
                    Is.EqualTo(
                        clientSpawnObject.transform.position));

                Assert.That(
                    remotePlayer.transform.position,
                    Is.EqualTo(
                        hostSpawnObject.transform.position));

                Component interpolator =
                    remotePlayer.GetComponent(
                        interpolatorType);

                Assert.That(
                    interpolator,
                    Is.Not.Null,
                    "The client remote host object should " +
                    "receive a RemotePlayerInterpolator.");

                FieldInfo remotePlayerIdField =
                    interpolatorType.GetField(
                        "remotePlayerId",
                        BindingFlags.Instance |
                        BindingFlags.NonPublic);

                Assert.That(
                    remotePlayerIdField,
                    Is.Not.Null);

                Assert.That(
                    remotePlayerIdField.GetValue(
                        interpolator),
                    Is.EqualTo(11u),
                    "The remote object must be bound to " +
                    "the host player ID.");

                Assert.That(
                    ((Behaviour)interpolator).enabled,
                    Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(
                    bootstrapObject);

                if (remotePlayer != null)
                {
                    UnityEngine.Object.DestroyImmediate(
                        remotePlayer);
                }

                UnityEngine.Object.DestroyImmediate(
                    scenePlayer);

                UnityEngine.Object.DestroyImmediate(
                    hostSpawnObject);

                UnityEngine.Object.DestroyImmediate(
                    clientSpawnObject);
            }
        }

        [Test]
        public void HandleRemotePlayerStateSnapshot_RaisesGameplayEvent()
        {
            Type bootstrapType =
                FindType(BootstrapTypeName);

            Assert.That(
                bootstrapType,
                Is.Not.Null);

            var bootstrapObject =
                new GameObject(
                    "State Event Bridge Test");

            bootstrapObject.SetActive(false);

            try
            {
                Component bootstrap =
                    bootstrapObject.AddComponent(
                        bootstrapType);

                var snapshot =
                    new PlayerStateSnapshotPayload(
                        new[]
                        {
                    new PlayerStateRecord(
                        11u,
                        -1f,
                        2f,
                        1f,
                        0f),

                    new PlayerStateRecord(
                        22u,
                        3f,
                        -2f,
                        0f,
                        1f)
                        });

                uint receivedSenderId =
                    0u;

                PlayerStateSnapshotPayload
                    receivedSnapshot = null;

                EventInfo stateEvent =
                    bootstrapType.GetEvent(
                        "PlayerStateSnapshotReceived",
                        BindingFlags.Instance |
                        BindingFlags.Public);

                Assert.That(
                    stateEvent,
                    Is.Not.Null,
                    "NetworkGameBootstrap must expose " +
                    "PlayerStateSnapshotReceived.");

                Action<uint, PlayerStateSnapshotPayload>
                    handler =
                        (senderId, received) =>
                        {
                            receivedSenderId =
                                senderId;

                            receivedSnapshot =
                                received;
                        };

                stateEvent.AddEventHandler(
                    bootstrap,
                    handler);

                InvokePrivate(
                    bootstrap,
                    "HandleRemotePlayerStateSnapshot",
                    11u,
                    snapshot);

                Assert.That(
                    receivedSenderId,
                    Is.EqualTo(11u));

                Assert.That(
                    receivedSnapshot,
                    Is.SameAs(snapshot));

                Assert.That(
                    receivedSnapshot.Players.Count,
                    Is.EqualTo(2));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(
                    bootstrapObject);
            }
        }

        [Test]
        public void SubscribeToRemoteStateSnapshots_ForwardsClientSnapshotEvent()
        {
            Type bootstrapType =
                FindType(BootstrapTypeName);

            Assert.That(
                bootstrapType,
                Is.Not.Null);

            var bootstrapObject =
                new GameObject(
                    "State Subscription Test");

            bootstrapObject.SetActive(false);

            var client =
                new NetworkClient();

            try
            {
                Component bootstrap =
                    bootstrapObject.AddComponent(
                        bootstrapType);

                InvokePrivate(
                    bootstrap,
                    "Awake");

                PropertyInfo stateProperty =
                    typeof(NetworkClient).GetProperty(
                        nameof(NetworkClient.State),
                        BindingFlags.Instance |
                        BindingFlags.Public);

                MethodInfo stateSetter =
                    stateProperty?.GetSetMethod(true);

                Assert.That(
                    stateSetter,
                    Is.Not.Null);

                stateSetter.Invoke(
                    client,
                    new object[]
                    {
                NetworkClientState.InRoom
                    });

                uint receivedSenderId =
                    0u;

                PlayerStateSnapshotPayload
                    receivedSnapshot = null;

                EventInfo bootstrapEvent =
                    bootstrapType.GetEvent(
                        "PlayerStateSnapshotReceived",
                        BindingFlags.Instance |
                        BindingFlags.Public);

                Assert.That(
                    bootstrapEvent,
                    Is.Not.Null);

                Action<uint, PlayerStateSnapshotPayload>
                    handler =
                        (senderId, snapshot) =>
                        {
                            receivedSenderId =
                                senderId;

                            receivedSnapshot =
                                snapshot;
                        };

                bootstrapEvent.AddEventHandler(
                    bootstrap,
                    handler);

                InvokePrivate(
                    bootstrap,
                    "SubscribeToRemoteStateSnapshots",
                    client);

                var expectedSnapshot =
                    new PlayerStateSnapshotPayload(
                        new[]
                        {
                    new PlayerStateRecord(
                        11u,
                        -1f,
                        2f,
                        1f,
                        0f),

                    new PlayerStateRecord(
                        22u,
                        3f,
                        -2f,
                        0f,
                        1f)
                        });

                NetworkTransportEvent transportEvent =
                    NetworkTransportEvent.UdpPacketReceived(
                        MessageType.PlayerStateSnapshot,
                        11u,
                        8u,
                        PlayerStateSnapshotCodec.Encode(
                            expectedSnapshot));

                MethodInfo handleTransportEvent =
                    typeof(NetworkClient).GetMethod(
                        "HandleTransportEvent",
                        BindingFlags.Instance |
                        BindingFlags.NonPublic);

                Assert.That(
                    handleTransportEvent,
                    Is.Not.Null);

                handleTransportEvent.Invoke(
                    client,
                    new object[]
                    {
                transportEvent
                    });

                Assert.That(
                    receivedSenderId,
                    Is.EqualTo(11u));

                Assert.That(
                    receivedSnapshot,
                    Is.Not.Null);

                Assert.That(
                    receivedSnapshot.Players.Count,
                    Is.EqualTo(2));
            }
            finally
            {
                client.Dispose();

                UnityEngine.Object.DestroyImmediate(
                    bootstrapObject);
            }
        }

        [Test]
        public void TryConfigureRemoteInterpolator_AttachesRemotePlayerId()
        {
            Type bootstrapType =
                FindType(BootstrapTypeName);

            Type interpolatorType =
                FindType(
                    "TopDownRoguelike.Gameplay.Networking." +
                    "RemotePlayerInterpolator");

            Assert.That(
                bootstrapType,
                Is.Not.Null);

            Assert.That(
                interpolatorType,
                Is.Not.Null,
                "RemotePlayerInterpolator must exist.");

            var bootstrapObject =
                new GameObject(
                    "Remote Interpolator Bootstrap Test");

            var remotePlayer =
                new GameObject(
                    "Remote Player Test");

            bootstrapObject.SetActive(false);
            remotePlayer.SetActive(false);

            try
            {
                Component bootstrap =
                    bootstrapObject.AddComponent(
                        bootstrapType);

                MethodInfo configureMethod =
                    bootstrapType.GetMethod(
                        "TryConfigureRemoteInterpolator",
                        BindingFlags.Static |
                        BindingFlags.NonPublic);

                Assert.That(
                    configureMethod,
                    Is.Not.Null,
                    "TryConfigureRemoteInterpolator " +
                    "must exist.");

                object result =
                    configureMethod.Invoke(
                        null,
                        new object[]
                        {
                            remotePlayer,
                            11u
                        });

                Assert.That(
                    result,
                    Is.EqualTo(true));

                Component interpolator =
                    remotePlayer.GetComponent(
                        interpolatorType);

                Assert.That(
                    interpolator,
                    Is.Not.Null);

                FieldInfo playerIdField =
                    interpolatorType.GetField(
                        "remotePlayerId",
                        BindingFlags.Instance |
                        BindingFlags.NonPublic);

                Assert.That(
                    playerIdField,
                    Is.Not.Null);

                Assert.That(
                    playerIdField.GetValue(
                        interpolator),
                    Is.EqualTo(11u));

                Assert.That(
                    ((Behaviour)interpolator).enabled,
                    Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(
                    bootstrapObject);

                UnityEngine.Object.DestroyImmediate(
                    remotePlayer);
            }
        }

        [Test]
        public void HandleRemotePlayerStateSnapshot_UpdatesRemoteInterpolator()
        {
            Type bootstrapType =
                FindType(BootstrapTypeName);

            Type interpolatorType =
                FindType(
                    "TopDownRoguelike.Gameplay.Networking." +
                    "RemotePlayerInterpolator");

            Assert.That(
                bootstrapType,
                Is.Not.Null);

            Assert.That(
                interpolatorType,
                Is.Not.Null);

            var bootstrapObject =
                new GameObject(
                    "Remote State Consumer Test");

            var remotePlayer =
                new GameObject(
                    "Remote Player Consumer Test");

            bootstrapObject.SetActive(false);
            remotePlayer.SetActive(false);

            try
            {
                Component bootstrap =
                    bootstrapObject.AddComponent(
                        bootstrapType);

                Component interpolator =
                    remotePlayer.AddComponent(
                        interpolatorType);

                MethodInfo configureMethod =
                    interpolatorType.GetMethod(
                        "Configure",
                        BindingFlags.Instance |
                        BindingFlags.Public);

                Assert.That(
                    configureMethod,
                    Is.Not.Null);

                configureMethod.Invoke(
                    interpolator,
                    new object[]
                    {
                        11u
                    });

                SetPrivateField(
                    bootstrap,
                    "remotePlayer",
                    remotePlayer);

                InvokePrivate(
                    bootstrap,
                    "Awake");

                var snapshot =
                    new PlayerStateSnapshotPayload(
                        new[]
                        {
                    new PlayerStateRecord(
                        11u,
                        8f,
                        -3f,
                        1f,
                        0f)
                        });

                InvokePrivate(
                    bootstrap,
                    "HandleRemotePlayerStateSnapshot",
                    11u,
                    snapshot);

                MethodInfo advanceMethod =
                    interpolatorType.GetMethod(
                        "Advance",
                        BindingFlags.Instance |
                        BindingFlags.NonPublic);

                Assert.That(
                    advanceMethod,
                    Is.Not.Null);

                advanceMethod.Invoke(
                    interpolator,
                    new object[]
                    {
                0.05f
                    });

                Assert.That(
                    remotePlayer.transform.position.x,
                    Is.EqualTo(8f).Within(0.01f));

                Assert.That(
                    remotePlayer.transform.position.y,
                    Is.EqualTo(-3f).Within(0.01f));

                Assert.That(
                    remotePlayer.transform.eulerAngles.z,
                    Is.EqualTo(0f).Within(0.01f));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(
                    bootstrapObject);

                UnityEngine.Object.DestroyImmediate(
                    remotePlayer);
            }
        }

        [Test]
        public void TryConfigureClientInputPublisher_AttachesConfiguredPublisher()
        {
            Type bootstrapType =
                FindType(BootstrapTypeName);

            Type localInputType =
                FindType(
                    "TopDownRoguelike.Gameplay.Characters." +
                    "LocalPlayerInputSource");

            Type publisherType =
                FindType(
                    "TopDownRoguelike.Gameplay.Networking." +
                    "ClientPlayerInputPublisher");

            Assert.That(bootstrapType, Is.Not.Null);
            Assert.That(localInputType, Is.Not.Null);
            Assert.That(publisherType, Is.Not.Null);

            var bootstrapObject =
                new GameObject("Bootstrap Test");

            var playerObject =
                new GameObject("Client Player Test");

            bootstrapObject.SetActive(false);
            playerObject.SetActive(false);

            try
            {
                Component bootstrap =
                    bootstrapObject.AddComponent(
                        bootstrapType);

                Component localInput =
                    playerObject.AddComponent(
                        localInputType);

                Action<PlayerInputPayload> sender =
                    _ => { };

                MethodInfo configureMethod =
                    bootstrapType.GetMethod(
                        "TryConfigureClientInputPublisher",
                        BindingFlags.Static |
                        BindingFlags.NonPublic);

                Assert.That(
                    configureMethod,
                    Is.Not.Null,
                    "TryConfigureClientInputPublisher must exist.");

                object result =
                    configureMethod.Invoke(
                        null,
                        new object[]
                        {
                            playerObject,
                            sender
                        });

                Assert.That(
                    result,
                    Is.EqualTo(true));

                Component publisher =
                    playerObject.GetComponent(
                        publisherType);

                Assert.That(
                    publisher,
                    Is.Not.Null);

                Assert.That(
                    ((Behaviour)publisher).enabled,
                    Is.True);

                FieldInfo inputSourceField =
                    publisherType.GetField(
                        "inputSource",
                        BindingFlags.Instance |
                        BindingFlags.NonPublic);

                FieldInfo senderField =
                    publisherType.GetField(
                        "sendInput",
                        BindingFlags.Instance |
                        BindingFlags.NonPublic);

                Assert.That(
                    inputSourceField,
                    Is.Not.Null);

                Assert.That(
                    senderField,
                    Is.Not.Null);

                Assert.That(
                    inputSourceField.GetValue(
                        publisher),
                    Is.SameAs(localInput));

                Assert.That(
                    senderField.GetValue(
                        publisher),
                    Is.Not.Null);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(
                    bootstrapObject);

                UnityEngine.Object.DestroyImmediate(
                    playerObject);
            }
        }

        [Test]
        public void BindCameraToLocalPlayer_AssignsBootstrapLocalPlayer()
        {
            Type bootstrapType =
                FindType(BootstrapTypeName);

            Type cameraFollowType =
                FindType("CameraFollow");

            Assert.That(bootstrapType, Is.Not.Null);
            Assert.That(cameraFollowType, Is.Not.Null);

            var bootstrapObject =
                new GameObject("Bootstrap Test");

            var cameraObject =
                new GameObject("Camera Test");

            var localPlayer =
                new GameObject("Local Player");

            bootstrapObject.SetActive(false);
            cameraObject.SetActive(false);

            try
            {
                cameraObject.AddComponent<Camera>();

                Component cameraFollow =
                    cameraObject.AddComponent(
                        cameraFollowType);

                Component bootstrap =
                    bootstrapObject.AddComponent(
                        bootstrapType);

                SetPrivateField(
                    bootstrap,
                    "localPlayer",
                    localPlayer);

                SetPrivateField(
                    bootstrap,
                    "cameraFollow",
                    cameraFollow);

                InvokePrivate(
                    bootstrap,
                    "BindCameraToLocalPlayer");

                PropertyInfo targetProperty =
                    cameraFollowType.GetProperty(
                        "Target",
                        BindingFlags.Instance |
                        BindingFlags.Public);

                Assert.That(
                    targetProperty,
                    Is.Not.Null,
                    "CameraFollow must expose Target.");

                var actualTarget =
                    targetProperty.GetValue(cameraFollow)
                        as Transform;

                Assert.That(
                    actualTarget,
                    Is.SameAs(localPlayer.transform));

                Assert.That(
                    ((Behaviour)cameraFollow).enabled,
                    Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(
                    bootstrapObject);

                UnityEngine.Object.DestroyImmediate(
                    cameraObject);

                UnityEngine.Object.DestroyImmediate(
                    localPlayer);
            }
        }

        [Test]
        public void BindHealthBarToLocalPlayer_BindsBootstrapLocalPlayer()
        {
            Type bootstrapType =
                FindType(BootstrapTypeName);

            Type healthBarViewType =
                FindType(
                    "TopDownRoguelike.Gameplay.UI." +
                    "HealthBarView");

            Assert.That(bootstrapType, Is.Not.Null);
            Assert.That(healthBarViewType, Is.Not.Null);

            var bootstrapObject =
                new GameObject("Bootstrap Test");

            var localPlayer =
                new GameObject("Local Player");

            GameObject healthBarObject = null;

            bootstrapObject.SetActive(false);
            localPlayer.SetActive(false);

            try
            {
                Component playerHealth =
                    AddInitializedPlayerHealth(
                        localPlayer);

                Component healthBarView =
                    CreateHealthBarView(
                        healthBarViewType,
                        out healthBarObject);

                Component bootstrap =
                    bootstrapObject.AddComponent(
                        bootstrapType);

                SetPrivateField(
                    bootstrap,
                    "localPlayer",
                    localPlayer);

                SetPrivateField(
                    bootstrap,
                    "healthBarView",
                    healthBarView);

                InvokePrivate(
                    bootstrap,
                    "BindHealthBarToLocalPlayer");

                FieldInfo boundHealthField =
                    healthBarViewType.GetField(
                        "boundPlayerHealth",
                        BindingFlags.Instance |
                        BindingFlags.NonPublic);

                Assert.That(boundHealthField, Is.Not.Null);

                Assert.That(
                    boundHealthField.GetValue(healthBarView),
                    Is.SameAs(playerHealth));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(
                    bootstrapObject);

                UnityEngine.Object.DestroyImmediate(
                    localPlayer);

                if (healthBarObject != null)
                {
                    UnityEngine.Object.DestroyImmediate(
                        healthBarObject);
                }
            }
        }

        [Test]
        public void DisableRemotePlayerHealthBars_DisablesNestedViews()
        {
            Type bootstrapType = FindType(BootstrapTypeName);
            Type healthBarViewType = FindType(
                "TopDownRoguelike.Gameplay.UI.HealthBarView");
            var bootstrapObject = new GameObject("Bootstrap Test");
            var remotePlayer = new GameObject("Remote Player");
            var nestedViewObject = new GameObject("Remote Health Bar");
            nestedViewObject.transform.SetParent(remotePlayer.transform);

            try
            {
                Component nestedView = nestedViewObject.AddComponent(healthBarViewType);
                Component bootstrap = bootstrapObject.AddComponent(bootstrapType);
                MethodInfo method = bootstrapType.GetMethod(
                    "DisableRemotePlayerHealthBars",
                    BindingFlags.Instance |
                    BindingFlags.Static |
                    BindingFlags.NonPublic);

                Assert.That(method, Is.Not.Null);
                method.Invoke(bootstrap, new object[] { remotePlayer });

                Assert.That(((Behaviour)nestedView).enabled, Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(bootstrapObject);
                UnityEngine.Object.DestroyImmediate(remotePlayer);
            }
        }

        [Test]
        public void NetworkGameBootstrap_ExposesHostShotgunPublisherConfiguration()
        {
            Type bootstrapType =
                FindType(
                    "TopDownRoguelike.Gameplay.Networking." +
                    "NetworkGameBootstrap");

            Assert.That(
                bootstrapType,
                Is.Not.Null,
                "NetworkGameBootstrap must exist.");

            MethodInfo method =
                bootstrapType.GetMethod(
                    "TryConfigureHostShotgunPublishers",
                    BindingFlags.Instance |
                    BindingFlags.NonPublic);

            Assert.That(
                method,
                Is.Not.Null,
                "NetworkGameBootstrap must configure " +
                "shotgun publishers for both host players.");
        }

        [Test]
        public void NetworkGameBootstrap_ExposesShotgunEventRouting()
        {
            Type bootstrapType =
                FindType(
                    "TopDownRoguelike.Gameplay.Networking." +
                    "NetworkGameBootstrap");

            Assert.That(
                bootstrapType,
                Is.Not.Null);

            EventInfo eventInfo =
                bootstrapType.GetEvent(
                    "PlayerShotgunEventReceived",
                    BindingFlags.Instance |
                    BindingFlags.Public);

            Assert.That(
                eventInfo,
                Is.Not.Null,
                "NetworkGameBootstrap must expose " +
                "PlayerShotgunEventReceived.");

            MethodInfo subscribeMethod =
                bootstrapType.GetMethod(
                    "SubscribeToRemoteShotgunEvents",
                    BindingFlags.Instance |
                    BindingFlags.NonPublic);

            Assert.That(
                subscribeMethod,
                Is.Not.Null,
                "NetworkGameBootstrap must subscribe to " +
                "NetworkClient.PlayerShotgunEventReceived.");
        }

        [Test]
        public void NetworkGameBootstrap_ExposesRemoteShotgunVisualConfiguration()
        {
            Type bootstrapType =
                FindType(
                    "TopDownRoguelike.Gameplay.Networking." +
                    "NetworkGameBootstrap");

            Assert.That(
                bootstrapType,
                Is.Not.Null);

            MethodInfo receiverMethod =
                bootstrapType.GetMethod(
                    "TryConfigureRemoteShotgunReceiver",
                    BindingFlags.Instance |
                    BindingFlags.NonPublic);

            Assert.That(
                receiverMethod,
                Is.Not.Null,
                "NetworkGameBootstrap must configure " +
                "the remote shotgun receiver.");

            MethodInfo spawnerMethod =
                bootstrapType.GetMethod(
                    "TryConfigureRemoteShotgunSpawner",
                    BindingFlags.Static |
                    BindingFlags.NonPublic);

            Assert.That(
                spawnerMethod,
                Is.Not.Null,
                "NetworkGameBootstrap must configure " +
                "the remote shotgun visual spawner.");
        }

        private static Component AddInitializedPlayerHealth(
            GameObject playerObject)
        {
            Type playerHealthType =
                FindType(
                    "TopDownRoguelike.Gameplay.Characters." +
                    "PlayerHealth");

            Assert.That(playerHealthType, Is.Not.Null);

            Component playerHealth =
                playerObject.AddComponent(
                    playerHealthType);

            InvokePrivate(
                playerHealth,
                "Awake");

            return playerHealth;
        }

        private static Component CreateHealthBarView(
            Type healthBarViewType,
            out GameObject viewObject)
        {
            viewObject =
                new GameObject("Health Bar View Test");

            viewObject.SetActive(false);

            var sliderObject =
                new GameObject(
                    "Health Slider",
                    typeof(RectTransform));

            var textObject =
                new GameObject(
                    "Health Text",
                    typeof(RectTransform));

            sliderObject.transform.SetParent(
                viewObject.transform);

            textObject.transform.SetParent(
                viewObject.transform);

            Slider slider =
                sliderObject.AddComponent<Slider>();

            TMP_Text healthText =
                textObject.AddComponent<
                    TextMeshProUGUI>();

            Component healthBarView =
                viewObject.AddComponent(
                    healthBarViewType);

            SetPrivateField(
                healthBarView,
                "healthSlider",
                slider);

            SetPrivateField(
                healthBarView,
                "healthText",
                healthText);

            return healthBarView;
        }

        private static Vector2 ReadVector2Property(
            Component target,
            string propertyName)
        {
            Assert.That(
                target,
                Is.Not.Null);

            PropertyInfo property =
                target.GetType().GetProperty(
                    propertyName,
                    BindingFlags.Instance |
                    BindingFlags.Public);

            Assert.That(
                property,
                Is.Not.Null,
                $"{propertyName} must exist.");

            return (Vector2)property.GetValue(
                target);
        }

        private static void SetPrivateField(
            Component target,
            string fieldName,
            object value)
        {
            FieldInfo field =
                target.GetType().GetField(
                    fieldName,
                    BindingFlags.Instance |
                    BindingFlags.NonPublic);

            Assert.That(
                field,
                Is.Not.Null,
                $"{fieldName} must exist.");

            field.SetValue(target, value);
        }

        private static void InvokePrivate(
            Component target,
            string methodName,
            params object[] arguments)
        {
            MethodInfo method =
                target.GetType().GetMethod(
                    methodName,
                    BindingFlags.Instance |
                    BindingFlags.NonPublic);

            Assert.That(
                method,
                Is.Not.Null,
                $"{methodName} must exist.");

            method.Invoke(
                target,
                arguments);
        }

        private static Type FindType(
            string fullTypeName)
        {
            foreach (var assembly in
                AppDomain.CurrentDomain.GetAssemblies())
            {
                Type result =
                    assembly.GetType(fullTypeName, false);

                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }
    }
}
