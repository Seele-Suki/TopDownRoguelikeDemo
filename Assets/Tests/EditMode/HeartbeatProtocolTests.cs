using NUnit.Framework;
using TopDownRoguelike.Networking.Client;
using TopDownRoguelike.Networking.Protocol;

namespace TopDownRoguelike.Tests.EditMode
{
    public sealed class HeartbeatProtocolTests
    {
        [Test]
        public void TcpHeartbeatMessageTypes_UseStableValues()
        {
            Assert.That(
                (ushort)MessageType.TcpHeartbeatRequest,
                Is.EqualTo(21));
            Assert.That(
                (ushort)MessageType.TcpHeartbeatResponse,
                Is.EqualTo(22));
        }

        [Test]
        public void TcpHeartbeatMessages_AreKnownTcpMessages()
        {
            byte[] request =
                PacketCodec.Encode(
                    MessageType.TcpHeartbeatRequest,
                    System.Array.Empty<byte>());

            byte[] response =
                PacketCodec.Encode(
                    MessageType.TcpHeartbeatResponse,
                    System.Array.Empty<byte>());

            Assert.That(request.Length, Is.EqualTo(PacketCodec.MessageHeaderSize));
            Assert.That(response.Length, Is.EqualTo(PacketCodec.MessageHeaderSize));
        }

        [Test]
        public void HeartbeatTiming_UsesTwoSecondIntervalAndSixSecondTimeout()
        {
            Assert.That(
                HeartbeatTiming.IntervalSeconds,
                Is.EqualTo(2f));
            Assert.That(
                HeartbeatTiming.TimeoutSeconds,
                Is.EqualTo(6f));
        }
    }
}
