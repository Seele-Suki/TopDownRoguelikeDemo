using System;
using TopDownRoguelike.Gameplay.Bosses;
using TopDownRoguelike.Gameplay.Characters;
using TopDownRoguelike.Gameplay.Enemies;
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
        private GameObject localPlayer;
        private GameObject remotePlayer;
        private NetworkClient remoteInputClient;
        private NetworkClient stateSnapshotClient;
        private NetworkClient shotEventClient;
        private NetworkClient shotgunEventClient;

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
        }

        private void ConfigureMultiplayerHost()
        {
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
                    !TryConfigureHostEnemyDeathPublisher(
                        networkBehaviour.Client
                            .SendWorldEntityRemoved))
                {
                    FailConfiguration(
                        "The host enemy death publisher " +
                        "could not be configured.");
                }

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
                SubscribeToRemoteStateSnapshots(
                    networkBehaviour.Client);

                SubscribeToRemoteShotEvents(
                    networkBehaviour.Client);

                SubscribeToRemoteShotgunEvents(
                    networkBehaviour.Client);
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
