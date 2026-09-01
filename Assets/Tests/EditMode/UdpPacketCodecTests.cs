using System;
using NUnit.Framework;
using TopDownRoguelike.Networking.Protocol;

namespace TopDownRoguelike.Tests.EditMode
{
    public sealed class UdpPacketCodecTests
    {
        [Test]
        public void PlayerInputWireValue_IsDefinedAndAcceptedAsUdp()
        {
            const ushort wireValue = 34;

            MessageType messageType =
                (MessageType)wireValue;

            Assert.That(
                Enum.GetName(
                    typeof(MessageType),
                    messageType),
                Is.EqualTo("PlayerInput"));

            var header =
                new UdpMessageHeader(
                    messageType,
                    CreateToken(),
                    7u,
                    9u);

            byte[] encoded =
                UdpPacketCodec.Encode(
                    header,
                    Array.Empty<byte>());

            DecodedUdpPacket decoded =
                UdpPacketCodec.Decode(encoded);

            Assert.That(
                decoded.Header.Type,
                Is.EqualTo(messageType));

            Assert.That(
                encoded[UdpPacketCodec.MessageTypeOffset],
                Is.EqualTo(0x00));

            Assert.That(
                encoded[
                    UdpPacketCodec.MessageTypeOffset + 1],
                Is.EqualTo(0x22));
        }

        [Test]
        public void PlayerStateSnapshotWireValue_IsDefinedAndAcceptedAsUdp()
        {
            const ushort wireValue = 35;

            MessageType messageType =
                (MessageType)wireValue;

            Assert.That(
                Enum.GetName(
                    typeof(MessageType),
                    messageType),
                Is.EqualTo("PlayerStateSnapshot"));

            var header =
                new UdpMessageHeader(
                    messageType,
                    CreateToken(),
                    7u,
                    10u);

            byte[] encoded =
                UdpPacketCodec.Encode(
                    header,
                    Array.Empty<byte>());

            DecodedUdpPacket decoded =
                UdpPacketCodec.Decode(encoded);

            Assert.That(
                decoded.Header.Type,
                Is.EqualTo(messageType));

            Assert.That(
                encoded[UdpPacketCodec.MessageTypeOffset],
                Is.EqualTo(0x00));

            Assert.That(
                encoded[
                    UdpPacketCodec.MessageTypeOffset + 1],
                Is.EqualTo(0x23));
        }

        [Test]
        public void WorldStateSnapshotWireValue_IsDefinedAndAcceptedAsUdp()
        {
            const ushort wireValue = 40;

            MessageType messageType =
                (MessageType)wireValue;

            Assert.That(
                Enum.GetName(
                    typeof(MessageType),
                    messageType),
                Is.EqualTo("WorldStateSnapshot"));

            var header =
                new UdpMessageHeader(
                    messageType,
                    CreateToken(),
                    1u,
                    11u);

            byte[] encoded =
                UdpPacketCodec.Encode(
                    header,
                    Array.Empty<byte>());

            DecodedUdpPacket decoded =
                UdpPacketCodec.Decode(encoded);

            Assert.That(
                decoded.Header.Type,
                Is.EqualTo(messageType));
        }

        [Test]
        public void EncodeUdpPing_MatchesCppLayout()
        {
            byte[] token = CreateToken();

            var header =
                new UdpMessageHeader(
                    MessageType.UdpPing,
                    token,
                    0x01020304u,
                    0xA1B2C3D4u);

            byte[] packet =
                UdpPacketCodec.Encode(
                    header,
                    Array.Empty<byte>());

            var expected =
                new byte[]
                {
                    0x54, 0x44, 0x52, 0x55,
                    0x00, 0x01,
                    0x00, 0x20,

                    0x00, 0x01, 0x02, 0x03,
                    0x04, 0x05, 0x06, 0x07,
                    0x08, 0x09, 0x0A, 0x0B,
                    0x0C, 0x0D, 0x0E, 0x0F,

                    0x01, 0x02, 0x03, 0x04,
                    0xA1, 0xB2, 0xC3, 0xD4
                };

            Assert.That(
                packet,
                Is.EqualTo(expected));
        }

        [Test]
        public void EncodeThenDecode_PreservesHeaderAndPayload()
        {
            byte[] token = CreateToken();

            var header =
                new UdpMessageHeader(
                    MessageType.UdpPong,
                    token,
                    42u,
                    0xFFFFFFFEu);

            var payload =
                new byte[]
                {
                    0xAA,
                    0xBB,
                    0xCC
                };

            byte[] encoded =
                UdpPacketCodec.Encode(
                    header,
                    payload);

            DecodedUdpPacket decoded =
                UdpPacketCodec.Decode(encoded);

            Assert.That(
                decoded.Header.Type,
                Is.EqualTo(MessageType.UdpPong));

            Assert.That(
                decoded.Header.SessionToken,
                Is.EqualTo(token));

            Assert.That(
                decoded.Header.PlayerId,
                Is.EqualTo(42u));

            Assert.That(
                decoded.Header.Sequence,
                Is.EqualTo(0xFFFFFFFEu));

            Assert.That(
                decoded.Payload,
                Is.EqualTo(payload));
        }

        [Test]
        public void DecodeInvalidMagic_ReportsProtocolError()
        {
            byte[] encoded =
                CreateValidPacket();

            encoded[UdpPacketCodec.MagicOffset] =
                0x00;

            var exception =
                Assert.Throws<UdpPacketDecodeException>(
                    () =>
                        UdpPacketCodec.Decode(encoded));

            Assert.That(
                exception.Code,
                Is.EqualTo(
                    UdpPacketError.InvalidMagic));
        }

        [Test]
        public void DecodeTcpMessageType_ReportsProtocolError()
        {
            byte[] encoded =
                CreateValidPacket();

            PacketCodec.WriteNetworkUInt16(
                encoded,
                UdpPacketCodec.MessageTypeOffset,
                (ushort)MessageType.ClientHello);

            var exception =
                Assert.Throws<UdpPacketDecodeException>(
                    () =>
                        UdpPacketCodec.Decode(encoded));

            Assert.That(
                exception.Code,
                Is.EqualTo(
                    UdpPacketError.NonUdpMessageType));
        }

        [Test]
        public void DecodeShortDatagram_RejectsInput()
        {
            var datagram =
                new byte[
                    UdpPacketCodec.MessageHeaderSize - 1];

            Assert.Throws<ArgumentException>(
                () =>
                    UdpPacketCodec.Decode(datagram));
        }

        private static byte[] CreateValidPacket()
        {
            var header =
                new UdpMessageHeader(
                    MessageType.UdpPing,
                    CreateToken(),
                    1u,
                    2u);

            return UdpPacketCodec.Encode(
                header,
                Array.Empty<byte>());
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
