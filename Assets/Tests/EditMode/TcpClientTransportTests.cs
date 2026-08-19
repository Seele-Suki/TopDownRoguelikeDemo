using System.Net;
using System.Net.Sockets;
using System.Threading;
using NUnit.Framework;
using TopDownRoguelike.Networking.Transport;
using TopDownRoguelike.Networking.Protocol;

namespace TopDownRoguelike.Tests.EditMode
{
    public sealed class TcpClientTransportTests
    {
        [Test]
        public void ConnectToIpv6Loopback_EnqueuesConnectedEvent()
        {
            var listener =
                new TcpListener(IPAddress.IPv6Loopback, 0);

            listener.Start();

            int port =
                ((IPEndPoint)listener.LocalEndpoint).Port;

            var queue =
                new MainThreadMessageQueue<NetworkTransportEvent>();

            var transport = new TcpClientTransport(queue);

            try
            {
                var acceptTask =
                    listener.AcceptTcpClientAsync();

                transport.Start("::1", port);

                NetworkTransportEvent transportEvent = null;

                bool received = SpinWait.SpinUntil(
                    () => queue.TryDequeue(out transportEvent),
                    2000);

                Assert.That(received, Is.True);
                Assert.That(
                    transportEvent.EventType,
                    Is.EqualTo(NetworkTransportEventType.Connected));

                Assert.That(acceptTask.Wait(2000), Is.True);

                using (TcpClient acceptedClient =
                    acceptTask.Result)
                {
                    Assert.That(
                        acceptedClient.Connected,
                        Is.True);
                }
            }
            finally
            {
                transport.Stop();
                listener.Stop();
            }

            Assert.That(transport.IsRunning, Is.False);
        }

        [Test]
        public void ReceivedTcpPacket_IsEnqueuedAsPacketReceived()
        {
            var listener =
                new TcpListener(IPAddress.IPv6Loopback, 0);

            listener.Start();

            int port =
                ((IPEndPoint)listener.LocalEndpoint).Port;

            var queue =
                new MainThreadMessageQueue<NetworkTransportEvent>();

            var transport =
                new TcpClientTransport(queue);

            TcpClient acceptedClient = null;

            try
            {
                var acceptTask =
                    listener.AcceptTcpClientAsync();

                transport.Start("::1", port);

                Assert.That(
                    acceptTask.Wait(2000),
                    Is.True);

                acceptedClient =
                    acceptTask.Result;

                var expectedPayload =
                    new byte[]
                    {
                0x11,
                0x22,
                0x33
                    };

                var encodedPacket =
                    PacketCodec.Encode(
                        MessageType.ServerHello,
                        expectedPayload);

                NetworkStream stream =
                    acceptedClient.GetStream();

                stream.Write(
                    encodedPacket,
                    0,
                    encodedPacket.Length);

                NetworkTransportEvent receivedEvent =
                    null;

                bool received =
                    SpinWait.SpinUntil(
                        () =>
                        {
                            while (queue.TryDequeue(
                                out var queuedEvent))
                            {
                                if (queuedEvent.EventType ==
                                    NetworkTransportEventType.PacketReceived)
                                {
                                    receivedEvent =
                                        queuedEvent;

                                    return true;
                                }
                            }

                            return false;
                        },
                        2000);

                Assert.That(
                    received,
                    Is.True);

                Assert.That(
                    receivedEvent.TransportKind,
                    Is.EqualTo(NetworkTransportKind.Tcp));

                Assert.That(
                    receivedEvent.EventType,
                    Is.EqualTo(
                        NetworkTransportEventType.PacketReceived));

                Assert.That(
                    receivedEvent.PacketType,
                    Is.EqualTo(MessageType.ServerHello));

                Assert.That(
                    receivedEvent.Payload,
                    Is.EqualTo(expectedPayload));
            }
            finally
            {
                transport.Stop();
                acceptedClient?.Close();
                listener.Stop();
            }

            Assert.That(
                transport.IsRunning,
                Is.False);
        }

        [Test]
        public void SendPacket_WritesEncodedPacketToServer()
        {
            var listener =
                new TcpListener(IPAddress.IPv6Loopback, 0);

            listener.Start();

            int port =
                ((IPEndPoint)listener.LocalEndpoint).Port;

            var queue =
                new MainThreadMessageQueue<NetworkTransportEvent>();

            var transport =
                new TcpClientTransport(queue);

            TcpClient acceptedClient = null;

            try
            {
                var acceptTask =
                    listener.AcceptTcpClientAsync();

                transport.Start("::1", port);

                Assert.That(
                    acceptTask.Wait(2000),
                    Is.True);

                acceptedClient =
                    acceptTask.Result;

                var payload =
                    new byte[]
                    {
                0x01
                    };

                var expectedPacket =
                    PacketCodec.Encode(
                        MessageType.SetReady,
                        payload);

                transport.Send(
                    MessageType.SetReady,
                    payload);

                NetworkStream stream =
                    acceptedClient.GetStream();

                stream.ReadTimeout = 2000;

                var receivedBytes =
                    new byte[expectedPacket.Length];

                int totalBytesRead = 0;

                while (totalBytesRead <
                    receivedBytes.Length)
                {
                    int bytesRead =
                        stream.Read(
                            receivedBytes,
                            totalBytesRead,
                            receivedBytes.Length -
                            totalBytesRead);

                    if (bytesRead == 0)
                    {
                        break;
                    }

                    totalBytesRead += bytesRead;
                }

                Assert.That(
                    totalBytesRead,
                    Is.EqualTo(expectedPacket.Length));

                Assert.That(
                    receivedBytes,
                    Is.EqualTo(expectedPacket));
            }
            finally
            {
                transport.Stop();
                acceptedClient?.Close();
                listener.Stop();
            }

            Assert.That(
                transport.IsRunning,
                Is.False);
        }
    }
}