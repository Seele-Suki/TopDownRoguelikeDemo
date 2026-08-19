using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using TopDownRoguelike.Networking.Protocol;

namespace TopDownRoguelike.Networking.Transport
{
    public sealed class UdpClientTransport
    {
        private readonly
            MainThreadMessageQueue<NetworkTransportEvent>
            messageQueue;

        private readonly ConcurrentQueue<byte[]>
            outgoingDatagrams =
                new ConcurrentQueue<byte[]>();

        private readonly ManualResetEventSlim stopSignal =
            new ManualResetEventSlim(false);

        private volatile Thread workerThread;
        private volatile UdpClient udpClient;

        private uint playerId;
        private byte[] sessionToken;

        public UdpClientTransport(
            MainThreadMessageQueue<NetworkTransportEvent>
                messageQueue)
        {
            this.messageQueue = messageQueue
                ?? throw new ArgumentNullException(
                    nameof(messageQueue));
        }

        public bool IsRunning =>
            workerThread != null &&
            workerThread.IsAlive;

        public void Start(
            string address,
            int port,
            uint playerId,
            byte[] sessionToken)
        {
            if (workerThread != null)
            {
                throw new InvalidOperationException(
                    "UDP transport has already started.");
            }

            if (sessionToken == null)
            {
                throw new ArgumentNullException(
                    nameof(sessionToken));
            }

            if (sessionToken.Length !=
                UdpPacketCodec.SessionTokenSize)
            {
                throw new ArgumentException(
                    "UDP session token must contain 16 bytes.",
                    nameof(sessionToken));
            }

            this.playerId = playerId;

            this.sessionToken =
                (byte[])sessionToken.Clone();

            stopSignal.Reset();

            workerThread = new Thread(
                () => Run(address, port))
            {
                IsBackground = true,
                Name = "UDP Client Transport"
            };

            workerThread.Start();
        }

        public void Send(
            MessageType messageType,
            uint sequence,
            byte[] payload)
        {
            if (workerThread == null ||
                stopSignal.IsSet)
            {
                throw new InvalidOperationException(
                    "UDP transport is not running.");
            }

            var header =
                new UdpMessageHeader(
                    messageType,
                    sessionToken,
                    playerId,
                    sequence);

            byte[] datagram =
                UdpPacketCodec.Encode(
                    header,
                    payload);

            outgoingDatagrams.Enqueue(
                datagram);
        }

        public void Stop()
        {
            stopSignal.Set();
            udpClient?.Close();

            Thread thread =
                workerThread;

            if (thread != null &&
                thread != Thread.CurrentThread &&
                !thread.Join(1000))
            {
                throw new TimeoutException(
                    "UDP worker thread did not stop.");
            }

            while (outgoingDatagrams.TryDequeue(
                out _))
            {
            }

            workerThread = null;
            udpClient = null;
            playerId = 0u;
            sessionToken = null;
        }

        private void Run(
            string address,
            int port)
        {
            UdpClient client = null;

            try
            {
                if (stopSignal.IsSet)
                {
                    return;
                }

                client =
                    new UdpClient(
                        AddressFamily.InterNetworkV6);

                client.Client.DualMode = true;

                udpClient = client;

                if (stopSignal.IsSet)
                {
                    return;
                }

                client.Connect(
                    address,
                    port);

                if (stopSignal.IsSet)
                {
                    return;
                }

                messageQueue.Enqueue(
                    NetworkTransportEvent.Connected(
                        NetworkTransportKind.Udp));

                var remoteEndpoint =
                    new IPEndPoint(
                        IPAddress.IPv6Any,
                        0);

                while (!stopSignal.IsSet)
                {
                    SendQueuedDatagrams(client);

                    if (!client.Client.Poll(
                        10000,
                        SelectMode.SelectRead))
                    {
                        continue;
                    }

                    byte[] datagram =
                        client.Receive(
                            ref remoteEndpoint);

                    HandleReceivedDatagram(
                        datagram);
                }
            }
            catch (Exception exception)
            {
                if (!stopSignal.IsSet)
                {
                    messageQueue.Enqueue(
                        NetworkTransportEvent.Error(
                            NetworkTransportKind.Udp,
                            exception.Message));
                }
            }
            finally
            {
                client?.Close();
                udpClient = null;
            }
        }

        private void SendQueuedDatagrams(
            UdpClient client)
        {
            while (outgoingDatagrams.TryDequeue(
                out byte[] datagram))
            {
                int bytesSent =
                    client.Send(
                        datagram,
                        datagram.Length);

                if (bytesSent !=
                    datagram.Length)
                {
                    throw new InvalidOperationException(
                        "UDP socket sent a partial datagram.");
                }
            }
        }

        private void HandleReceivedDatagram(
            byte[] datagram)
        {
            try
            {
                DecodedUdpPacket decoded =
                    UdpPacketCodec.Decode(
                        datagram);

                if (!MatchesCredentials(
                    decoded.Header))
                {
                    messageQueue.Enqueue(
                        NetworkTransportEvent.Error(
                            NetworkTransportKind.Udp,
                            "UDP packet identity does not match the active session."));

                    return;
                }

                messageQueue.Enqueue(
                    NetworkTransportEvent.UdpPacketReceived(
                        decoded.Header.Type,
                        decoded.Header.Sequence,
                        decoded.Payload));
            }
            catch (Exception exception)
            {
                if (!stopSignal.IsSet)
                {
                    messageQueue.Enqueue(
                        NetworkTransportEvent.Error(
                            NetworkTransportKind.Udp,
                            exception.Message));
                }
            }
        }

        private bool MatchesCredentials(
            UdpMessageHeader header)
        {
            if (header.PlayerId != playerId ||
                header.SessionToken == null ||
                header.SessionToken.Length !=
                sessionToken.Length)
            {
                return false;
            }

            for (int index = 0;
                index < sessionToken.Length;
                index++)
            {
                if (header.SessionToken[index] !=
                    sessionToken[index])
                {
                    return false;
                }
            }

            return true;
        }
    }
}