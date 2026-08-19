using NUnit.Framework;
using TopDownRoguelike.Networking.Protocol;
using TopDownRoguelike.Networking.Transport;

namespace TopDownRoguelike.Tests.EditMode
{
    public sealed class NetworkTransportEventTests
    {
        [Test]
        public void Connected_CreatesConnectedEvent()
        {
            NetworkTransportEvent transportEvent =
                NetworkTransportEvent.Connected(
                    NetworkTransportKind.Tcp);

            Assert.That(
                transportEvent.TransportKind,
                Is.EqualTo(NetworkTransportKind.Tcp));

            Assert.That(
                transportEvent.EventType,
                Is.EqualTo(NetworkTransportEventType.Connected));
        }

        [Test]
        public void PacketReceived_PreservesPacketData()
        {
            byte[] payload = { 1, 2, 3 };

            NetworkTransportEvent transportEvent =
                NetworkTransportEvent.PacketReceived(
                    NetworkTransportKind.Tcp,
                    MessageType.ServerHello,
                    payload);

            Assert.That(
                transportEvent.EventType,
                Is.EqualTo(NetworkTransportEventType.PacketReceived));

            Assert.That(
                transportEvent.PacketType,
                Is.EqualTo(MessageType.ServerHello));

            Assert.That(
                transportEvent.Payload,
                Is.EqualTo(payload));
        }

        [Test]
        public void Disconnected_CreatesDisconnectedEvent()
        {
            NetworkTransportEvent transportEvent =
                NetworkTransportEvent.Disconnected(
                    NetworkTransportKind.Tcp);

            Assert.That(
                transportEvent.EventType,
                Is.EqualTo(NetworkTransportEventType.Disconnected));
        }

        [Test]
        public void Error_PreservesErrorMessage()
        {
            NetworkTransportEvent transportEvent =
                NetworkTransportEvent.Error(
                    NetworkTransportKind.Udp,
                    "UDP receive failed.");

            Assert.That(
                transportEvent.EventType,
                Is.EqualTo(NetworkTransportEventType.Error));

            Assert.That(
                transportEvent.ErrorMessage,
                Is.EqualTo("UDP receive failed."));
        }

        [Test]
        public void UdpPacketReceived_PreservesSequence()
        {
            byte[] payload = { 1, 2, 3 };

            NetworkTransportEvent transportEvent =
                NetworkTransportEvent.UdpPacketReceived(
                    MessageType.UdpPong,
                    42u,
                    payload);

            Assert.That(
                transportEvent.TransportKind,
                Is.EqualTo(NetworkTransportKind.Udp));

            Assert.That(
                transportEvent.EventType,
                Is.EqualTo(
                    NetworkTransportEventType.PacketReceived));

            Assert.That(
                transportEvent.PacketType,
                Is.EqualTo(MessageType.UdpPong));

            Assert.That(
                transportEvent.Sequence,
                Is.EqualTo(42u));

            Assert.That(
                transportEvent.Payload,
                Is.EqualTo(payload));
        }
    }
}