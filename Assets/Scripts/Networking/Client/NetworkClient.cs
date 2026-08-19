using System;
using System.Text;
using TopDownRoguelike.Infrastructure;
using TopDownRoguelike.Networking.Protocol;
using TopDownRoguelike.Networking.Transport;

namespace TopDownRoguelike.Networking.Client
{
    public sealed class NetworkClient
        : IDisposable
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

        private string pendingRoomId =
            string.Empty;

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

            pendingRoomId =
                string.Empty;

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
    string nickname,
    string roomId)
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

            if (string.IsNullOrWhiteSpace(
                roomId))
            {
                throw new ArgumentException(
                    "Room ID cannot be empty.",
                    nameof(roomId));
            }

            string normalizedNickname =
                nickname.Trim();

            string normalizedRoomId =
                roomId.Trim();

            byte[] nicknamePayload =
                StrictUtf8.GetBytes(
                    normalizedNickname);

            byte[] roomIdPayload =
                StrictUtf8.GetBytes(
                    normalizedRoomId);

            try
            {
                tcpTransport.Send(
                    MessageType.SetNickname,
                    nicknamePayload);

                tcpTransport.Send(
                    MessageType.JoinRoomRequest,
                    roomIdPayload);

                pendingRoomId =
                    normalizedRoomId;

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

                if (!string.Equals(
                    roomId,
                    pendingRoomId,
                    StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        "JoinRoomResponse room ID does not " +
                        "match the requested room.");
                }

                CurrentRoomId =
                    roomId;

                pendingRoomId =
                    string.Empty;

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
                    pendingRoomId =
                        string.Empty;

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

                pendingRoomId =
                    string.Empty;

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

            pendingRoomId =
                string.Empty;

            PlayerId =
                0u;

            sessionToken =
                null;

            pendingBindSequence =
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