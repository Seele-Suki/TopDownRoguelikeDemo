using System;
using TopDownRoguelike.Gameplay.Characters;
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

            DisableLocalControl(
                createdRemotePlayer);

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

            inputSource.ApplyInput(
                new Vector2(
                    input.MoveX,
                    input.MoveY),
                new Vector2(
                    input.AimX,
                    input.AimY));
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

            PlayerStateSnapshotReceived?.Invoke(
                senderPlayerId,
                snapshot);
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
                    out PlayerController controller))
            {
                return false;
            }

            controller.SetInputSource(
                inputSource);

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
