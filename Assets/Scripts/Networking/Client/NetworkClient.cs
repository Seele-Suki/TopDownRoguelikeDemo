using System;
using System.Text;
using TopDownRoguelike.Infrastructure;
using TopDownRoguelike.Networking.Protocol;
using TopDownRoguelike.Networking.Transport;
using UnityEngine;

namespace TopDownRoguelike.Networking.Client
{
    public sealed class NetworkClient
        : IRoomNetworkClient,
          IDisposable
    {
        private static readonly Encoding StrictUtf8 =
            new UTF8Encoding(
                false,
                true);

        private readonly
            MainThreadMessageQueue<
                NetworkTransportEvent>
            messageQueue;

        private readonly
            MainThreadNetworkEventDispatcher
            dispatcher;

        private readonly TcpClientTransport
            tcpTransport;

        private readonly UdpClientTransport
            udpTransport;

        private string serverAddress =
            string.Empty;

        private int serverPort;

        private byte[] sessionToken;

        private uint nextUdpSequence =
            1u;

        private uint pendingBindSequence;

        private bool hasLastPlayerStateSequence;

        private uint lastPlayerStateSequence;

        private bool disposed;

        public NetworkClient()
        {
            messageQueue =
                new MainThreadMessageQueue<
                    NetworkTransportEvent>();

            dispatcher =
                new MainThreadNetworkEventDispatcher(
                    messageQueue);

            tcpTransport =
                new TcpClientTransport(
                    messageQueue);

            udpTransport =
                new UdpClientTransport(
                    messageQueue);

            dispatcher.EventDispatched +=
                HandleTransportEvent;

            State =
                NetworkClientState.Disconnected;

            LastError =
                string.Empty;
        }

        public event Action<NetworkClientState>
            StateChanged;

        public event Action<RoomStateSnapshot>
            RoomStateChanged;

        public event Action<string>
            ErrorReceived;

        public event Action
            GameStarted;

        public event Action<
            uint,
            PlayerInputPayload>
            RemotePlayerInputReceived;

        public event Action<
            uint,
            PlayerStateSnapshotPayload>
            PlayerStateSnapshotReceived;

        public event Action<
            uint,
            uint,
            WorldStateSnapshotPayload>
            WorldStateSnapshotReceived;

        public event Action<WorldEntityRecord>
            WorldEntitySpawnedReceived;

        public event Action<WorldEntityRemovedPayload>
            WorldEntityRemovedReceived;

        public event Action<
            uint,
            PlayerShotEvent>
            PlayerShotEventReceived;

        public event Action<
            uint,
            PlayerShotgunEvent>
            PlayerShotgunEventReceived;

        public NetworkClientState State
        {
            get;
            private set;
        }

        public uint PlayerId
        {
            get;
            private set;
        }

        public byte[] SessionToken =>
            sessionToken == null
                ? Array.Empty<byte>()
                : (byte[])sessionToken.Clone();

        public string CurrentRoomId
        {
            get;
            private set;
        } = string.Empty;

        public RoomStateSnapshot CurrentRoomState
        {
            get;
            private set;
        }

        public string LastError
        {
            get;
            private set;
        }

        public void Connect(
            string address,
            int port)
        {
            ThrowIfDisposed();

            if (State !=
                NetworkClientState.Disconnected)
            {
                throw new InvalidOperationException(
                    "Network client must be disconnected " +
                    "before connecting.");
            }

            if (string.IsNullOrWhiteSpace(address))
            {
                throw new ArgumentException(
                    "Server address cannot be empty.",
                    nameof(address));
            }

            if (port < 1 ||
                port > 65535)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(port));
            }

            serverAddress =
                address;

            serverPort =
                port;

            LastError =
                string.Empty;

            CurrentRoomId =
                string.Empty;

            CurrentRoomState =
                null;

            nextUdpSequence =
                1u;

            pendingBindSequence =
                0u;

            PlayerId =
                0u;

            sessionToken =
                null;

            TransitionTo(
                NetworkClientState.ConnectingTcp);

