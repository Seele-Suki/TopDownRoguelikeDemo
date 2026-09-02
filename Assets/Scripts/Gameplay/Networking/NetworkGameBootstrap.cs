using System;
using System.Collections.Generic;
using TopDownRoguelike.Gameplay.Bosses;
using TopDownRoguelike.Gameplay.Characters;
using TopDownRoguelike.Gameplay.Core;
using TopDownRoguelike.Gameplay.Enemies;
using TopDownRoguelike.Gameplay.Experience;
using TopDownRoguelike.Gameplay.Upgrades;
using TopDownRoguelike.Gameplay.Weapons;
using TopDownRoguelike.Gameplay.UI;
using TopDownRoguelike.Infrastructure;
using TopDownRoguelike.Networking.Client;
using TopDownRoguelike.Networking.Gameplay;
using TopDownRoguelike.Networking.Protocol;
using UnityEngine;

namespace TopDownRoguelike.Gameplay.Networking
{
    public sealed class NetworkGameBootstrap
        : MonoBehaviour
    {
        private const uint SinglePlayerId = 1u;

        [Header("Player Spawning")]
        [SerializeField]
        private GameObject playerPrefab;

        [SerializeField]
        private GameObject remoteProjectileVisualPrefab;

        [SerializeField]
        private GameObject scenePlayer;

        [SerializeField]
        private Transform hostSpawnPoint;

        [SerializeField]
        private Transform clientSpawnPoint;

        [Header("Local View")]
        [SerializeField]
        private CameraFollow cameraFollow;

        [SerializeField]
        private HealthBarView healthBarView;

        private NetworkPlayerRegistry registry;
        private NetworkEntityRegistry entityRegistry;
        private GameObject localPlayer;
        private GameObject remotePlayer;
        private NetworkClient remoteInputClient;
        private NetworkClient stateSnapshotClient;
        private NetworkClient shotEventClient;
        private NetworkClient shotgunEventClient;
        private uint lastSharedExperienceSequence;
        private ClientWorldSnapshotConsumer clientWorldSnapshotConsumer;
        private NetworkUpgradeCoordinator networkUpgradeCoordinator;
        private NetworkBossCoordinator networkBossCoordinator;
        private NetworkClient bossCombatStateClient;
        private NetworkClient gameResultClient;
        private GameManager gameResultGameManager;
        private PlayerHealth clientLocalPlayerHealth;
        private PlayerHealth hostRemotePlayerHealth;
        private NetworkClient playerDeathClient;
        private LevelSystem hostUpgradeLevelSystem;
        private NetworkClient upgradeMessageClient;

        public NetworkPlayerRegistry Registry =>
            registry;

        public GameObject LocalPlayer =>
            localPlayer;

        public GameObject RemotePlayer =>
            remotePlayer;

        public event Action<
            uint,
            PlayerStateSnapshotPayload>
            PlayerStateSnapshotReceived;

        public event Action<
            uint,
            PlayerShotgunEvent>
            PlayerShotgunEventReceived;

        private void Awake()
        {
            registry =
                new NetworkPlayerRegistry();
            entityRegistry =
                new NetworkEntityRegistry();
        }

        private void Start()
        {
            switch (GameSession.CurrentMode)
            {
                case GameMode.SinglePlayer:
                    ConfigureSinglePlayer();
                    break;

                case GameMode.MultiplayerHost:
                    ConfigureMultiplayerHost();
                    break;

                case GameMode.MultiplayerClient:
                    ConfigureMultiplayerClient();
                    break;
            }

            if (enabled &&
                localPlayer != null)
            {
                BindCameraToLocalPlayer();

                if (enabled)
                {
                    BindHealthBarToLocalPlayer();
                }
            }
        }

        private void ConfigureSinglePlayer()
        {
            if (scenePlayer == null ||
                hostSpawnPoint == null)
            {
                FailConfiguration(
                    "Scene player or host spawn point " +
                    "is not assigned.");

                return;
            }

            scenePlayer.name = "Player";

            scenePlayer.transform.SetPositionAndRotation(
                hostSpawnPoint.position,
                hostSpawnPoint.rotation);

            scenePlayer.SetActive(true);

            if (!EnsurePlayerNetworkEntityId(
                    scenePlayer,
                    SinglePlayerId))
            {
                FailConfiguration(
                    "Failed to assign the single-player entity ID.");
                return;
            }

            if (!registry.TryRegister(
                    SinglePlayerId,
                    scenePlayer))
            {
                FailConfiguration(
                    "Failed to register the " +
                    "single-player object.");

                return;
            }

            localPlayer = scenePlayer;
            RegisterEntity(scenePlayer);
        }

        private void ConfigureMultiplayerHost()
        {
            EnsureSharedExperienceState();

            NetworkClientBehaviour networkBehaviour =
                NetworkClientBehaviour.Instance;

            if (networkBehaviour == null ||
                networkBehaviour.Client == null)
            {
                FailConfiguration(
                    "Network client is not available.");

                return;
            }

            uint localPlayerId =
                networkBehaviour.Client.PlayerId;

            RoomStateSnapshot roomState =
                networkBehaviour.Client.CurrentRoomState;

            if (!TryResolveHostPlayerIds(
                    localPlayerId,
                    roomState,
                    out uint remotePlayerId))
            {
                FailConfiguration(
                    "The host room player IDs are invalid.");

                return;
            }

            ConfigureHostPlayers(
                localPlayerId,
                remotePlayerId);

            if (enabled)
            {
                SubscribeToRemoteInput(
                    networkBehaviour.Client);

                TryConfigureNetworkUpgradeCoordinator();
                TryConfigureNetworkBossCoordinator(networkBehaviour.Client);
                ConfigureGameResultSync(networkBehaviour.Client);
                ConfigurePlayerDeathSync(networkBehaviour.Client);

                if (!TryConfigureHostStatePublisher(
                        localPlayerId,
                        remotePlayerId,
                        networkBehaviour.Client
                            .SendPlayerStateSnapshot))
                {
                        FailConfiguration(
                            "The host state publisher " +
                            "could not be configured.");
                }

                if (enabled &&
                    !TryConfigureHostWorldSnapshotPublisher(
                        networkBehaviour.Client
                            .SendWorldStateSnapshot))
                {
                    FailConfiguration(
                        "The host world snapshot publisher " +
                        "could not be configured.");
                }

                if (enabled &&
                    !TryConfigureHostEnemySpawnPublisher(
                        networkBehaviour.Client
                            .SendWorldEntitySpawned))
                {
                        FailConfiguration(
                            "The host enemy spawn publisher " +
                            "could not be configured.");
                }

                if (enabled &&
                    !TryConfigureHostExperienceOrbSpawnPublisher(
                        networkBehaviour.Client
                            .SendWorldEntitySpawned))
                {
                    FailConfiguration(
                        "The host experience orb spawn publisher " +
                        "could not be configured.");
                }

                if (enabled &&
                    !TryConfigureHostBossSpawnPublisher(
                        networkBehaviour.Client
                            .SendWorldEntitySpawned))
                {
                    FailConfiguration(
                        "The host Boss spawn publisher could not be configured.");
                }

                if (enabled &&
                    !TryConfigureHostEnemyDeathPublisher(
                        networkBehaviour.Client
                            .SendWorldEntityRemoved))
                {
                    FailConfiguration(
                            "The host enemy death publisher " +
                            "could not be configured.");
                }

                if (enabled &&
                    !TryConfigureHostExperienceOrbCollectionPublisher(
                        networkBehaviour.Client
                            .SendWorldEntityRemoved))
                {
                    FailConfiguration(
                        "The host experience orb collection publisher " +
                        "could not be configured.");
                }

                if (enabled &&
                    !TryConfigureHostBossDeathPublisher(
                        networkBehaviour.Client
                            .SendWorldEntityRemoved))
                {
                    FailConfiguration(
                        "The host Boss death publisher could not be configured.");
                }

                ConfigureHostSharedExperiencePublisher(
                    networkBehaviour.Client.SendSharedExperienceSnapshot);

                SubscribeToHostUpgradeLevel();

                if (enabled &&
                    !TryConfigureHostShotPublisher(
                        localPlayerId,
                        networkBehaviour.Client
                            .SendPlayerShotEvent))
                {
                    FailConfiguration(
                        "The host shot publisher " +
                        "could not be configured.");
                }

                if (enabled &&
                    !TryConfigureHostShotgunPublishers(
                        localPlayerId,
                        remotePlayerId,
                        networkBehaviour.Client
                            .SendPlayerShotgunEvent))
                {
                    FailConfiguration(
                        "The host shotgun publishers " +
                        "could not be configured.");
                }
            }
        }

        private void ConfigureMultiplayerClient()
        {
            EnsureSharedExperienceState();

            NetworkClientBehaviour networkBehaviour =
                NetworkClientBehaviour.Instance;

            Debug.Log(
                $"NetworkGameBootstrap client setup: " +
                $"Instance={(networkBehaviour != null)}, " +
                $"State={networkBehaviour?.Client?.State}, " +
                $"PlayerId={networkBehaviour?.Client?.PlayerId}, " +
                $"RoomId={networkBehaviour?.Client?.CurrentRoomId}",
                this);

            if (networkBehaviour == null ||
                networkBehaviour.Client == null)
            {
                FailConfiguration(
                    "Network client is not available.");

                return;
            }

            uint localPlayerId =
                networkBehaviour.Client.PlayerId;

            RoomStateSnapshot roomState =
                networkBehaviour.Client.CurrentRoomState;

            if (!TryResolveClientPlayerIds(
                    localPlayerId,
                    roomState,
                    out uint hostPlayerId))
            {
                FailConfiguration(
                    "The client room player IDs are invalid.");

                return;
            }

            ConfigureClientPlayers(
                localPlayerId,
                hostPlayerId);

            if (enabled)
            {
                ConfigureClientWorldSnapshotConsumer(
                    networkBehaviour.Client,
                    hostPlayerId);

                TryConfigureNetworkUpgradeCoordinator();
                TryConfigureNetworkBossCoordinator(networkBehaviour.Client);
                ConfigureGameResultSync(networkBehaviour.Client);
                ConfigurePlayerDeathSync(networkBehaviour.Client);
            }

            if (enabled)
            {
                SubscribeToRemoteStateSnapshots(
                    networkBehaviour.Client);

                SubscribeToRemoteShotEvents(
                    networkBehaviour.Client);

                SubscribeToRemoteShotgunEvents(
                    networkBehaviour.Client);

                SubscribeToClientUpgradeMessages(
                    networkBehaviour.Client);

                networkBehaviour.Client.SharedExperienceSnapshotReceived +=
                    HandleSharedExperienceSnapshot;
            }

            if (enabled &&
                !TryConfigureClientInputPublisherWithStateGuard(
                    localPlayer,
                    networkBehaviour.Client
                        .SendPlayerInput,
                    () => networkBehaviour.Client.State ==
                        NetworkClientState.InRoom))
            {
                FailConfiguration(
                    "The client input publisher " +
                    "could not be configured.");
            }
        }

        private void ConfigureHostPlayers(
            uint localPlayerId,
            uint remotePlayerId)
        {
            if (scenePlayer == null ||
                hostSpawnPoint == null ||
                clientSpawnPoint == null)
            {
                FailConfiguration(
                    "Player or spawn point references " +
                    "are missing.");

                return;
            }

            if (localPlayerId == 0u ||
                remotePlayerId == 0u ||
                localPlayerId == remotePlayerId)
            {
                FailConfiguration(
                    "Player IDs must be non-zero " +
                    "and different.");

                return;
            }

            scenePlayer.name = "HostPlayer";

            scenePlayer.transform.SetPositionAndRotation(
                hostSpawnPoint.position,
                hostSpawnPoint.rotation);

            scenePlayer.SetActive(true);

            GameObject createdRemotePlayer =
                Instantiate(
                    scenePlayer,
                    clientSpawnPoint.position,
                    clientSpawnPoint.rotation);

            createdRemotePlayer.name =
                "ClientPlayer";

            DisableLocalControl(
                createdRemotePlayer);

            DisableRemotePlayerHealthBars(
                createdRemotePlayer);
            
            if (!TryEnableRemoteSimulation(
                createdRemotePlayer))
            {
                DestroyPlayer(
                    createdRemotePlayer);

                FailConfiguration(
                    "The remote player cannot be " +
                    "configured for host simulation.");

                return;
            }

            if (!EnsurePlayerNetworkEntityId(
                    scenePlayer,
                    localPlayerId) ||
                !EnsurePlayerNetworkEntityId(
                    createdRemotePlayer,
                    remotePlayerId))
            {
                DestroyPlayer(createdRemotePlayer);

                FailConfiguration(
                    "Failed to assign host player entity IDs.");
                return;
            }

            if (!registry.TryRegister(
                    localPlayerId,
                    scenePlayer) ||
                !registry.TryRegister(
                    remotePlayerId,
                    createdRemotePlayer))
            {
                DestroyPlayer(createdRemotePlayer);

                FailConfiguration(
                    "Failed to register host players.");

                return;
            }

            localPlayer = scenePlayer;
            remotePlayer = createdRemotePlayer;
            RegisterEntity(scenePlayer);
            RegisterEntity(createdRemotePlayer);
        }

        private void ConfigureClientPlayers(
            uint localPlayerId,
            uint hostPlayerId)
        {
            if (scenePlayer == null ||
                hostSpawnPoint == null ||
                clientSpawnPoint == null)
            {
                FailConfiguration(
                    "Player or spawn point references " +
                    "are missing.");

                return;
            }

            if (localPlayerId == 0u ||
                hostPlayerId == 0u ||
                localPlayerId == hostPlayerId)
            {
                FailConfiguration(
                    "Player IDs must be non-zero " +
                    "and different.");

                return;
            }

            scenePlayer.name = "ClientPlayer";

            scenePlayer.transform.SetPositionAndRotation(
                clientSpawnPoint.position,
                clientSpawnPoint.rotation);

            scenePlayer.SetActive(true);

            GameObject createdRemotePlayer =
                Instantiate(
                    scenePlayer,
                    hostSpawnPoint.position,
                    hostSpawnPoint.rotation);

            createdRemotePlayer.name =
                "HostPlayer";

            DisableComponent<DashSkill>(
                scenePlayer);

            DisableComponent<ShotgunSkill>(
                scenePlayer);

            DisableLocalControl(
                createdRemotePlayer);

            DisableRemotePlayerHealthBars(
                createdRemotePlayer);

            if (!EnsurePlayerNetworkEntityId(
                    scenePlayer,
                    localPlayerId) ||
                !EnsurePlayerNetworkEntityId(
                    createdRemotePlayer,
                    hostPlayerId))
            {
                DestroyPlayer(createdRemotePlayer);

                FailConfiguration(
                    "Failed to assign client player entity IDs.");
                return;
            }

            if (!TryConfigureLocalDashReconciler(
                    scenePlayer,
                    localPlayerId))
            {
                DestroyPlayer(
                    createdRemotePlayer);

                FailConfiguration(
                    "The local client dash reconciler " +
                    "could not be configured.");

                return;
            }

            if (!registry.TryRegister(
                localPlayerId,
                    scenePlayer) ||
                !registry.TryRegister(
                    hostPlayerId,
                    createdRemotePlayer))
            {
                DestroyPlayer(createdRemotePlayer);

                FailConfiguration(
                    "Failed to register client players.");

                return;
            }

            if (!TryConfigureRemoteInterpolator(
                createdRemotePlayer,
                hostPlayerId))
            {
                registry.Remove(
                    localPlayerId);

                registry.Remove(
                    hostPlayerId);

                DestroyPlayer(
                    createdRemotePlayer);

                FailConfiguration(
                    "The remote player interpolator " +
                    "could not be configured.");

                return;
            }

            if (!TryConfigureRemoteShotReceiver(
                createdRemotePlayer,
                hostPlayerId))
            {
                registry.Remove(
                    localPlayerId);

                registry.Remove(
                    hostPlayerId);

                DestroyPlayer(
                    createdRemotePlayer);

                FailConfiguration(
                    "The remote shot receiver could not " +
                    "be configured.");

                return;
            }

            if (!TryConfigureRemoteShotgunReceiver(
                createdRemotePlayer,
                hostPlayerId))
            {
                registry.Remove(
                    localPlayerId);

                registry.Remove(
                    hostPlayerId);

                DestroyPlayer(
                    createdRemotePlayer);

                FailConfiguration(
                    "The remote shotgun receiver could " +
                    "not be configured.");

                return;
            }

            if (remoteProjectileVisualPrefab == null)
            {
                if (Application.isPlaying)
                {
                    registry.Remove(
                        localPlayerId);

                    registry.Remove(
                        hostPlayerId);

                    DestroyPlayer(
                        createdRemotePlayer);

                    FailConfiguration(
                        "The remote projectile visual prefab " +
                        "is not assigned.");

                    return;
                }
            }
            else if (!TryConfigureRemoteShotSpawner(
                         createdRemotePlayer,
                         remoteProjectileVisualPrefab))
            {
                registry.Remove(
                    localPlayerId);

                registry.Remove(
                    hostPlayerId);

                DestroyPlayer(
                    createdRemotePlayer);

                FailConfiguration(
                    "The remote projectile visual spawner " +
                    "could not be configured.");

                return;
            }

            else if (!TryConfigureRemoteShotgunSpawner(
             createdRemotePlayer,
             remoteProjectileVisualPrefab))
            {
                registry.Remove(
                    localPlayerId);

                registry.Remove(
                    hostPlayerId);

                DestroyPlayer(
                    createdRemotePlayer);

                FailConfiguration(
                    "The remote shotgun visual spawner " +
                    "could not be configured.");

                return;
            }

            localPlayer = scenePlayer;
            remotePlayer = createdRemotePlayer;
            RegisterEntity(scenePlayer);
            RegisterEntity(createdRemotePlayer);
        }

        private static bool TryResolveHostPlayerIds(
            uint localPlayerId,
            RoomStateSnapshot roomState,
            out uint remotePlayerId)
        {
            remotePlayerId = 0u;

            if (localPlayerId == 0u ||
                roomState == null ||
                roomState.Players.Count != 2)
            {
                return false;
            }

            bool localPlayerIsHost = false;

            foreach (RoomPlayerSnapshot player
                in roomState.Players)
            {
                if (player.PlayerId == localPlayerId)
                {
                    localPlayerIsHost =
                        player.IsHost;
                }
                else
                {
                    remotePlayerId =
                        player.PlayerId;
                }
            }

            return localPlayerIsHost &&
                remotePlayerId != 0u;
        }

        private static bool TryResolveClientPlayerIds(
            uint localPlayerId,
            RoomStateSnapshot roomState,
            out uint hostPlayerId)
        {
            hostPlayerId = 0u;

            if (localPlayerId == 0u ||
                roomState == null ||
                roomState.Players.Count != 2)
            {
                return false;
            }

            bool localPlayerWasFound = false;

            foreach (RoomPlayerSnapshot player
                in roomState.Players)
            {
                if (player.PlayerId == localPlayerId)
                {
                    if (player.IsHost)
                    {
                        return false;
                    }

                    localPlayerWasFound = true;
                }
                else if (player.IsHost)
                {
                    hostPlayerId =
                        player.PlayerId;
                }
            }

            return localPlayerWasFound &&
                hostPlayerId != 0u;
        }

        private void BindCameraToLocalPlayer()
        {
            if (cameraFollow == null)
            {
                FailConfiguration(
                    "CameraFollow is not assigned.");

                return;
            }

            if (localPlayer == null)
            {
                FailConfiguration(
                    "Local player is not available.");

                return;
            }

            cameraFollow.SetTarget(
                localPlayer.transform);
        }

        private void BindHealthBarToLocalPlayer()
        {
            if (healthBarView == null)
            {
                FailConfiguration(
                    "HealthBarView is not assigned.");

                return;
            }

            if (localPlayer == null)
            {
                FailConfiguration(
                    "Local player is not available.");

                return;
            }

            if (!localPlayer.TryGetComponent(
                    out PlayerHealth playerHealth))
            {
                FailConfiguration(
                    "Local player has no PlayerHealth.");

                return;
            }

            healthBarView.Bind(
                playerHealth);
        }

        private void SubscribeToRemoteStateSnapshots(
            NetworkClient client)
        {
            if (client == null ||
                stateSnapshotClient == client)
            {
                return;
            }

            if (stateSnapshotClient != null)
            {
                stateSnapshotClient
                    .PlayerStateSnapshotReceived -=
                    HandleRemotePlayerStateSnapshot;
            }

            stateSnapshotClient =
                client;

            stateSnapshotClient
                .PlayerStateSnapshotReceived +=
                HandleRemotePlayerStateSnapshot;
        }

        private void SubscribeToRemoteShotEvents(
            NetworkClient client)
        {
            if (client == null ||
                shotEventClient == client)
            {
                return;
            }

            if (shotEventClient != null)
            {
                shotEventClient
                    .PlayerShotEventReceived -=
                    HandleRemotePlayerShotEvent;
            }

            shotEventClient =
                client;

            shotEventClient
                .PlayerShotEventReceived +=
                HandleRemotePlayerShotEvent;
        }

        private void SubscribeToRemoteShotgunEvents(
            NetworkClient client)
        {
            if (client == null ||
                shotgunEventClient == client)
            {
                return;
            }

            if (shotgunEventClient != null)
            {
                shotgunEventClient
                    .PlayerShotgunEventReceived -=
                    HandleRemotePlayerShotgunEvent;
            }

            shotgunEventClient =
                client;

            shotgunEventClient
                .PlayerShotgunEventReceived +=
                HandleRemotePlayerShotgunEvent;
        }

        private void SubscribeToRemoteInput(
            NetworkClient client)
        {
            if (client == null ||
                remoteInputClient == client)
            {
                return;
            }

            if (remoteInputClient != null)
            {
                remoteInputClient
                    .RemotePlayerInputReceived -=
                    HandleRemotePlayerInput;
            }

            remoteInputClient =
                client;

            remoteInputClient
                .RemotePlayerInputReceived +=
                HandleRemotePlayerInput;
        }

        private void HandleRemotePlayerInput(
            uint playerId,
            PlayerInputPayload input)
        {
            if (input == null ||
                registry == null ||
                !registry.TryGetPlayer(
                    playerId,
                    out GameObject player) ||
                player == null ||
                player == localPlayer)
            {
                return;
            }

            if (!player.TryGetComponent(
                    out RemotePlayerInputSource
                        inputSource))
            {
                Debug.LogError(
                    "NetworkGameBootstrap: Registered " +
                    "remote player has no " +
                    "RemotePlayerInputSource.",
                    this);

                return;
            }

            inputSource.ApplyInputState(
                new Vector2(
                    input.MoveX,
                    input.MoveY),
                new Vector2(
                    input.AimX,
                    input.AimY),
                input.FireHeld,
                input.DashRequestSequence,
                input.ShotgunRequestSequence);
        }

        private void HandleRemotePlayerStateSnapshot(
            uint senderPlayerId,
            PlayerStateSnapshotPayload snapshot)
        {
            if (senderPlayerId == 0u ||
                snapshot == null)
            {
                return;
            }

            ApplyAuthoritativePlayerHealth(snapshot);

            if (remotePlayer != null &&
                remotePlayer.TryGetComponent(
                out RemotePlayerInterpolator interpolator))
            {
                interpolator.ApplySnapshot(
                    snapshot);
            }

            if (localPlayer != null &&
                localPlayer.TryGetComponent(
                    out LocalPlayerDashReconciler reconciler))
            {
                reconciler.ApplySnapshot(
                    snapshot);
            }

            PlayerStateSnapshotReceived?.Invoke(
                senderPlayerId,
                snapshot);
        }

        private void EnsureSharedExperienceState()
        {
            if (GetComponent<SharedExperienceState>() == null)
            {
                gameObject.AddComponent<SharedExperienceState>();
            }
        }

        private void RegisterEntity(GameObject player)
        {
            if (entityRegistry != null && player != null &&
                player.TryGetComponent(out NetworkEntityId identifier))
            {
                entityRegistry.TryRegister(identifier);
            }
        }

        private void TryConfigureNetworkUpgradeCoordinator()
        {
            UpgradeManager upgradeManager =
                FindObjectOfType<UpgradeManager>();
            GameManager gameManager =
                FindObjectOfType<GameManager>();

            if (upgradeManager == null || gameManager == null)
            {
                Debug.LogWarning(
                    "NetworkGameBootstrap: upgrade references are missing.",
                    this);
                return;
            }

            networkUpgradeCoordinator =
                GetComponent<NetworkUpgradeCoordinator>();
            if (networkUpgradeCoordinator == null)
            {
                networkUpgradeCoordinator =
                    gameObject.AddComponent<NetworkUpgradeCoordinator>();
            }

            networkUpgradeCoordinator.Configure(
                upgradeManager,
                gameManager);
        }

        private void SubscribeToHostUpgradeLevel()
        {
            if (!GameSession.IsHost ||
                networkUpgradeCoordinator == null ||
                localPlayer == null ||
                !localPlayer.TryGetComponent(
                    out hostUpgradeLevelSystem))
            {
                return;
            }

            hostUpgradeLevelSystem.OnLevelUp +=
                HandleHostUpgradeLevelUp;

            networkUpgradeCoordinator.UpgradeStarted +=
                HandleHostUpgradeStarted;
            networkUpgradeCoordinator.UpgradeApplied +=
                HandleHostUpgradeApplied;
            networkUpgradeCoordinator.UpgradeCompleted +=
                HandleHostUpgradeCompleted;
            NetworkClient hostClient =
                NetworkClientBehaviour.Instance.Client;
            hostClient.UpgradeChoiceSubmittedReceived +=
                HandleHostUpgradeChoiceSubmitted;
        }

        private void HandleHostUpgradeLevelUp(int newLevel)
        {
            if (networkUpgradeCoordinator == null ||
                networkUpgradeCoordinator.State != NetworkUpgradeState.Idle)
            {
                return;
            }

            networkUpgradeCoordinator.BeginHostUpgrade(
                unchecked((uint)newLevel));
        }

        private void HandleHostUpgradeStarted(
            uint sequence,
            IReadOnlyList<UpgradeData> options)
        {
            NetworkClientBehaviour networkBehaviour =
                NetworkClientBehaviour.Instance;

            if (networkBehaviour == null ||
                networkBehaviour.Client == null ||
                options == null)
            {
                return;
            }

            var upgradeIds = new List<ushort>(options.Count);
            foreach (UpgradeData option in options)
            {
                if (option == null || option.UpgradeId == 0)
                {
                    return;
                }

                upgradeIds.Add(option.UpgradeId);
            }

            networkBehaviour.Client.SendUpgradeStarted(
                new UpgradeStartedPayload(sequence, upgradeIds));

            if (GameSession.IsHost &&
                networkUpgradeCoordinator != null)
            {
                networkUpgradeCoordinator.UpgradeManager
                    .PresentNetworkOptions(
                        networkUpgradeCoordinator.CurrentOptions,
                        HandleHostUpgradeSelected);
            }
        }

        private void HandleHostUpgradeSelected(
            UpgradeData upgradeData)
        {
            NetworkClientBehaviour networkBehaviour =
                NetworkClientBehaviour.Instance;

            if (upgradeData == null ||
                networkBehaviour == null ||
                networkBehaviour.Client == null ||
                networkUpgradeCoordinator == null)
            {
                return;
            }

            bool submitted = networkUpgradeCoordinator.TrySubmitChoice(
                networkBehaviour.Client.PlayerId,
                upgradeData.UpgradeId);
            if (!submitted)
            {
                return;
            }

            networkUpgradeCoordinator.UpgradeManager
                .SetNetworkWaiting(true);

            if (networkUpgradeCoordinator.AllChoicesSubmitted)
            {
                networkUpgradeCoordinator.CompleteHostUpgrade();
            }
        }

        private void HandleHostUpgradeChoiceSubmitted(
            uint senderPlayerId,
            UpgradeChoicePayload payload)
        {
            if (networkUpgradeCoordinator == null || payload == null ||
                payload.Sequence != networkUpgradeCoordinator.CurrentSequence)
            {
                return;
            }

            if (networkUpgradeCoordinator.TrySubmitChoice(
                    senderPlayerId,
                    payload.UpgradeId) &&
                networkUpgradeCoordinator.AllChoicesSubmitted)
            {
                networkUpgradeCoordinator.CompleteHostUpgrade();
            }
        }

        private void HandleHostUpgradeApplied(
            uint playerId,
            UpgradeData upgradeData)
        {
            if (playerId == 0u ||
                upgradeData == null ||
                networkUpgradeCoordinator == null)
            {
                return;
            }

            UpgradeManager upgradeManager =
                networkUpgradeCoordinator.UpgradeManager;

            if (registry != null &&
                registry.TryGetPlayer(
                    playerId,
                    out GameObject targetPlayer) &&
                targetPlayer != null)
            {
                upgradeManager.ApplyUpgradeToPlayer(
                    targetPlayer,
                    upgradeData);
                return;
            }

            NetworkClient client =
                NetworkClientBehaviour.Instance?.Client;
            if (client != null &&
                client.PlayerId == playerId)
            {
                upgradeManager.ApplyUpgrade(upgradeData);
            }
        }

        private void HandleHostUpgradeCompleted(
            uint sequence,
            IReadOnlyDictionary<uint, ushort> choices)
        {
            NetworkClient client = NetworkClientBehaviour.Instance?.Client;
            if (client != null)
            {
                client.SendUpgradeCompleted(
                    new UpgradeCompletedPayload(sequence, choices));
            }

            networkUpgradeCoordinator.UpgradeManager
                .SetNetworkWaiting(false);
            FindObjectOfType<UpgradePanelView>()?.Hide();
        }

        private void SubscribeToClientUpgradeMessages(
            NetworkClient client)
        {
            if (!GameSession.IsClient || client == null)
            {
                return;
            }

            upgradeMessageClient = client;
            upgradeMessageClient.UpgradeStartedReceived +=
                HandleClientUpgradeStarted;
            upgradeMessageClient.UpgradeCompletedReceived +=
                HandleClientUpgradeCompleted;
        }

        private void HandleClientUpgradeStarted(
            UpgradeStartedPayload payload)
        {
            if (networkUpgradeCoordinator == null ||
                payload == null ||
                !networkUpgradeCoordinator.ApplyRemoteUpgradeStart(payload))
            {
                return;
            }

            UpgradeManager upgradeManager =
                networkUpgradeCoordinator.UpgradeManager;
            upgradeManager.PresentNetworkOptions(
                networkUpgradeCoordinator.CurrentOptions,
                HandleClientUpgradeSelected);
        }

        private void HandleClientUpgradeSelected(
            UpgradeData upgradeData)
        {
            if (upgradeData == null ||
                networkUpgradeCoordinator == null ||
                upgradeMessageClient == null ||
                networkUpgradeCoordinator.State !=
                    NetworkUpgradeState.WaitingForChoices)
            {
                return;
            }

            upgradeMessageClient.SendUpgradeChoiceSubmitted(
                new UpgradeChoicePayload(
                    networkUpgradeCoordinator.CurrentSequence,
                    upgradeData.UpgradeId));
            networkUpgradeCoordinator.UpgradeManager
                .SetNetworkWaiting(true);
        }

        private void HandleClientUpgradeCompleted(
            UpgradeCompletedPayload payload)
        {
            if (networkUpgradeCoordinator == null ||
                upgradeMessageClient == null || payload == null)
            {
                return;
            }

            if (networkUpgradeCoordinator.ApplyRemoteUpgradeCompletion(
                    upgradeMessageClient.PlayerId,
                    payload))
            {
                networkUpgradeCoordinator.UpgradeManager
                    .SetNetworkWaiting(false);
                UpgradePanelView panel =
                    FindObjectOfType<UpgradePanelView>();
                panel?.Hide();
            }
        }

        private void ConfigureClientWorldSnapshotConsumer(
            NetworkClient client,
            uint hostPlayerId)
        {
            clientWorldSnapshotConsumer =
                GetComponent<ClientWorldSnapshotConsumer>();
            if (clientWorldSnapshotConsumer == null)
            {
                clientWorldSnapshotConsumer =
                    gameObject.AddComponent<ClientWorldSnapshotConsumer>();
            }

            ExperienceOrbPool pool =
                FindObjectOfType<ExperienceOrbPool>();
            EnemySpawner enemySpawner =
                FindObjectOfType<EnemySpawner>();
            BossEncounterController bossEncounter =
                FindObjectOfType<BossEncounterController>();

            if (pool == null || bossEncounter == null || enemySpawner == null)
            {
                return;
            }

            clientWorldSnapshotConsumer.ConfigureAuthoritativeHost(hostPlayerId);
                clientWorldSnapshotConsumer.ConfigureEntityRegistry(entityRegistry);
            clientWorldSnapshotConsumer.ConfigureEntityFactory(
                record => record.EntityType == NetworkEntityType.Boss
                    ? bossEncounter.CreateClientBoss(record)
                    : record.EntityType == NetworkEntityType.Enemy
                        ? enemySpawner.CreateClientEnemy(record)
                        : pool.CreateClientOrb(record));
            clientWorldSnapshotConsumer.ConfigureEntityRemover(
                entity =>
                {
                    if (entity != null)
                    {
                        if (entity.TryGetComponent(
                                out BossHealth bossHealth))
                        {
                            FindObjectOfType<BossHealthView>()?.Hide();
                        }

                        Destroy(entity);
                    }
                });
            client.WorldStateSnapshotReceived +=
                (sender, sequence, snapshot) =>
                    clientWorldSnapshotConsumer.EnqueueSnapshot(
                        sender, sequence, snapshot);
            client.WorldEntitySpawnedReceived +=
                record => clientWorldSnapshotConsumer.EnqueueSpawn(record);
            client.WorldEntityRemovedReceived +=
                removed =>
                {
                    bool removedSuccessfully =
                        clientWorldSnapshotConsumer.TryRemoveEntity(removed);
                    Debug.Log(
                        $"NetworkGameBootstrap: applied WorldEntityRemoved " +
                        $"entity={removed.EntityId} type={removed.EntityType} " +
                        $"success={removedSuccessfully}");
                };
        }

        private void HandleSharedExperienceSnapshot(
            SharedExperienceSnapshotPayload snapshot)
        {
            if (!GameSession.IsClient || snapshot == null ||
                (lastSharedExperienceSequence != 0u &&
                 snapshot.Sequence <= lastSharedExperienceSequence))
            {
                return;
            }

            lastSharedExperienceSequence = snapshot.Sequence;
            SharedExperienceState state =
                GetComponent<SharedExperienceState>();
            if (state != null)
            {
                state.ApplyAuthoritativeState(
                    snapshot.CurrentLevel,
                    snapshot.CurrentExperience,
                    snapshot.ExperienceToNextLevel);
            }

            if (GameSession.IsHost &&
                localPlayer != null &&
                localPlayer.TryGetComponent(
                    out LevelSystem hostLevelSystem))
            {
                hostLevelSystem.ApplyAuthoritativeState(
                    snapshot.CurrentLevel,
                    snapshot.CurrentExperience,
                    snapshot.ExperienceToNextLevel);
            }

            if (localPlayer != null &&
                localPlayer.TryGetComponent(
                    out LevelSystem levelSystem))
            {
                levelSystem.ApplyAuthoritativeState(
                    snapshot.CurrentLevel,
                    snapshot.CurrentExperience,
                    snapshot.ExperienceToNextLevel);
            }
        }

        private void ConfigureHostSharedExperiencePublisher(
            Action<SharedExperienceSnapshotPayload> send)
        {
            SharedExperienceState shared =
                GetComponent<SharedExperienceState>();
            HostSharedExperiencePublisher publisher =
                GetComponent<HostSharedExperiencePublisher>();
            if (publisher == null)
            {
                publisher = gameObject.AddComponent<HostSharedExperiencePublisher>();
            }
            publisher.Configure(shared, send);
            shared.StateChanged += ApplyHostSharedExperienceToLocalPlayer;
        }

        private void ApplyHostSharedExperienceToLocalPlayer(
            int level,
            int experience,
            int experienceToNext)
        {
            if (!GameSession.IsHost || localPlayer == null ||
                !localPlayer.TryGetComponent(
                    out LevelSystem levelSystem))
            {
                return;
            }

            levelSystem.ApplyAuthoritativeState(
                level,
                experience,
                experienceToNext);
        }

        private void ApplyAuthoritativePlayerHealth(
            PlayerStateSnapshotPayload snapshot)
        {
            if (!GameSession.IsClient ||
                registry == null ||
                snapshot == null)
            {
                return;
            }

            for (int index = 0;
                index < snapshot.Players.Count;
                index++)
            {
                PlayerStateRecord state =
                    snapshot.Players[index];

                if (state == null ||
                    !registry.TryGetPlayer(
                        state.PlayerId,
                        out GameObject player) ||
                    player == null ||
                    !player.TryGetComponent(
                        out PlayerHealth playerHealth))
                {
                    continue;
                }

                playerHealth.ApplyAuthoritativeState(
                    state.CurrentHealth,
                    state.MaxHealth);
            }
        }

        private void HandleRemotePlayerShotEvent(
            uint playerId,
            PlayerShotEvent shotEvent)
        {
            Debug.Log(
                $"NetworkGameBootstrap: received remote shot " +
                $"player={playerId}, " +
                $"sequence={shotEvent?.ShotSequence}",
                this);

            if (playerId == 0u ||
                shotEvent == null ||
                registry == null ||
                !registry.TryGetPlayer(
                    playerId,
                    out GameObject player) ||
                player == null ||
                player == localPlayer)
            {
                return;
            }

            if (!player.TryGetComponent(
                    out RemotePlayerShotEventReceiver receiver))
            {
                Debug.LogError(
                    "NetworkGameBootstrap: Registered " +
                    "remote player has no " +
                    "RemotePlayerShotEventReceiver.",
                    this);

                return;
            }

            receiver.Enqueue(
                playerId,
                shotEvent);
        }

        private void HandleRemotePlayerShotgunEvent(
            uint senderPlayerId,
            PlayerShotgunEvent shotgunEvent)
        {
            if (senderPlayerId == 0u ||
                shotgunEvent == null ||
                registry == null)
            {
                return;
            }

            uint targetPlayerId =
                shotgunEvent.PlayerId;

            if (targetPlayerId == 0u ||
                !registry.TryGetPlayer(
                    targetPlayerId,
                    out GameObject targetPlayer) ||
                targetPlayer == null)
            {
                return;
            }

            Debug.Log(
                $"NetworkGameBootstrap: routed remote " +
                $"shotgun event sender={senderPlayerId}, " +
                $"player={targetPlayerId}, " +
                $"sequence={shotgunEvent.VolleySequence}",
                this);

            if(!targetPlayer.TryGetComponent(
                out RemotePlayerShotgunEventReceiver receiver))
{
                if (!TryConfigureRemoteShotgunReceiver(
                        targetPlayer,
                        targetPlayerId))
                {
                    Debug.LogError(
                        "NetworkGameBootstrap: Player could not " +
                        "be configured with a shotgun receiver.",
                        this);

                    return;
                }

                receiver =
                    targetPlayer.GetComponent<
                        RemotePlayerShotgunEventReceiver>();
            }

            if (!targetPlayer.TryGetComponent(
                out RemoteShotgunVisualSpawner visualSpawner))
            {
                if (remoteProjectileVisualPrefab == null ||
                    !TryConfigureRemoteShotgunSpawner(
                        targetPlayer,
                        remoteProjectileVisualPrefab))
                {
                    Debug.LogError(
                        "NetworkGameBootstrap: Player could not " +
                        "be configured with a shotgun visual spawner.",
                        this);

                    return;
                }
            }

            receiver.Enqueue(
                senderPlayerId,
                shotgunEvent);

            PlayerShotgunEventReceived?.Invoke(
                targetPlayerId,
                shotgunEvent);
        }

        private static bool TryConfigureLocalDashReconciler(
            GameObject player,
            uint localPlayerId)
        {
            if (player == null ||
                localPlayerId == 0u ||
                !player.TryGetComponent(
                    out PlayerController controller))
            {
                return false;
            }

            LocalPlayerDashReconciler reconciler =
                player.GetComponent<
                    LocalPlayerDashReconciler>();

            if (reconciler == null)
            {
                reconciler =
                    player.AddComponent<
                        LocalPlayerDashReconciler>();
            }

            reconciler.Configure(
                localPlayerId);

            return reconciler.enabled &&
                controller != null;
        }

        private static bool TryConfigureRemoteInterpolator(
            GameObject player,
            uint remotePlayerId)
        {
            if (player == null ||
                remotePlayerId == 0u)
            {
                return false;
            }

            RemotePlayerInterpolator interpolator =
                player.GetComponent<
                    RemotePlayerInterpolator>();

            if (interpolator == null)
            {
                interpolator =
                    player.AddComponent<
                        RemotePlayerInterpolator>();
            }

            interpolator.Configure(
                remotePlayerId);

            return true;
        }

        private static bool TryConfigureRemoteShotReceiver(
            GameObject player,
            uint remotePlayerId)
        {
            if (player == null ||
                remotePlayerId == 0u)
            {
                return false;
            }

            RemotePlayerShotEventReceiver receiver =
                player.GetComponent<
                    RemotePlayerShotEventReceiver>();

            if (receiver == null)
            {
                receiver =
                    player.AddComponent<
                        RemotePlayerShotEventReceiver>();
            }

            receiver.Configure(
                remotePlayerId);

            return true;
        }

        private bool TryConfigureRemoteShotgunReceiver(
            GameObject player,
            uint remotePlayerId)
        {
            if (player == null ||
                remotePlayerId == 0u)
            {
                return false;
            }

            RemotePlayerShotgunEventReceiver receiver =
                player.GetComponent<
                    RemotePlayerShotgunEventReceiver>();

            if (receiver == null)
            {
                receiver =
                    player.AddComponent<
                        RemotePlayerShotgunEventReceiver>();
            }

            receiver.Configure(
                remotePlayerId);

            return true;
        }

        private static bool TryConfigureRemoteShotSpawner(
            GameObject player,
            GameObject visualPrefab)
        {
            if (player == null ||
                visualPrefab == null)
            {
                return false;
            }

            RemotePlayerShotEventReceiver receiver =
                player.GetComponent<
                    RemotePlayerShotEventReceiver>();

            if (receiver == null)
            {
                return false;
            }

            RemoteProjectileVisualSpawner spawner =
                player.GetComponent<
                    RemoteProjectileVisualSpawner>();

            if (spawner == null)
            {
                spawner =
                    player.AddComponent<
                        RemoteProjectileVisualSpawner>();
            }

            spawner.Configure(
                receiver,
                visualPrefab);

            return true;
        }

        private static bool
            TryConfigureRemoteShotgunSpawner(
                GameObject player,
                GameObject visualPrefab)
        {
            if (player == null ||
                visualPrefab == null)
            {
                return false;
            }

            RemotePlayerShotgunEventReceiver receiver =
                player.GetComponent<
                    RemotePlayerShotgunEventReceiver>();

            if (receiver == null)
            {
                return false;
            }

            RemoteShotgunVisualSpawner spawner =
                player.GetComponent<
                    RemoteShotgunVisualSpawner>();

            if (spawner == null)
            {
                spawner =
                    player.AddComponent<
                        RemoteShotgunVisualSpawner>();
            }

            spawner.Configure(
                receiver,
                visualPrefab);

            return true;
        }

        private bool TryConfigureHostStatePublisher(
            uint localPlayerId,
            uint remotePlayerId,
            Action<PlayerStateSnapshotPayload>
                sendSnapshot)
        {
            if (!GameSession.IsHost ||
                registry == null ||
                localPlayerId == 0u ||
                remotePlayerId == 0u ||
                localPlayerId == remotePlayerId ||
                sendSnapshot == null)
            {
                return false;
            }

            HostPlayerStatePublisher publisher =
                GetComponent<HostPlayerStatePublisher>();

            if (publisher == null)
            {
                publisher =
                    gameObject.AddComponent<
                        HostPlayerStatePublisher>();
            }

            publisher.Configure(
                registry,
                new uint[]
                {
            localPlayerId,
            remotePlayerId
                },
                sendSnapshot);

            return true;
        }

        private static bool EnsurePlayerNetworkEntityId(
            GameObject player,
            uint playerId)
        {
            if (player == null ||
                playerId == 0u)
            {
                return false;
            }

            NetworkEntityId identifier =
                player.GetComponent<NetworkEntityId>();

            if (identifier == null)
            {
                identifier =
                    player.AddComponent<NetworkEntityId>();
            }

            if (identifier.IsAssigned)
            {
                return identifier.EntityId == playerId &&
                    identifier.EntityType ==
                        NetworkEntityType.Player;
            }

            return identifier.TryAssign(
                playerId,
                NetworkEntityType.Player);
        }

        private bool TryConfigureHostWorldSnapshotPublisher(
            Action<WorldStateSnapshotPayload>
                sendSnapshot)
        {
            if (!GameSession.IsHost ||
                registry == null ||
                sendSnapshot == null)
            {
                return false;
            }

            EnemySpawner enemySpawner =
                FindObjectOfType<EnemySpawner>();

            BossEncounterController bossEncounterController =
                FindObjectOfType<BossEncounterController>();

            if (enemySpawner == null ||
                bossEncounterController == null)
            {
                return false;
            }

            HostWorldSnapshotPublisher publisher =
                GetComponent<HostWorldSnapshotPublisher>();

            if (publisher == null)
            {
                publisher =
                    gameObject.AddComponent<
                        HostWorldSnapshotPublisher>();
            }

            publisher.ConfigureWorldSources(
                registry,
                enemySpawner,
                bossEncounterController);

            publisher.ConfigureSnapshotSender(
                sendSnapshot);

            return true;
        }

        private bool TryConfigureHostEnemySpawnPublisher(
            Action<WorldEntityRecord> sendSpawn)
        {
            if (!GameSession.IsHost ||
                sendSpawn == null)
            {
                return false;
            }

            EnemySpawner enemySpawner =
                FindObjectOfType<EnemySpawner>();

            if (enemySpawner == null)
            {
                return false;
            }

            HostEnemySpawnPublisher publisher =
                GetComponent<HostEnemySpawnPublisher>();

            if (publisher == null)
            {
                publisher =
                    gameObject.AddComponent<
                        HostEnemySpawnPublisher>();
            }

            publisher.Configure(
                enemySpawner,
                sendSpawn);

            return true;
        }

        private bool TryConfigureHostExperienceOrbSpawnPublisher(
            Action<WorldEntityRecord> sendSpawn)
        {
            if (!GameSession.IsHost ||
                sendSpawn == null)
            {
                return false;
            }

            ExperienceOrbPool orbPool =
                FindObjectOfType<ExperienceOrbPool>();

            if (orbPool == null)
            {
                return false;
            }

            HostExperienceOrbSpawnPublisher publisher =
                GetComponent<HostExperienceOrbSpawnPublisher>();

            if (publisher == null)
            {
                publisher =
                    gameObject.AddComponent<
                        HostExperienceOrbSpawnPublisher>();
            }

            publisher.Configure(
                orbPool,
                sendSpawn);

            return true;
        }

        private void TryConfigureNetworkBossCoordinator(
            NetworkClient client)
        {
            GameManager gameManager = FindObjectOfType<GameManager>();
            if (gameManager == null || client == null)
                return;

            networkBossCoordinator =
                GetComponent<NetworkBossCoordinator>();
            if (networkBossCoordinator == null)
                networkBossCoordinator =
                    gameObject.AddComponent<NetworkBossCoordinator>();

            networkBossCoordinator.Configure(gameManager);
            bossCombatStateClient = client;

            if (GameSession.IsHost)
            {
                networkBossCoordinator.StateBroadcastRequested +=
                    HandleHostBossCombatStateBroadcast;
            }
            else
            {
                client.BossCombatStateReceived +=
                    HandleClientBossCombatState;
            }
        }

        private void HandleHostBossCombatStateBroadcast(
            NetworkBossCombatState state)
        {
            if (bossCombatStateClient == null)
                return;

            bossCombatStateClient.SendBossCombatState(
                new BossCombatStatePayload(
                    (BossCombatState)(byte)state));
        }

        private void HandleClientBossCombatState(
            BossCombatStatePayload payload)
        {
            if (networkBossCoordinator == null || payload == null)
                return;

            networkBossCoordinator.ApplyRemoteState(
                (NetworkBossCombatState)(byte)payload.State);
        }

        private void ConfigureGameResultSync(NetworkClient client)
        {
            gameResultGameManager = FindObjectOfType<GameManager>();
            if (gameResultGameManager == null || client == null)
                return;
            gameResultClient = client;
            if (GameSession.IsHost)
                gameResultGameManager.OnStateChanged += HandleHostResultState;
            else
                client.GameResultReceived += HandleClientGameResult;
        }

        private void HandleHostResultState(GameState state)
        {
            if (gameResultClient == null ||
                (state != GameState.Victory && state != GameState.Defeat))
                return;
            gameResultClient.SendGameResult(
                new GameResultPayload(
                    state == GameState.Victory
                        ? GameResult.Victory
                        : GameResult.Defeat));
        }

        private void HandleClientGameResult(GameResultPayload payload)
        {
            if (gameResultGameManager == null || payload == null)
                return;
            if (payload.Result == GameResult.Victory)
                gameResultGameManager.NotifyVictory();
            else
                gameResultGameManager.NotifyDefeat();
        }

        private void ConfigurePlayerDeathSync(NetworkClient client)
        {
            if (client == null)
                return;
            playerDeathClient = client;
            if (GameSession.IsClient && localPlayer != null &&
                localPlayer.TryGetComponent(out clientLocalPlayerHealth))
            {
                clientLocalPlayerHealth.OnDied += HandleClientPlayerDied;
            }
            else if (GameSession.IsHost)
            {
                client.PlayerDiedReceived += HandleRemotePlayerDied;
                if (remotePlayer != null &&
                    remotePlayer.TryGetComponent(out hostRemotePlayerHealth))
                {
                    hostRemotePlayerHealth.OnDied += HandleRemotePlayerDied;
                }
            }
        }

        private void HandleClientPlayerDied()
        {
            playerDeathClient?.SendPlayerDied();
        }

        private void HandleRemotePlayerDied()
        {
            gameResultGameManager?.NotifyDefeat();
        }

        private bool TryConfigureHostBossSpawnPublisher(
            Action<WorldEntityRecord> sendSpawn)
        {
            if (!GameSession.IsHost || sendSpawn == null)
            {
                return false;
            }

            BossEncounterController encounter =
                FindObjectOfType<BossEncounterController>();

            if (encounter == null)
            {
                return false;
            }

            HostBossSpawnPublisher publisher =
                GetComponent<HostBossSpawnPublisher>();

            if (publisher == null)
            {
                publisher =
                    gameObject.AddComponent<HostBossSpawnPublisher>();
            }

            publisher.Configure(encounter, sendSpawn);
            return true;
        }

        private bool TryConfigureHostEnemyDeathPublisher(
            Action<WorldEntityRemovedPayload> sendRemoval)
        {
            if (!GameSession.IsHost || sendRemoval == null)
            {
                return false;
            }

            EnemySpawner enemySpawner =
                FindObjectOfType<EnemySpawner>();

            if (enemySpawner == null)
            {
                return false;
            }

            HostEnemyDeathPublisher publisher =
                GetComponent<HostEnemyDeathPublisher>();

            if (publisher == null)
            {
                publisher = gameObject.AddComponent<
                    HostEnemyDeathPublisher>();
            }

            publisher.Configure(enemySpawner, sendRemoval);
            return true;
        }

        private bool TryConfigureHostExperienceOrbCollectionPublisher(
            Action<WorldEntityRemovedPayload> sendRemoval)
        {
            if (!GameSession.IsHost || sendRemoval == null)
            {
                return false;
            }

            ExperienceOrbPool orbPool =
                FindObjectOfType<ExperienceOrbPool>();

            if (orbPool == null)
            {
                return false;
            }

            HostExperienceOrbCollectionPublisher publisher =
                GetComponent<HostExperienceOrbCollectionPublisher>();

            if (publisher == null)
            {
                publisher =
                    gameObject.AddComponent<
                        HostExperienceOrbCollectionPublisher>();
            }

            publisher.Configure(orbPool, sendRemoval);
            return true;
        }

        private bool TryConfigureHostBossDeathPublisher(
            Action<WorldEntityRemovedPayload> sendRemoval)
        {
            if (!GameSession.IsHost || sendRemoval == null)
            {
                return false;
            }

            BossEncounterController encounter =
                FindObjectOfType<BossEncounterController>();

            if (encounter == null)
            {
                return false;
            }

            HostBossDeathPublisher publisher =
                GetComponent<HostBossDeathPublisher>();

            if (publisher == null)
            {
                publisher =
                    gameObject.AddComponent<HostBossDeathPublisher>();
            }

            publisher.Configure(encounter, sendRemoval);
            return true;
        }

        private bool TryConfigureHostShotPublisher(
            uint localPlayerId,
            Action<PlayerShotEvent>
                sendShotEvent)
        {
            if (!GameSession.IsHost ||
                localPlayer == null ||
                localPlayerId == 0u ||
                sendShotEvent == null)
            {
                return false;
            }

            PlayerShooterShotEventSource
                shotEventSource =
                localPlayer.GetComponent<
                    PlayerShooterShotEventSource>();

            if (shotEventSource == null)
            {
                shotEventSource =
                    localPlayer.AddComponent<
                        PlayerShooterShotEventSource>();
            }

            shotEventSource.Configure(
                localPlayerId);

            HostPlayerShotPublisher publisher =
                GetComponent<
                    HostPlayerShotPublisher>();

            if (publisher == null)
            {
                publisher =
                    gameObject.AddComponent<
                        HostPlayerShotPublisher>();
            }

            publisher.Configure(
                shotEventSource,
                sendShotEvent);

            return true;
        }

        private bool TryConfigureHostShotgunPublishers(
            uint localPlayerId,
            uint remotePlayerId,
            Action<PlayerShotgunEvent>
        sendShotgunEvent)
        {
            if (!GameSession.IsHost ||
                localPlayer == null ||
                remotePlayer == null ||
                localPlayerId == 0u ||
                remotePlayerId == 0u ||
                localPlayerId == remotePlayerId ||
                sendShotgunEvent == null)
            {
                return false;
            }

            if (!TryConfigurePlayerShotgunPublisher(
                    localPlayer,
                    localPlayerId,
                    sendShotgunEvent))
            {
                return false;
            }

            if (!TryConfigurePlayerShotgunPublisher(
                    remotePlayer,
                    remotePlayerId,
                    sendShotgunEvent))
            {
                return false;
            }

            return true;
        }

        private static bool
    TryConfigurePlayerShotgunPublisher(
        GameObject player,
        uint playerId,
        Action<PlayerShotgunEvent>
            sendShotgunEvent)
        {
            if (player == null ||
                playerId == 0u ||
                sendShotgunEvent == null)
            {
                return false;
            }

            ShotgunSkill shotgunSkill =
                player.GetComponent<
                    ShotgunSkill>();

            if (shotgunSkill == null)
            {
                return false;
            }

            PlayerShotgunEventSource eventSource =
                player.GetComponent<
                    PlayerShotgunEventSource>();

            if (eventSource == null)
            {
                eventSource =
                    player.AddComponent<
                        PlayerShotgunEventSource>();
            }

            eventSource.Configure(
                playerId);

            HostPlayerShotgunPublisher publisher =
                player.GetComponent<
                    HostPlayerShotgunPublisher>();

            if (publisher == null)
            {
                publisher =
                    player.AddComponent<
                        HostPlayerShotgunPublisher>();
            }

            publisher.Configure(
                eventSource,
                sendShotgunEvent);

            shotgunSkill.SetShotgunEventSource(
                eventSource);

            return true;
        }

        private static bool
            TryConfigureClientInputPublisher(
                GameObject player,
                Action<PlayerInputPayload> sendInput)
        {
            return TryConfigureClientInputPublisherWithStateGuard(
                player,
                sendInput,
                () => true);
        }

        private static bool
            TryConfigureClientInputPublisherWithStateGuard(
                GameObject player,
                Action<PlayerInputPayload> sendInput,
                Func<bool> canSendInput)
        {
            if (player == null ||
                sendInput == null ||
                canSendInput == null)
            {
                return false;
            }

            if (!player.TryGetComponent(
                    out LocalPlayerInputSource
                        inputSource))
            {
                return false;
            }

            ClientPlayerInputPublisher publisher =
                player.GetComponent<
                    ClientPlayerInputPublisher>();

            if (publisher == null)
            {
                publisher =
                    player.AddComponent<
                        ClientPlayerInputPublisher>();
            }

            publisher.ConfigureWithStateGuard(
                inputSource,
                sendInput,
                canSendInput);

            return true;
        }

        private static bool TryEnableRemoteSimulation(
            GameObject player)
        {
            if (player == null)
            {
                return false;
            }

            RemotePlayerInputSource inputSource =
                player.GetComponent<
                    RemotePlayerInputSource>();

            if (inputSource == null)
            {
                inputSource =
                    player.AddComponent<
                        RemotePlayerInputSource>();
            }

            if (!player.TryGetComponent(
                out PlayerController controller) ||
            !player.TryGetComponent(
                out PlayerShooter shooter))
            {
                return false;
            }

            DashSkill dashSkill =
                player.GetComponent<DashSkill>();

            ShotgunSkill shotgunSkill =
                player.GetComponent<ShotgunSkill>();

            controller.SetInputSource(
                inputSource);

            shooter.SetInputSource(
                inputSource);

            if (dashSkill != null)
            {
                dashSkill.SetInputSource(
                    inputSource);
            }

            if (shotgunSkill != null)
            {
                shotgunSkill.SetInputSource(
                    inputSource);
            }

            return true;
        }

        private static void DisableLocalControl(
            GameObject player)
        {
            DisableComponent<LocalPlayerInputSource>(
                player);

            DisableComponent<PlayerController>(
                player);

            DisableComponent<PlayerShooter>(
                player);

            DisableComponent<DashSkill>(
                player);

            DisableComponent<ShotgunSkill>(
                player);

            if (player.TryGetComponent(
                    out Rigidbody2D body))
            {
                body.velocity = Vector2.zero;
            }
        }

        private static void DisableRemotePlayerHealthBars(
            GameObject remotePlayer)
        {
            if (remotePlayer == null)
            {
                return;
            }

            HealthBarView[] healthBars =
                remotePlayer.GetComponentsInChildren<HealthBarView>(
                    true);

            foreach (HealthBarView healthBar in healthBars)
            {
                healthBar.enabled = false;
            }
        }

        private static void DisableComponent<T>(
            GameObject player)
            where T : Behaviour
        {
            T component =
                player.GetComponent<T>();

            if (component != null)
            {
                component.enabled = false;
            }
        }

        private void FailConfiguration(
            string message)
        {
            Debug.LogError(
                "NetworkGameBootstrap: " +
                message,
                this);

            enabled = false;
        }

        private static void DestroyPlayer(
            GameObject player)
        {
            if (player == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(player);
            }
            else
            {
                DestroyImmediate(player);
            }
        }

        private void OnDestroy()
        {
            if (hostUpgradeLevelSystem != null)
            {
                hostUpgradeLevelSystem.OnLevelUp -=
                    HandleHostUpgradeLevelUp;
                hostUpgradeLevelSystem = null;
            }

            if (networkUpgradeCoordinator != null)
            {
                networkUpgradeCoordinator.UpgradeStarted -=
                    HandleHostUpgradeStarted;
            }

            if (upgradeMessageClient != null)
            {
                upgradeMessageClient.UpgradeStartedReceived -=
                    HandleClientUpgradeStarted;
                upgradeMessageClient.UpgradeCompletedReceived -=
                    HandleClientUpgradeCompleted;
                upgradeMessageClient = null;
            }

            if (networkUpgradeCoordinator != null)
            {
                networkUpgradeCoordinator.UpgradeStarted -=
                    HandleHostUpgradeStarted;
                networkUpgradeCoordinator.UpgradeApplied -=
                    HandleHostUpgradeApplied;
                networkUpgradeCoordinator.UpgradeCompleted -=
                    HandleHostUpgradeCompleted;
            }

            if (networkBossCoordinator != null)
            {
                networkBossCoordinator.StateBroadcastRequested -=
                    HandleHostBossCombatStateBroadcast;
            }

            if (bossCombatStateClient != null)
            {
                bossCombatStateClient.BossCombatStateReceived -=
                    HandleClientBossCombatState;
                bossCombatStateClient = null;
            }

            if (gameResultGameManager != null)
            {
                gameResultGameManager.OnStateChanged -=
                    HandleHostResultState;
                gameResultGameManager = null;
            }

            if (gameResultClient != null)
            {
                gameResultClient.GameResultReceived -=
                    HandleClientGameResult;
                gameResultClient = null;
            }

            if (clientLocalPlayerHealth != null)
            {
                clientLocalPlayerHealth.OnDied -= HandleClientPlayerDied;
                clientLocalPlayerHealth = null;
            }


            if (hostRemotePlayerHealth != null)
            {
                hostRemotePlayerHealth.OnDied -= HandleRemotePlayerDied;
                hostRemotePlayerHealth = null;
            }

            if (playerDeathClient != null)
            {
                playerDeathClient.PlayerDiedReceived -= HandleRemotePlayerDied;
                playerDeathClient = null;
            }

            if (stateSnapshotClient != null)
            {
                stateSnapshotClient
                    .PlayerStateSnapshotReceived -=
                    HandleRemotePlayerStateSnapshot;

                stateSnapshotClient =
                    null;
            }

            if (shotEventClient != null)
            {
                shotEventClient
                    .PlayerShotEventReceived -=
                    HandleRemotePlayerShotEvent;

                shotEventClient =
                    null;
            }

            if (shotgunEventClient != null)
            {
                shotgunEventClient
                    .PlayerShotgunEventReceived -=
                    HandleRemotePlayerShotgunEvent;

                shotgunEventClient =
                    null;
            }

            if (remoteInputClient != null)
            {
                remoteInputClient
                    .RemotePlayerInputReceived -=
                    HandleRemotePlayerInput;

                remoteInputClient =
                    null;
            }

            registry?.Clear();

            localPlayer = null;
            remotePlayer = null;
        }
    }
}
