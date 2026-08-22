using System;

namespace TopDownRoguelike.Networking.Client
{
    public sealed class RoomConnectionFlow
        : IDisposable
    {
        private readonly IRoomNetworkClient client;

        private RoomConnectionRequest pendingRequest;
        private bool pendingHostRequest;
        private bool disposed;

        public RoomConnectionFlow(
            IRoomNetworkClient client)
        {
            this.client =
                client
                ?? throw new ArgumentNullException(
                    nameof(client));

            this.client.StateChanged +=
                HandleStateChanged;
        }

        public void BeginHost(
            RoomConnectionRequest request)
        {
            ThrowIfDisposed();

            if (request == null)
            {
                throw new ArgumentNullException(
                    nameof(request));
            }

            if (!string.IsNullOrEmpty(
                request.RoomId))
            {
                throw new ArgumentException(
                    "Host request must not contain " +
                    "a room ID.",
                    nameof(request));
            }

            if (client.State !=
                NetworkClientState.Disconnected)
            {
                throw new InvalidOperationException(
                    "Network client must be disconnected " +
                    "before starting a room connection.");
            }

            pendingRequest =
                request;

            pendingHostRequest =
                true;

            try
            {
                client.Connect(
                    request.Address,
                    request.Port);
            }
            catch
            {
                ClearPendingRequest();
                throw;
            }
        }

        public void BeginJoin(
            RoomConnectionRequest request)
        {
            ThrowIfDisposed();

            if (request == null)
            {
                throw new ArgumentNullException(
                    nameof(request));
            }

            if (string.IsNullOrWhiteSpace(
                request.RoomId))
            {
                throw new ArgumentException(
                    "Join request must contain a room ID.",
                    nameof(request));
            }

            if (client.State !=
                NetworkClientState.Disconnected)
            {
                throw new InvalidOperationException(
                    "Network client must be disconnected " +
                    "before starting a room connection.");
            }

            pendingRequest =
                request;

            pendingHostRequest =
                false;

            try
            {
                client.Connect(
                    request.Address,
                    request.Port);
            }
            catch
            {
                ClearPendingRequest();
                throw;
            }
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            client.StateChanged -=
                HandleStateChanged;

            ClearPendingRequest();

            disposed =
                true;
        }

        private void HandleStateChanged(
            NetworkClientState state)
        {
            if (state ==
                    NetworkClientState.Error ||
                state ==
                    NetworkClientState.Disconnected)
            {
                ClearPendingRequest();
                return;
            }

            if (state !=
                    NetworkClientState.Connected ||
                pendingRequest == null)
            {
                return;
            }

            RoomConnectionRequest request =
                pendingRequest;

            bool shouldCreateRoom =
                pendingHostRequest;

            ClearPendingRequest();

            if (shouldCreateRoom)
            {
                client.CreateRoom(
                    request.Nickname);

                return;
            }

            client.JoinRoom(
                request.Nickname,
                request.RoomId);
        }

        private void ClearPendingRequest()
        {
            pendingRequest =
                null;

            pendingHostRequest =
                false;
        }

        private void ThrowIfDisposed()
        {
            if (disposed)
            {
                throw new ObjectDisposedException(
                    nameof(RoomConnectionFlow));
            }
        }
    }
}