using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Reflection;
using NUnit.Framework;
using TopDownRoguelike.Networking.Client;
using TopDownRoguelike.Networking.Protocol;
using TopDownRoguelike.Infrastructure;
using System.Collections.Generic;
using System.Text;

namespace TopDownRoguelike.Tests.EditMode
{
    public sealed class NetworkClientTests
    {
        [Test]
        public void JoinRoom_DoesNotExposeLegacyRoomIdState()
        {
            Type clientType =
                typeof(NetworkClient);

            Assert.That(
                clientType.GetMethod(
                    "JoinRoom",
                    new[]
                    {
                typeof(string),
                typeof(string)
                    }),
                Is.Null,
                "NetworkClient must not expose the legacy " +
                "JoinRoom(nickname, roomId) overload.");

            Assert.That(
                clientType.GetField(
                    "pendingRoomId",
                    BindingFlags.Instance |
                    BindingFlags.NonPublic),
                Is.Null,
                "NetworkClient must not retain " +
                "pendingRoomId state.");
        }

        [Test]
        public void TcpAndUdpHandshake_ReachesConnected()
        {
            var tcpListener =
                new TcpListener(
                    IPAddress.IPv6Loopback,
                    0);

            tcpListener.Start();

            int port =
                ((IPEndPoint)
                    tcpListener.LocalEndpoint).Port;

            var udpServer =
                new UdpClient(
                    AddressFamily.InterNetworkV6);

            udpServer.Client.DualMode = true;

            udpServer.Client.Bind(
                new IPEndPoint(
                    IPAddress.IPv6Loopback,
                    port));

            udpServer.Client.ReceiveTimeout =
                2000;

            var client =
                new NetworkClient();

            TcpClient acceptedClient =
                null;

            byte[] token =
                CreateToken();

            try
            {
                var acceptTask =
                    tcpListener.AcceptTcpClientAsync();

                client.Connect(
                    "::1",
                    port);

                Assert.That(
                    acceptTask.Wait(2000),
                    Is.True);

                acceptedClient =
                    acceptTask.Result;

                WaitForState(
                    client,
                    NetworkClientState
                        .WaitingForServerHello);

                var credentials =
                    new UdpBindingCredentials(
                        7u,
                        token);

                byte[] helloPayload =
                    UdpBindingCredentialsCodec.Encode(
                        credentials);

                byte[] serverHello =
                    PacketCodec.Encode(
                        MessageType.ServerHello,
                        helloPayload);

                NetworkStream stream =
                    acceptedClient.GetStream();

                stream.Write(
                    serverHello,
                    0,
                    serverHello.Length);

                WaitForState(
                    client,
                    NetworkClientState.BindingUdp);

                var clientEndpoint =
                    new IPEndPoint(
                        IPAddress.IPv6Any,
                        0);

                byte[] bindDatagram =
                    udpServer.Receive(
                        ref clientEndpoint);

                DecodedUdpPacket bindRequest =
                    UdpPacketCodec.Decode(
                        bindDatagram);

                Assert.That(
                    bindRequest.Header.Type,
                    Is.EqualTo(
                        MessageType.UdpBindRequest));

                Assert.That(
                    bindRequest.Header.PlayerId,
                    Is.EqualTo(7u));

                Assert.That(
                    bindRequest.Header.SessionToken,
                    Is.EqualTo(token));

                var acceptedHeader =
                    new UdpMessageHeader(
                        MessageType.UdpBindAccepted,
                        token,
                        7u,
                        bindRequest.Header.Sequence);

                byte[] bindAccepted =
                    UdpPacketCodec.Encode(
                        acceptedHeader,
                        Array.Empty<byte>());

                udpServer.Send(
                    bindAccepted,
                    bindAccepted.Length,
                    clientEndpoint);

                WaitForState(
                    client,
                    NetworkClientState.Connected);

                Assert.That(
                    client.PlayerId,
                    Is.EqualTo(7u));

                Assert.That(
                    client.SessionToken,
                    Is.EqualTo(token));

                Assert.That(
                    client.LastError,
                    Is.Empty);

                client.CreateRoom(
    " Seele ");

                Assert.That(
                    client.State,
                    Is.EqualTo(
                        NetworkClientState.CreatingRoom));

                List<DecodedPacket> roomRequests =
                    ReceiveTcpPackets(
                        acceptedClient,
                        2);

                Assert.That(
                    roomRequests[0].Type,
                    Is.EqualTo(
                        MessageType.SetNickname));

                Assert.That(
                    Encoding.UTF8.GetString(
                        roomRequests[0].Payload),
                    Is.EqualTo("Seele"));

                Assert.That(
                    roomRequests[1].Type,
                    Is.EqualTo(
                        MessageType.CreateRoomRequest));

                Assert.That(
                    roomRequests[1].Payload,
                    Is.Empty);

                byte[] createRoomResponse =
                    PacketCodec.Encode(
                        MessageType.CreateRoomResponse,
                        Encoding.UTF8.GetBytes(
                            "ROOM-1"));

                stream.Write(
                    createRoomResponse,
                    0,
                    createRoomResponse.Length);

                WaitForState(
                    client,
                    NetworkClientState.InRoom);

                Assert.That(
                    client.CurrentRoomId,
                    Is.EqualTo("ROOM-1"));

                Assert.That(
                    client.LastError,
                    Is.Empty);
            }
            finally
            {
                client.Dispose();
                acceptedClient?.Close();
                udpServer.Close();
                tcpListener.Stop();
            }

            Assert.That(
                client.State,
                Is.EqualTo(
                    NetworkClientState.Disconnected));
        }
        [Test]
        public void TcpAndUdpHandshake_JoinRoomResponseEntersInRoom()
        {
            var tcpListener =
                new TcpListener(
                    IPAddress.IPv6Loopback,
                    0);

            tcpListener.Start();

            int port =
                ((IPEndPoint)
                    tcpListener.LocalEndpoint).Port;

            var udpServer =
                new UdpClient(
                    AddressFamily.InterNetworkV6);

            udpServer.Client.DualMode = true;

            udpServer.Client.Bind(
                new IPEndPoint(
                    IPAddress.IPv6Loopback,
                    port));

            udpServer.Client.ReceiveTimeout =
                2000;

            var client =
                new NetworkClient();

            TcpClient acceptedClient =
                null;

            byte[] token =
                CreateToken();

            try
            {
                var acceptTask =
                    tcpListener.AcceptTcpClientAsync();

                client.Connect(
                    "::1",
                    port);

                Assert.That(
                    acceptTask.Wait(2000),
                    Is.True);

                acceptedClient =
                    acceptTask.Result;

                WaitForState(
                    client,
                    NetworkClientState
                        .WaitingForServerHello);

                var credentials =
                    new UdpBindingCredentials(
                        7u,
                        token);

                byte[] helloPayload =
                    UdpBindingCredentialsCodec.Encode(
                        credentials);

                byte[] serverHello =
                    PacketCodec.Encode(
                        MessageType.ServerHello,
                        helloPayload);

                NetworkStream stream =
                    acceptedClient.GetStream();

                stream.Write(
                    serverHello,
                    0,
                    serverHello.Length);

                WaitForState(
                    client,
                    NetworkClientState.BindingUdp);

                var clientEndpoint =
                    new IPEndPoint(
                        IPAddress.IPv6Any,
                        0);

                byte[] bindDatagram =
                    udpServer.Receive(
                        ref clientEndpoint);

                DecodedUdpPacket bindRequest =
                    UdpPacketCodec.Decode(
                        bindDatagram);

                Assert.That(
                    bindRequest.Header.Type,
                    Is.EqualTo(
                        MessageType.UdpBindRequest));

                Assert.That(
                    bindRequest.Header.PlayerId,
                    Is.EqualTo(7u));

                Assert.That(
                    bindRequest.Header.SessionToken,
                    Is.EqualTo(token));

                var acceptedHeader =
                    new UdpMessageHeader(
                        MessageType.UdpBindAccepted,
                        token,
                        7u,
                        bindRequest.Header.Sequence);

                byte[] bindAccepted =
                    UdpPacketCodec.Encode(
                        acceptedHeader,
                        Array.Empty<byte>());

                udpServer.Send(
                    bindAccepted,
                    bindAccepted.Length,
                    clientEndpoint);

                WaitForState(
                    client,
                    NetworkClientState.Connected);

                Assert.That(
                    client.PlayerId,
                    Is.EqualTo(7u));

                Assert.That(
                    client.SessionToken,
                    Is.EqualTo(token));

                Assert.That(
                    client.LastError,
                    Is.Empty);

                MethodInfo joinRoomMethod =
                    typeof(NetworkClient).GetMethod(
                        "JoinRoom",
                        new[]
                        {
                            typeof(string)
                        });

                Assert.That(
                    joinRoomMethod,
                    Is.Not.Null,
                    "NetworkClient must define JoinRoom(string).");

                joinRoomMethod.Invoke(
                    client,
                    new object[]
                    {
                        " Guest "
                    });

                Assert.That(
                    client.State,
                    Is.EqualTo(
                        NetworkClientState.JoiningRoom));

                List<DecodedPacket> roomRequests =
                    ReceiveTcpPackets(
                        acceptedClient,
                        2);

                Assert.That(
                    roomRequests[0].Type,
                    Is.EqualTo(
                        MessageType.SetNickname));

                Assert.That(
                    Encoding.UTF8.GetString(
                        roomRequests[0].Payload),
                    Is.EqualTo("Guest"));

                Assert.That(
                    roomRequests[1].Type,
                    Is.EqualTo(
                        MessageType.JoinRoomRequest));

                Assert.That(
                    roomRequests[1].Payload,
                    Is.Empty,
                    "JoinRoomRequest payload must be empty.");

                byte[] joinRoomResponse =
                    PacketCodec.Encode(
                        MessageType.JoinRoomResponse,
                        Encoding.UTF8.GetBytes(
                            "ROOM-1"));

                stream.Write(
                    joinRoomResponse,
                    0,
                    joinRoomResponse.Length);

                WaitForState(
                    client,
                    NetworkClientState.InRoom);

                Assert.That(
                    client.CurrentRoomId,
                    Is.EqualTo("ROOM-1"));

                Assert.That(
                    client.LastError,
                    Is.Empty);

                RoomStateSnapshot receivedSnapshot =
                    null;

                client.RoomStateChanged +=
                    snapshot =>
                        receivedSnapshot = snapshot;

                var expectedSnapshot =
                    new RoomStateSnapshot(
                        "ROOM-1",
                        RoomStateStatus.Waiting,
                        DifficultyId.Hard,
                        new[]
                        {
                            new RoomPlayerSnapshot(
                                7u,
                                true,
                                false,
                                CharacterId.Ranged,
                                "Guest")
                        });

                byte[] snapshotPacket =
                    PacketCodec.Encode(
                        MessageType.RoomStateSnapshot,
                        RoomStateSnapshotCodec.Encode(
                            expectedSnapshot));

                stream.Write(
                    snapshotPacket,
                    0,
                    snapshotPacket.Length);

                bool snapshotDispatched =
                    SpinWait.SpinUntil(
                        () =>
                        {
                            client.Tick();

                            return receivedSnapshot !=
                                null;
                        },
                        2000);

                Assert.That(
                    snapshotDispatched,
                    Is.True);

                Assert.That(
                    client.CurrentRoomState,
                    Is.SameAs(receivedSnapshot));

                Assert.That(
                    receivedSnapshot.RoomId,
                    Is.EqualTo("ROOM-1"));

                Assert.That(
                    receivedSnapshot.SelectedDifficulty,
                    Is.EqualTo(DifficultyId.Hard));

                Assert.That(
                    receivedSnapshot.Players[0].PlayerId,
                    Is.EqualTo(7u));

                Assert.That(
                    client.State,
                    Is.EqualTo(NetworkClientState.InRoom));

                Assert.That(
                    client.LastError,
                    Is.Empty);

                string receivedError =
                    null;

                client.ErrorReceived +=
                    errorMessage =>
                        receivedError = errorMessage;

                const string expectedError =
                    "Only the room host can start the game.";

                byte[] errorPacket =
                    PacketCodec.Encode(
                        MessageType.ErrorMessage,
                        Encoding.UTF8.GetBytes(
                            expectedError));

                stream.Write(
                    errorPacket,
                    0,
                    errorPacket.Length);

                bool errorDispatched =
                    SpinWait.SpinUntil(
                        () =>
                        {
                            client.Tick();

                            return receivedError != null;
                        },
                        2000);

                Assert.That(
                    errorDispatched,
                    Is.True);

                Assert.That(
                    receivedError,
                    Is.EqualTo(expectedError));

                Assert.That(
                    client.LastError,
                    Is.EqualTo(expectedError));

                Assert.That(
                    client.State,
                    Is.EqualTo(NetworkClientState.InRoom));

                Assert.That(
                    client.CurrentRoomState,
                    Is.SameAs(receivedSnapshot));

                client.SetPlayerSelection(
                    CharacterId.Melee,
                    DifficultyId.Hard);

                Assert.That(
                    client.State,
                    Is.EqualTo(
                        NetworkClientState.InRoom));

                List<DecodedPacket> selectionPackets =
                    ReceiveTcpPackets(
                        acceptedClient,
                        1);

                Assert.That(
                    selectionPackets[0].Type,
                    Is.EqualTo(
                        MessageType.SetPlayerSelection));

                Assert.That(
                    selectionPackets[0].Payload,
                    Is.EqualTo(
                        new byte[]
                        {
                            (byte)CharacterId.Melee,
                            (byte)DifficultyId.Hard
                        }));

                client.SetReady(true);

                Assert.That(
                    client.State,
                    Is.EqualTo(
                        NetworkClientState.InRoom));

                List<DecodedPacket> readyPackets =
                    ReceiveTcpPackets(
                        acceptedClient,
                        1);

                Assert.That(
                    readyPackets[0].Type,
                    Is.EqualTo(
                        MessageType.SetReady));

                Assert.That(
                    readyPackets[0].Payload,
                    Is.EqualTo(
                        new byte[] { 1 }));

                bool gameStarted =
                    false;

                client.GameStarted +=
                    () =>
                        gameStarted = true;

                client.StartGame();

                Assert.That(
                    client.State,
                    Is.EqualTo(
                        NetworkClientState.InRoom));

                List<DecodedPacket> startPackets =
                    ReceiveTcpPackets(
                        acceptedClient,
                        1);

                Assert.That(
                    startPackets[0].Type,
                    Is.EqualTo(
                        MessageType.StartGameRequest));

                Assert.That(
                    startPackets[0].Payload,
                    Is.Empty);

                byte[] gameStartedPacket =
                    PacketCodec.Encode(
                        MessageType.GameStarted,
                        Array.Empty<byte>());

                stream.Write(
                    gameStartedPacket,
                    0,
                    gameStartedPacket.Length);

                bool gameStartedDispatched =
                    SpinWait.SpinUntil(
                        () =>
                        {
                            client.Tick();

                            return gameStarted;
                        },
                        2000);

                Assert.That(
                    gameStartedDispatched,
                    Is.True);

                Assert.That(
                    client.State,
                    Is.EqualTo(
                        NetworkClientState.InRoom));

                client.LeaveRoom();

                Assert.That(
                    client.State,
                    Is.EqualTo(
                        NetworkClientState.InRoom));

                List<DecodedPacket> leaveRequests =
                    ReceiveTcpPackets(
                        acceptedClient,
                        1);

                Assert.That(
                    leaveRequests[0].Type,
                    Is.EqualTo(
                        MessageType.LeaveRoom));

                Assert.That(
                    leaveRequests[0].Payload,
                    Is.Empty);

                byte[] leavePacket =
                    PacketCodec.Encode(
                        MessageType.LeaveRoom,
                        Array.Empty<byte>());

                stream.Write(
                    leavePacket,
                    0,
                    leavePacket.Length);

                WaitForState(
                    client,
                    NetworkClientState.Connected);

                Assert.That(
                    client.CurrentRoomId,
                    Is.Empty);

                Assert.That(
                    client.CurrentRoomState,
                    Is.Null);
            }
            finally
            {
                client.Dispose();
                acceptedClient?.Close();
                udpServer.Close();
                tcpListener.Stop();
            }

            Assert.That(
                client.State,
                Is.EqualTo(
                    NetworkClientState.Disconnected));
        }

