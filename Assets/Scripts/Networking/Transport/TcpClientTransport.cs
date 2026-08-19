using System;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Concurrent;
using TopDownRoguelike.Networking.Protocol;

namespace TopDownRoguelike.Networking.Transport
{
    public sealed class TcpClientTransport
    {
        private readonly
            MainThreadMessageQueue<NetworkTransportEvent>
            messageQueue;

        private readonly ConcurrentQueue<byte[]>
            outgoingPackets =
            new ConcurrentQueue<byte[]>();

        private readonly ManualResetEventSlim stopSignal =
            new ManualResetEventSlim(false);

        private volatile Thread workerThread;
        private volatile TcpClient tcpClient;

        public TcpClientTransport(
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

        public void Start(string address, int port)
        {
            if (workerThread != null)
            {
                throw new InvalidOperationException(
                    "TCP transport has already started.");
            }

            stopSignal.Reset();

            workerThread = new Thread(
                () => Run(address, port))
            {
                IsBackground = true,
                Name = "TCP Client Transport"
            };

            workerThread.Start();
        }

        public void Send(
            MessageType messageType,
            byte[] payload)
        {
            if (workerThread == null ||
                stopSignal.IsSet)
            {
                throw new InvalidOperationException(
                    "TCP transport is not running.");
            }

            byte[] encodedPacket =
                PacketCodec.Encode(
                    messageType,
                    payload);

            outgoingPackets.Enqueue(
                encodedPacket);
        }

        public void Stop()
        {
            stopSignal.Set();
            tcpClient?.Close();

            Thread thread = workerThread;

            if (thread != null &&
                thread != Thread.CurrentThread &&
                !thread.Join(1000))
            {
                throw new TimeoutException(
                    "TCP worker thread did not stop.");
            }

            while (outgoingPackets.TryDequeue(
                out _))
            {
            }

            workerThread = null;
            tcpClient = null;
        }

        private void Run(string address, int port)
        {
            TcpClient client = null;

            try
            {
                if (stopSignal.IsSet)
                {
                    return;
                }

                client = new TcpClient(
                    AddressFamily.InterNetworkV6);

                tcpClient = client;

                if (stopSignal.IsSet)
                {
                    return;
                }

                client.Connect(address, port);

                if (stopSignal.IsSet)
                {
                    return;
                }

                messageQueue.Enqueue(
                    NetworkTransportEvent.Connected(
                        NetworkTransportKind.Tcp));

                NetworkStream stream =
                    client.GetStream();

                var receiveBuffer =
                    new byte[8192];

                var packetCodec =
                    new PacketCodec();

                while (!stopSignal.IsSet)
                {
                    SendQueuedPackets(stream);

                    if (!client.Client.Poll(
                        10000,
                        SelectMode.SelectRead))
                    {
                        continue;
                    }

                    int bytesRead =
                        stream.Read(
                            receiveBuffer,
                            0,
                            receiveBuffer.Length);

                    if (bytesRead == 0)
                    {
                        if (!stopSignal.IsSet)
                        {
                            messageQueue.Enqueue(
                                NetworkTransportEvent.Disconnected(
                                    NetworkTransportKind.Tcp));
                        }

                        break;
                    }

                    packetCodec.Append(
                        receiveBuffer,
                        0,
                        bytesRead);

                    foreach (var packet
                        in packetCodec.DecodeAvailable())
                    {
                        messageQueue.Enqueue(
                            NetworkTransportEvent.PacketReceived(
                                NetworkTransportKind.Tcp,
                                packet.Type,
                                packet.Payload));
                    }
                }
            }
            catch (Exception exception)
            {
                if (!stopSignal.IsSet)
                {
                    messageQueue.Enqueue(
                        NetworkTransportEvent.Error(
                            NetworkTransportKind.Tcp,
                            exception.Message));
                }
            }
            finally
            {
                client?.Close();
                tcpClient = null;
            }
        }

        private void SendQueuedPackets(
            NetworkStream stream)
        {
            while (outgoingPackets.TryDequeue(
                out byte[] packet))
            {
                stream.Write(
                    packet,
                    0,
                    packet.Length);
            }
        }
    }
}