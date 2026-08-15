using System;
using NUnit.Framework;
using TopDownRoguelike.Networking.Protocol;

namespace TopDownRoguelike.Tests.EditMode
{
    public sealed class PacketCodecTests
    {
        [SetUp]
        public void SetUp()
        {
            PacketCodec.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            PacketCodec.Clear();
        }

        [Test]
        public void EncodeEmptyPayload_MatchesCppLayout()
        {
            var packet = PacketCodec.Encode(
                MessageType.ClientHello,
                Array.Empty<byte>());

            var expected = new byte[]
            {
                0x54, 0x44, 0x52, 0x31,
                0x00, 0x01,
                0x00, 0x01,
                0x00, 0x00, 0x00, 0x00
            };

            Assert.That(packet, Is.EqualTo(expected));
        }

        [Test]
        public void EncodePayload_WritesNetworkOrderAndPayload()
        {
            var payload = new byte[]
            {
                0xAA, 0xBB, 0xCC
            };

            var packet = PacketCodec.Encode(
                MessageType.SetNickname,
                payload);

            Assert.That(
                packet.Length,
                Is.EqualTo(PacketCodec.MessageHeaderSize + 3));

            Assert.That(packet[8], Is.EqualTo(0x00));
            Assert.That(packet[9], Is.EqualTo(0x00));
            Assert.That(packet[10], Is.EqualTo(0x00));
            Assert.That(packet[11], Is.EqualTo(0x03));

            Assert.That(packet[12], Is.EqualTo(0xAA));
            Assert.That(packet[13], Is.EqualTo(0xBB));
            Assert.That(packet[14], Is.EqualTo(0xCC));
        }

        [Test]
        public void EncodeThenDecode_ReturnsOriginalPacket()
        {
            var payload = new byte[]
            {
                1, 2, 3, 4
            };

            var encoded = PacketCodec.Encode(
                MessageType.SetNickname,
                payload);

            PacketCodec.Append(
                encoded,
                0,
                encoded.Length);

            Assert.That(
                PacketCodec.TryDecode(out var decoded),
                Is.True);

            Assert.That(
                decoded.Type,
                Is.EqualTo(MessageType.SetNickname));

            Assert.That(
                decoded.Payload,
                Is.EqualTo(payload));

            Assert.That(
                PacketCodec.TryDecode(out _),
                Is.False);
        }

        [Test]
        public void HeaderHalfPacket_WaitsForRemainingBytes()
        {
            var encoded = PacketCodec.Encode(
                MessageType.ClientHello,
                Array.Empty<byte>());

            PacketCodec.Append(encoded, 0, 5);

            Assert.That(
                PacketCodec.TryDecode(out _),
                Is.False);

            PacketCodec.Append(
                encoded,
                5,
                encoded.Length - 5);

            Assert.That(
                PacketCodec.TryDecode(out var decoded),
                Is.True);

            Assert.That(
                decoded.Type,
                Is.EqualTo(MessageType.ClientHello));
        }

        [Test]
        public void PayloadHalfPacket_WaitsForRemainingBytes()
        {
            var payload = new byte[]
            {
                1, 2, 3, 4, 5
            };

            var encoded = PacketCodec.Encode(
                MessageType.SetNickname,
                payload);

            var firstChunkSize =
                PacketCodec.MessageHeaderSize + 2;

            PacketCodec.Append(
                encoded,
                0,
                firstChunkSize);

            Assert.That(
                PacketCodec.TryDecode(out _),
                Is.False);

            PacketCodec.Append(
                encoded,
                firstChunkSize,
                encoded.Length - firstChunkSize);

            Assert.That(
                PacketCodec.TryDecode(out var decoded),
                Is.True);

            Assert.That(
                decoded.Payload,
                Is.EqualTo(payload));
        }

        [Test]
        public void DecodeAvailable_HandlesStickyPackets()
        {
            var first = PacketCodec.Encode(
                MessageType.ClientHello,
                Array.Empty<byte>());

            var second = PacketCodec.Encode(
                MessageType.SetReady,
                new byte[] { 1 });

            var combined = new byte[
                first.Length + second.Length];

            Buffer.BlockCopy(
                first, 0,
                combined, 0,
                first.Length);

            Buffer.BlockCopy(
                second, 0,
                combined, first.Length,
                second.Length);

            PacketCodec.Append(
                combined,
                0,
                combined.Length);

            var packets =
                PacketCodec.DecodeAvailable();

            Assert.That(packets.Count, Is.EqualTo(2));
            Assert.That(
                packets[0].Type,
                Is.EqualTo(MessageType.ClientHello));
            Assert.That(
                packets[1].Type,
                Is.EqualTo(MessageType.SetReady));
        }

        [Test]
        public void DecodeInvalidMagic_ReportsProtocolError()
        {
            var encoded = PacketCodec.Encode(
                MessageType.ClientHello,
                Array.Empty<byte>());

            encoded[PacketCodec.MagicOffset] = 0x00;

            PacketCodec.Append(
                encoded,
                0,
                encoded.Length);

            var exception = Assert.Throws<PacketDecodeException>(
                () => PacketCodec.TryDecode(out _));

            Assert.That(
                exception.Code,
                Is.EqualTo(PacketError.InvalidMagic));
        }

        [Test]
        public void DecodeOversizedPayload_ReportsProtocolError()
        {
            var encoded = PacketCodec.Encode(
                MessageType.ClientHello,
                Array.Empty<byte>());

            encoded[PacketCodec.PayloadSizeOffset] = 0x00;
            encoded[PacketCodec.PayloadSizeOffset + 1] = 0x01;
            encoded[PacketCodec.PayloadSizeOffset + 2] = 0x00;
            encoded[PacketCodec.PayloadSizeOffset + 3] = 0x01;

            PacketCodec.Append(
                encoded,
                0,
                encoded.Length);

            var exception = Assert.Throws<PacketDecodeException>(
                () => PacketCodec.TryDecode(out _));

            Assert.That(
                exception.Code,
                Is.EqualTo(PacketError.PayloadTooLarge));
        }
    }
}