        [Test]
        public void InvalidServerHello_EntersErrorState()
        {
            var tcpListener =
                new TcpListener(
                    IPAddress.IPv6Loopback,
                    0);

            tcpListener.Start();

            int port =
                ((IPEndPoint)
                    tcpListener.LocalEndpoint).Port;

            var client =
                new NetworkClient();

            TcpClient acceptedClient =
                null;

            try
            {
                var acceptTask =
                    tcpListener.AcceptTcpClientAsync();

                client.Connect(
                    "::1",
                    port);

                Assert.That(
                    acceptTask.Wait(2000),
                    Is.True);

                acceptedClient =
                    acceptTask.Result;

                WaitForState(
                    client,
                    NetworkClientState
                        .WaitingForServerHello);

                byte[] invalidPayload =
                    new byte[
                        UdpBindingCredentialsCodec
                            .CredentialsSize - 1];

                byte[] serverHello =
                    PacketCodec.Encode(
                        MessageType.ServerHello,
                        invalidPayload);

                NetworkStream stream =
                    acceptedClient.GetStream();

                stream.Write(
                    serverHello,
                    0,
                    serverHello.Length);

                WaitForState(
                    client,
                    NetworkClientState.Error);

                Assert.That(
                    client.LastError,
                    Does.Contain("20 bytes"));
            }
            finally
            {
                client.Dispose();
                acceptedClient?.Close();
                tcpListener.Stop();
            }
        }