            try
            {
                tcpTransport.Start(
                    serverAddress,
                    serverPort);
            }
            catch (Exception exception)
            {
                Fail(
                    exception.Message);
            }
        }

        public void CreateRoom(
    string nickname)
        {
            ThrowIfDisposed();

            if (State !=
                NetworkClientState.Connected)
            {
                throw new InvalidOperationException(
                    "Network client must be connected " +
                    "before creating a room.");
            }

            if (string.IsNullOrWhiteSpace(
                nickname))
            {
                throw new ArgumentException(
                    "Nickname cannot be empty.",
                    nameof(nickname));
            }

            string normalizedNickname =
                nickname.Trim();

            byte[] nicknamePayload =
                StrictUtf8.GetBytes(
                    normalizedNickname);

            try
            {
                tcpTransport.Send(
                    MessageType.SetNickname,
                    nicknamePayload);

                tcpTransport.Send(
                    MessageType.CreateRoomRequest,
                    Array.Empty<byte>());

                TransitionTo(
                    NetworkClientState.CreatingRoom);
            }
            catch (Exception exception)
            {
                Fail(
                    exception.Message);
            }
        }

        public void JoinRoom(
            string nickname)
        {
            ThrowIfDisposed();

            if (State !=
                NetworkClientState.Connected)
            {
                throw new InvalidOperationException(
                    "Network client must be connected " +
                    "before joining a room.");
            }

            if (string.IsNullOrWhiteSpace(
                nickname))
            {
                throw new ArgumentException(
                    "Nickname cannot be empty.",
                    nameof(nickname));
            }

            string normalizedNickname =
                nickname.Trim();

            byte[] nicknamePayload =
                StrictUtf8.GetBytes(
                    normalizedNickname);

            try
            {
                tcpTransport.Send(
                    MessageType.SetNickname,
                    nicknamePayload);

                tcpTransport.Send(
                    MessageType.JoinRoomRequest,
                    Array.Empty<byte>());

                TransitionTo(
                    NetworkClientState.JoiningRoom);
            }
            catch (Exception exception)
            {
                Fail(
                    exception.Message);
            }
        }

        public void SetPlayerSelection(
            CharacterId character,
            DifficultyId difficulty)
        {
            ThrowIfDisposed();

            if (State !=
                NetworkClientState.InRoom)
            {
                throw new InvalidOperationException(
                    "Network client must be in a room " +
                    "before selecting.");
            }

            if (character !=
                    CharacterId.Ranged &&
                character !=
                    CharacterId.Melee)
            {
                throw new ArgumentException(
                    "Character selection is invalid.",
                    nameof(character));
            }

            if (difficulty !=
                    DifficultyId.None &&
                difficulty !=
                    DifficultyId.Normal &&
                difficulty !=
                    DifficultyId.Hard &&
                difficulty !=
                    DifficultyId.Hell)
            {
                throw new ArgumentException(
                    "Difficulty selection is invalid.",
                    nameof(difficulty));
            }

            byte[] payload =
            {
                (byte)character,
                (byte)difficulty
            };

            try
            {
                tcpTransport.Send(
                    MessageType.SetPlayerSelection,
                    payload);
            }
            catch (Exception exception)
            {
                Fail(
                    exception.Message);
            }
        }

        public void SetReady(
            bool ready)
        {
            ThrowIfDisposed();

            if (State !=
                NetworkClientState.InRoom)
            {
                throw new InvalidOperationException(
                    "Network client must be in a room " +
                    "before changing ready state.");
            }

            byte[] payload =
            {
                ready ? (byte)1 : (byte)0
            };

            try
            {
                tcpTransport.Send(
                    MessageType.SetReady,
                    payload);
            }
            catch (Exception exception)
            {
                Fail(
                    exception.Message);
            }
        }

        public void StartGame()
        {
            ThrowIfDisposed();

            if (State !=
                NetworkClientState.InRoom)
            {
                throw new InvalidOperationException(
                    "Network client must be in a room " +
                    "before starting the game.");
            }

            try
            {
                tcpTransport.Send(
                    MessageType.StartGameRequest,
                    Array.Empty<byte>());
            }
            catch (Exception exception)
            {
                Fail(
                    exception.Message);
            }
        }

        public void LeaveRoom()
        {
            ThrowIfDisposed();

            if (State !=
                NetworkClientState.InRoom)
            {
                throw new InvalidOperationException(
                    "Network client must be in a room " +
                    "before leaving it.");
            }

            try
            {
                tcpTransport.Send(
                    MessageType.LeaveRoom,
                    Array.Empty<byte>());
            }
            catch (Exception exception)
            {
                Fail(
                    exception.Message);
            }
        }

        public void SendPlayerInput(
            PlayerInputPayload input)
        {
            ThrowIfDisposed();

            if (State !=
                NetworkClientState.InRoom)
            {
                throw new InvalidOperationException(
                    "Network client must be in a room " +
                    "before sending player input.");
            }

            byte[] payload =
                PlayerInputCodec.Encode(
                    input);

            uint sequence =
                nextUdpSequence;

            nextUdpSequence =
                unchecked(
                    nextUdpSequence + 1u);

            try
            {
                udpTransport.Send(
                    MessageType.PlayerInput,
                    sequence,
                    payload);
            }
            catch (Exception exception)
            {
                Fail(
                    exception.Message);
            }
        }

        public void SendPlayerStateSnapshot(
            PlayerStateSnapshotPayload snapshot)
        {
            ThrowIfDisposed();

            if (State !=
                NetworkClientState.InRoom)
            {
                throw new InvalidOperationException(
                    "Network client must be in a room " +
                    "before sending a player state snapshot.");
            }

            byte[] payload =
                PlayerStateSnapshotCodec.Encode(
                    snapshot);

            uint sequence =
                nextUdpSequence;

            nextUdpSequence =
                unchecked(
                    nextUdpSequence + 1u);

            try
            {
                udpTransport.Send(
                    MessageType.PlayerStateSnapshot,
                    sequence,
                    payload);
            }
            catch (Exception exception)
            {
                Fail(
                    exception.Message);
            }
        }

        public void SendWorldStateSnapshot(
            WorldStateSnapshotPayload snapshot)
        {
            ThrowIfDisposed();

            if (State !=
                NetworkClientState.InRoom)
            {
                throw new InvalidOperationException(
                    "Network client must be in a room " +
                    "before sending a world state snapshot.");
            }

            byte[] payload =
                WorldStateSnapshotCodec.Encode(
                    snapshot);

            uint sequence =
                nextUdpSequence;

            nextUdpSequence =
                unchecked(
                    nextUdpSequence + 1u);

            try
            {
                udpTransport.Send(
                    MessageType.WorldStateSnapshot,
                    sequence,
                    payload);
            }
            catch (Exception exception)
            {
                Fail(
                    exception.Message);
            }
        }

        public void SendWorldEntitySpawned(
            WorldEntityRecord record)
        {
            ThrowIfDisposed();

            if (State !=
                NetworkClientState.InRoom)
            {
                throw new InvalidOperationException(
                    "Network client must be in a room " +
                    "before sending a world entity spawn.");
            }

            if (!GameSession.IsHost)
            {
                throw new InvalidOperationException(
                    "Only the room host can send " +
                    "a world entity spawn.");
            }

            byte[] payload =
                WorldEntitySpawnedCodec.Encode(
                    record);

            try
            {
                tcpTransport.Send(
                    MessageType.WorldEntitySpawned,
                    payload);
            }
            catch (Exception exception)
            {
                Fail(
                    exception.Message);
            }
        }

        public void SendWorldEntityRemoved(
            WorldEntityRemovedPayload removed)
        {
            ThrowIfDisposed();

            if (State != NetworkClientState.InRoom)
            {
                throw new InvalidOperationException(
                    "Network client must be in a room " +
                    "before sending a world entity removal.");
            }

            if (!GameSession.IsHost)
            {
                throw new InvalidOperationException(
                    "Only the room host can send " +
                    "a world entity removal.");
            }

            byte[] payload =
                WorldEntityRemovedCodec.Encode(removed);

            try
            {
                tcpTransport.Send(
                    MessageType.WorldEntityRemoved,
                    payload);
            }
            catch (Exception exception)
            {
                Fail(exception.Message);
            }
        }

        public void SendPlayerShotEvent(
            PlayerShotEvent shotEvent)
        {
            ThrowIfDisposed();

            if (State !=
                NetworkClientState.InRoom)
            {
                throw new InvalidOperationException(
                    "Network client must be in a room " +
                    "before sending player shot event.");
            }

            byte[] payload =
                PlayerShotEventCodec.Encode(
                    shotEvent);

            uint sequence =
                nextUdpSequence;

            nextUdpSequence =
                unchecked(
                    nextUdpSequence + 1u);

            try
            {
                udpTransport.Send(
                    MessageType.PlayerShotEvent,
                    sequence,
                    payload);
            }
            catch (Exception exception)
            {
                Fail(
                    exception.Message);
            }
        }

        public void SendPlayerShotgunEvent(
            PlayerShotgunEvent shotgunEvent)
        {
            ThrowIfDisposed();

            if (State !=
                NetworkClientState.InRoom)
            {
                throw new InvalidOperationException(
                    "Network client must be in a room " +
                    "before sending player shotgun event.");
            }

            byte[] payload =
                PlayerShotgunEventCodec.Encode(
                    shotgunEvent);

            uint sequence =
                nextUdpSequence;

            nextUdpSequence =
                unchecked(
                    nextUdpSequence + 1u);

            try
            {
                udpTransport.Send(
                    MessageType.PlayerShotgunEvent,
                    sequence,
                    payload);
            }
            catch (Exception exception)
            {
                Fail(
                    exception.Message);
            }
        }

        public int Tick()
        {
            ThrowIfDisposed();

            return dispatcher.DispatchPending();
        }

        public void Disconnect()
        {
            if (disposed)
            {
                return;
            }

            try
            {
                StopTransports();

                ClearSession();

                LastError =
                    string.Empty;

                TransitionTo(
                    NetworkClientState.Disconnected);
            }
            catch (Exception exception)
            {
                ClearSession();

                LastError =
                    exception.Message;

                TransitionTo(
                    NetworkClientState.Error);
            }
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            Disconnect();

            dispatcher.EventDispatched -=
                HandleTransportEvent;

            disposed =
                true;
        }

        private void HandleTransportEvent(
            NetworkTransportEvent transportEvent)
        {
            if (transportEvent.EventType ==
                NetworkTransportEventType.Error)
            {
                Fail(
                    transportEvent.ErrorMessage);

                return;
            }

            if (transportEvent.EventType ==
                NetworkTransportEventType.Disconnected)
            {
                Fail(
                    $"{transportEvent.TransportKind} " +
                    "transport disconnected.");

                return;
            }

            if (transportEvent.EventType ==
                NetworkTransportEventType.Connected)
            {
                HandleConnectedEvent(
                    transportEvent.TransportKind);

                return;
            }

            if (transportEvent.EventType ==
                NetworkTransportEventType.PacketReceived)
            {
                HandlePacketReceived(
                    transportEvent);
            }
        }

        private void HandleConnectedEvent(
            NetworkTransportKind transportKind)
        {
            if (transportKind ==
                NetworkTransportKind.Tcp &&
                State ==
                NetworkClientState.ConnectingTcp)
            {
                TransitionTo(
                    NetworkClientState
                        .WaitingForServerHello);

                return;
            }

            if (transportKind ==
                NetworkTransportKind.Udp &&
                State ==
                NetworkClientState.ConnectingUdp)
            {
                SendUdpBindRequest();
            }
        }

        private void HandlePacketReceived(
            NetworkTransportEvent transportEvent)
        {
            if (transportEvent.TransportKind ==
                NetworkTransportKind.Tcp &&
                transportEvent.PacketType ==
                MessageType.ServerHello &&
                State ==
                NetworkClientState
                    .WaitingForServerHello)
            {
                HandleServerHello(
                    transportEvent.Payload);

                return;
            }

            if (transportEvent.TransportKind ==
                NetworkTransportKind.Tcp &&
            transportEvent.PacketType ==
                MessageType.CreateRoomResponse &&
            State ==
                NetworkClientState.CreatingRoom)
            {
                HandleCreateRoomResponse(
                    transportEvent.Payload);

                return;
            }

            if (transportEvent.TransportKind ==
                NetworkTransportKind.Tcp &&
                transportEvent.PacketType ==
                MessageType.JoinRoomResponse &&
                State ==
                NetworkClientState.JoiningRoom)
            {
                HandleJoinRoomResponse(
                    transportEvent.Payload);

                return;
            }

            if (transportEvent.TransportKind ==
                NetworkTransportKind.Tcp &&
                transportEvent.PacketType ==
                MessageType.RoomStateSnapshot &&
                State ==
                NetworkClientState.InRoom)
            {
                HandleRoomStateSnapshot(
                    transportEvent.Payload);

                return;
            }

            if (transportEvent.TransportKind ==
                NetworkTransportKind.Tcp &&
                transportEvent.PacketType ==
                MessageType.ErrorMessage &&
                (State == NetworkClientState.Connected ||
                 State == NetworkClientState.CreatingRoom ||
                 State == NetworkClientState.JoiningRoom ||
                 State == NetworkClientState.InRoom))
            {
                HandleErrorMessage(
                    transportEvent.Payload);

                return;
            }

            if (transportEvent.TransportKind ==
                NetworkTransportKind.Tcp &&
                transportEvent.PacketType ==
                MessageType.GameStarted &&
                State ==
                NetworkClientState.InRoom)
            {
                HandleGameStarted(
                    transportEvent.Payload);

                return;
            }

            if (transportEvent.TransportKind ==
                NetworkTransportKind.Tcp &&
                transportEvent.PacketType ==
                MessageType.LeaveRoom &&
                State ==
                NetworkClientState.InRoom)
            {
                HandleLeaveRoom(
                    transportEvent.Payload);

                return;
            }

            if (transportEvent.TransportKind ==
                NetworkTransportKind.Udp &&
            transportEvent.PacketType ==
                MessageType.PlayerInput &&
            State ==
                NetworkClientState.InRoom)
            {
                HandleRemotePlayerInput(
                    transportEvent.PlayerId,
                    transportEvent.Payload);

                return;
            }

            if (transportEvent.TransportKind ==
                NetworkTransportKind.Udp &&
                transportEvent.PacketType ==
                MessageType.PlayerStateSnapshot &&
                State ==
                NetworkClientState.InRoom)
            {
                HandlePlayerStateSnapshot(
                    transportEvent.PlayerId,
                    transportEvent.Sequence,
                    transportEvent.Payload);

                return;
            }

            if (transportEvent.TransportKind ==
                NetworkTransportKind.Tcp &&
                transportEvent.PacketType ==
                MessageType.WorldEntitySpawned &&
                State ==
                NetworkClientState.InRoom)
            {
                HandleWorldEntitySpawned(
                    transportEvent.Payload);

                return;
            }

            if (transportEvent.TransportKind ==
                NetworkTransportKind.Tcp &&
                transportEvent.PacketType ==
                MessageType.WorldEntityRemoved &&
                State == NetworkClientState.InRoom)
            {
                HandleWorldEntityRemoved(
                    transportEvent.Payload);

                return;
            }

            if (transportEvent.TransportKind ==
                NetworkTransportKind.Udp &&
                transportEvent.PacketType ==
                MessageType.WorldStateSnapshot &&
                State ==
                NetworkClientState.InRoom)
            {
                HandleWorldStateSnapshot(
                    transportEvent.PlayerId,
                    transportEvent.Sequence,
                    transportEvent.Payload);

                return;
            }

            if (transportEvent.TransportKind ==
                NetworkTransportKind.Udp &&
                transportEvent.PacketType ==
                MessageType.PlayerShotEvent &&
                State ==
                    NetworkClientState.InRoom)
            {
                HandlePlayerShotEvent(
                    transportEvent.PlayerId,
                    transportEvent.Payload);

                return;
            }

            if (transportEvent.TransportKind ==
                NetworkTransportKind.Udp &&
                transportEvent.PacketType ==
                MessageType.PlayerShotgunEvent &&
                State ==
                NetworkClientState.InRoom)
            {
                HandlePlayerShotgunEvent(
                    transportEvent);
            }

            if (transportEvent.TransportKind ==
                NetworkTransportKind.Udp &&
                transportEvent.PacketType ==
                MessageType.UdpBindAccepted &&
                State ==
                NetworkClientState.BindingUdp)
            {
                HandleUdpBindAccepted(
                    transportEvent.Sequence);
            }
        }

        private void HandleCreateRoomResponse(
            byte[] payload)
        {
            try
            {
                string roomId =
                    StrictUtf8.GetString(
                        payload);

                if (string.IsNullOrWhiteSpace(
                    roomId))
                {
                    throw new InvalidOperationException(
                        "CreateRoomResponse contains " +
                        "an empty room ID.");
                }

                CurrentRoomId =
                    roomId;

                TransitionTo(
                    NetworkClientState.InRoom);
            }
            catch (Exception exception)
            {
                Fail(
                    exception.Message);
            }
        }

        private void HandleJoinRoomResponse(
            byte[] payload)
        {
            try
            {
                string roomId =
                    StrictUtf8.GetString(
                        payload);

                if (string.IsNullOrWhiteSpace(
                    roomId))
                {
                    throw new InvalidOperationException(
                        "JoinRoomResponse contains " +
                        "an empty room ID.");
                }

                CurrentRoomId =
                    roomId;

                TransitionTo(
                    NetworkClientState.InRoom);
            }
            catch (Exception exception)
            {
                Fail(
                    exception.Message);
            }
        }

        private void HandleRoomStateSnapshot(
            byte[] payload)
        {
            try
            {
                RoomStateSnapshot snapshot =
                    RoomStateSnapshotCodec.Decode(
                        payload);

                if (!string.Equals(
                    snapshot.RoomId,
                    CurrentRoomId,
                    StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        "RoomStateSnapshot room ID does not " +
                        "match the current room.");
                }

                CurrentRoomState =
                    snapshot;

                RoomStateChanged?.Invoke(
                    snapshot);
            }
            catch (Exception exception)
            {
                Fail(
                    exception.Message);
            }
        }

        private void HandleErrorMessage(
            byte[] payload)
        {
            try
            {
                string errorMessage =
                    StrictUtf8.GetString(
                        payload);

                if (string.IsNullOrWhiteSpace(
                    errorMessage))
                {
                    throw new InvalidOperationException(
                        "ErrorMessage contains an empty message.");
                }

                LastError =
                    errorMessage;

                if (State ==
                        NetworkClientState.CreatingRoom ||
                    State ==
                        NetworkClientState.JoiningRoom)
                {
                    TransitionTo(
                        NetworkClientState.Connected);
                }

                ErrorReceived?.Invoke(
                    errorMessage);
            }
            catch (Exception exception)
            {
                Fail(
                    exception.Message);
            }
        }

        private void HandleGameStarted(
            byte[] payload)
        {
            try
            {
                if (payload.Length != 0)
                {
                    throw new InvalidOperationException(
                        "GameStarted payload must be empty.");
                }

                GameStarted?.Invoke();
            }
            catch (Exception exception)
            {
                Fail(
                    exception.Message);
            }
        }

        private void HandleRemotePlayerInput(
            uint remotePlayerId,
            byte[] payload)
        {
            try
            {
                if (remotePlayerId == 0u)
                {
                    throw new InvalidOperationException(
                        "Remote PlayerInput contains " +
                        "an invalid player ID.");
                }

                if (remotePlayerId ==
                    PlayerId)
                {
                    throw new InvalidOperationException(
                        "Remote PlayerInput cannot use " +
                        "the local player ID.");
                }

                PlayerInputPayload input =
                    PlayerInputCodec.Decode(
                        payload);

                RemotePlayerInputReceived?.Invoke(
                    remotePlayerId,
                    input);
            }
            catch (Exception exception)
            {
                Fail(
                    exception.Message);
            }
        }

        private void HandlePlayerStateSnapshot(
            uint senderPlayerId,
            uint sequence,
            byte[] payload)
        {
            try
            {
                if (senderPlayerId == 0u)
                {
                    throw new InvalidOperationException(
                        "Player state snapshot contains " +
                        "an invalid sender ID.");
                }

                PlayerStateSnapshotPayload snapshot =
                    PlayerStateSnapshotCodec.Decode(
                        payload);

                if (hasLastPlayerStateSequence &&
                    !IsSequenceNewer(
                        sequence,
                        lastPlayerStateSequence))
                {
                    return;
                }

                hasLastPlayerStateSequence =
                    true;

                lastPlayerStateSequence =
                    sequence;

                PlayerStateSnapshotReceived?.Invoke(
                    senderPlayerId,
                    snapshot);
            }
            catch (Exception exception)
            {
                Fail(
                    exception.Message);
            }
        }

        private void HandleWorldStateSnapshot(
            uint senderPlayerId,
            uint sequence,
            byte[] payload)
        {
            try
            {
                if (senderPlayerId == 0u)
                {
                    throw new InvalidOperationException(
                        "World state snapshot contains " +
                        "an invalid sender ID.");
                }

                WorldStateSnapshotPayload snapshot =
                    WorldStateSnapshotCodec.Decode(
                        payload);

                WorldStateSnapshotReceived?.Invoke(
                    senderPlayerId,
                    sequence,
                    snapshot);
            }
            catch (Exception exception)
            {
                Fail(
                    exception.Message);
            }
        }

        private void HandleWorldEntitySpawned(
            byte[] payload)
        {
            try
            {
                WorldEntityRecord record =
                    WorldEntitySpawnedCodec.Decode(
                        payload);

                WorldEntitySpawnedReceived?.Invoke(
                    record);
            }
            catch (Exception exception)
            {
                Fail(
                    exception.Message);
            }
        }

        private void HandleWorldEntityRemoved(
            byte[] payload)
        {
            try
            {
                WorldEntityRemovedPayload removed =
                    WorldEntityRemovedCodec.Decode(payload);

                WorldEntityRemovedReceived?.Invoke(removed);
            }
            catch (Exception exception)
            {
                Fail(exception.Message);
            }
        }

        private void HandlePlayerShotEvent(
            uint senderPlayerId,
            byte[] payload)
        {
            try
            {
                if (senderPlayerId == 0u)
                {
                    throw new InvalidOperationException(
                        "Player shot event contains " +
                        "an invalid sender ID.");
                }

                if (senderPlayerId ==
                    PlayerId)
                {
                    return;
                }

                PlayerShotEvent shotEvent =
                    PlayerShotEventCodec.Decode(
                        payload);

                if (shotEvent.PlayerId !=
                    senderPlayerId)
                {
                    throw new InvalidOperationException(
                        "Player shot event ID does not " +
                        "match the UDP sender ID.");
                }

                PlayerShotEventReceived?.Invoke(
                    senderPlayerId,
                    shotEvent);
            }
            catch (Exception exception)
            {
                Fail(
                    exception.Message);
            }
        }

        private void HandlePlayerShotgunEvent(
            NetworkTransportEvent transportEvent)
        {
            try
            {
                if (transportEvent == null ||
                    transportEvent.Payload == null)
                {
                    throw new InvalidOperationException(
                        "PlayerShotgunEvent transport data " +
                        "cannot be null.");
                }

                PlayerShotgunEvent shotgunEvent =
                    PlayerShotgunEventCodec.Decode(
                        transportEvent.Payload);

                uint senderPlayerId =
                    transportEvent.PlayerId;

                if (senderPlayerId == 0u)
                {
                    throw new InvalidOperationException(
                        "PlayerShotgunEvent sender ID must " +
                        "be non-zero.");
                }

                if (shotgunEvent.PlayerId == 0u)
                {
                    throw new InvalidOperationException(
                        "PlayerShotgunEvent player ID must " +
                        "be non-zero.");
                }

                PlayerShotgunEventReceived?.Invoke(
                    senderPlayerId,
                    shotgunEvent);
            }
            catch (Exception exception)
            {
                Fail(
                    exception.Message);
            }
        }

        private static bool IsSequenceNewer(
            uint candidate,
            uint current)
        {
            return candidate != current &&
                unchecked(
                    (int)(candidate - current)) > 0;
        }

        private void HandleLeaveRoom(
            byte[] payload)
        {
            try
            {
                if (payload.Length != 0)
                {
                    throw new InvalidOperationException(
                        "LeaveRoom payload must be empty.");
                }

                CurrentRoomId =
                    string.Empty;

                CurrentRoomState =
                    null;

                TransitionTo(
                    NetworkClientState.Connected);
            }
            catch (Exception exception)
            {
                Fail(
                    exception.Message);
            }
        }

        private void HandleServerHello(
            byte[] payload)
        {
            try
            {
                UdpBindingCredentials credentials =
                    UdpBindingCredentialsCodec.Decode(
                        payload);

                if (credentials.PlayerId == 0u)
                {
                    throw new InvalidOperationException(
                        "ServerHello contains an invalid player ID.");
                }

                PlayerId =
                    credentials.PlayerId;

                sessionToken =
                    (byte[])
                    credentials.SessionToken.Clone();

                TransitionTo(
                    NetworkClientState.ConnectingUdp);

                udpTransport.Start(
                    serverAddress,
                    serverPort,
                    PlayerId,
                    sessionToken);
            }
            catch (Exception exception)
            {
                Fail(
                    exception.Message);
            }
        }

        private void SendUdpBindRequest()
        {
            try
            {
                pendingBindSequence =
                    nextUdpSequence++;

                TransitionTo(
                    NetworkClientState.BindingUdp);

                udpTransport.Send(
                    MessageType.UdpBindRequest,
                    pendingBindSequence,
                    Array.Empty<byte>());
            }
            catch (Exception exception)
            {
                Fail(
                    exception.Message);
            }
        }

        private void HandleUdpBindAccepted(
            uint sequence)
        {
            if (sequence !=
                pendingBindSequence)
            {
                Fail(
                    "UDP bind response sequence does not " +
                    "match the request.");

                return;
            }

            pendingBindSequence =
                0u;

            TransitionTo(
                NetworkClientState.Connected);
        }

        private void Fail(
            string errorMessage)
        {
            string finalError =
                string.IsNullOrWhiteSpace(errorMessage)
                    ? "Unknown network error."
                    : errorMessage;

            Debug.LogError(
                $"NetworkClient failure: {finalError}");

            try
            {
                StopTransports();
            }
            catch (Exception stopException)
            {
                finalError =
                    finalError +
                    " Shutdown error: " +
                    stopException.Message;
            }

            ClearSession();

            LastError =
                finalError;

            TransitionTo(
                NetworkClientState.Error);
        }

        private void StopTransports()
        {
            Exception firstException =
                null;

            try
            {
                udpTransport.Stop();
            }
            catch (Exception exception)
            {
                firstException =
                    exception;
            }

            try
            {
                tcpTransport.Stop();
            }
            catch (Exception exception)
            {
                if (firstException == null)
                {
                    firstException =
                        exception;
                }
            }

            if (firstException != null)
            {
                throw new InvalidOperationException(
                    "Failed to stop network transports.",
                    firstException);
            }
        }

        private void ClearSession()
        {
            CurrentRoomId =
                string.Empty;

            CurrentRoomState =
                null;

            PlayerId =
                0u;

            sessionToken =
                null;

            pendingBindSequence =
                0u;

            hasLastPlayerStateSequence =
                false;

            lastPlayerStateSequence =
                0u;

            serverAddress =
                string.Empty;

            serverPort =
                0;
        }

        private void TransitionTo(
            NetworkClientState nextState)
        {
            if (State == nextState)
            {
                return;
            }

            NetworkClientState previousState =
                State;

            Debug.Log(
                $"NetworkClient state transition: " +
                $"{previousState} -> {nextState}, " +
                $"room='{CurrentRoomId}', " +
                $"playerId={PlayerId}");

            State =
                nextState;

            StateChanged?.Invoke(
                State);
        }

        private void ThrowIfDisposed()
        {
            if (disposed)
            {
                throw new ObjectDisposedException(
                    nameof(NetworkClient));
            }
        }
    }
}
