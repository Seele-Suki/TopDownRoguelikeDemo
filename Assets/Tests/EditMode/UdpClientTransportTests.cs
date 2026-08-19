using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using NUnit.Framework;
using TopDownRoguelike.Networking.Protocol;
using TopDownRoguelike.Networking.Transport;

namespace TopDownRoguelike.Tests.EditMode
{
    public sealed class UdpClientTransportTests
    {
        [Test]
        public void SendUdpPing_WritesEncodedDatagramToServer()
        {
            UdpClient server =
                CreateServer(out int port);

            var queue =
                new MainThreadMessageQueue<
                    NetworkTransportEvent>();

            var transport =
                new UdpClientTransport(queue);

            byte[] token = CreateToken();

            try
            {
                transport.Start(
                    "::1",
                    port,
                    7u,
                    token);

                WaitForEvent(
                    queue,
                    NetworkTransportEventType.Connected);

                transport.Send(
                    MessageType.UdpPing,
                    41u,
                    Array.Empty<byte>());

                var remoteEndpoint =
                    new IPEndPoint(
                        IPAddress.IPv6Any,
                        0);

                byte[] datagram =
                    server.Receive(
                        ref remoteEndpoint);

                DecodedUdpPacket decoded =
                    UdpPacketCodec.Decode(datagram);

                Assert.That(
                    decoded.Header.Type,
                    Is.EqualTo(MessageType.UdpPing));

                Assert.That(
                    decoded.Header.SessionToken,
                    Is.EqualTo(token));

                Assert.That(
                    decoded.Header.PlayerId,
                    Is.EqualTo(7u));

                Assert.That(
                    decoded.Header.Sequence,
                    Is.EqualTo(41u));

                Assert.That(
                    decoded.Payload,
                    Is.Empty);
            }
            finally
            {
                transport.Stop();
                server.Close();
            }

            Assert.That(
                transport.IsRunning,
                Is.False);
        }

        [Test]
        public void ReceivedUdpPong_IsEnqueuedWithSequence()
        {
            UdpClient server =
                CreateServer(out int port);

            var queue =
                new MainThreadMessageQueue<
                    NetworkTransportEvent>();

            var transport =
                new UdpClientTransport(queue);

            byte[] token = CreateToken();

            try
            {
                transport.Start(
                    "::1",
                    port,
                    7u,
                    token);

                WaitForEvent(
                    queue,
                    NetworkTransportEventType.Connected);

                transport.Send(
                    MessageType.UdpPing,
                    42u,
                    Array.Empty<byte>());

                var clientEndpoint =
                    new IPEndPoint(
                        IPAddress.IPv6Any,
                        0);

                server.Receive(
                    ref clientEndpoint);

                var pongHeader =
                    new UdpMessageHeader(
                        MessageType.UdpPong,
                        token,
                        7u,
                        42u);

                byte[] pong =
                    UdpPacketCodec.Encode(
                        pongHeader,
                        Array.Empty<byte>());

                server.Send(
                    pong,
                    pong.Length,
                    clientEndpoint);

                NetworkTransportEvent transportEvent =
                    WaitForEvent(
                        queue,
                        NetworkTransportEventType.PacketReceived);

                Assert.That(
                    transportEvent.TransportKind,
                    Is.EqualTo(NetworkTransportKind.Udp));

                Assert.That(
                    transportEvent.PacketType,
                    Is.EqualTo(MessageType.UdpPong));

                Assert.That(
                    transportEvent.Sequence,
                    Is.EqualTo(42u));

                Assert.That(
                    transportEvent.Payload,
                    Is.Empty);
            }
            finally
            {
                transport.Stop();
                server.Close();
            }

            Assert.That(
                transport.IsRunning,
                Is.False);
        }

        private static UdpClient CreateServer(
            out int port)
        {
            var server =
                new UdpClient(
                    AddressFamily.InterNetworkV6);

            server.Client.DualMode = true;

            server.Client.Bind(
                new IPEndPoint(
                    IPAddress.IPv6Loopback,
                    0));

            server.Client.ReceiveTimeout =
                2000;

            port =
                ((IPEndPoint)
                    server.Client.LocalEndPoint).Port;

            return server;
        }

        private static NetworkTransportEvent
            WaitForEvent(
                MainThreadMessageQueue<
                    NetworkTransportEvent> queue,
                NetworkTransportEventType eventType)
        {
            NetworkTransportEvent result =
                null;

            bool received =
                SpinWait.SpinUntil(
                    () =>
                    {
                        while (queue.TryDequeue(
                            out var transportEvent))
                        {
                            if (transportEvent.EventType ==
                                eventType)
                            {
                                result =
                                    transportEvent;

                                return true;
                            }
                        }

                        return false;
                    },
                    2000);

            Assert.That(
                received,
                Is.True,
                $"Timed out waiting for {eventType}.");

            return result;
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