        private static List<DecodedPacket>
    ReceiveTcpPackets(
        TcpClient client,
        int expectedCount)
        {
            NetworkStream stream =
                client.GetStream();

            stream.ReadTimeout =
                2000;

            var codec =
                new PacketCodec();

            var packets =
                new List<DecodedPacket>();

            var buffer =
                new byte[1024];

            while (packets.Count < expectedCount)
            {
                int bytesRead =
                    stream.Read(
                        buffer,
                        0,
                        buffer.Length);

                if (bytesRead == 0)
                {
                    throw new InvalidOperationException(
                        "TCP client disconnected.");
                }

                codec.Append(
                    buffer,
                    0,
                    bytesRead);

                packets.AddRange(
                    codec.DecodeAvailable());
            }

            return packets;
        }

        private static void WaitForState(
            NetworkClient client,
            NetworkClientState expectedState)
        {
            bool reached =
                SpinWait.SpinUntil(
                    () =>
                    {
                        client.Tick();

                        return client.State ==
                            expectedState;
                    },
                    2000);

            Assert.That(
                reached,
                Is.True,
                $"Timed out waiting for {expectedState}. " +
                $"Current state: {client.State}. " +
                $"Error: {client.LastError}");
        }

        private static byte[] CreateToken()
        {
            var token =
                new byte[
                    UdpPacketCodec.SessionTokenSize];

            for (int index = 0;
                index < token.Length;
                index++)
            {
                token[index] =
                    (byte)index;
            }

            return token;
        }
    }
}