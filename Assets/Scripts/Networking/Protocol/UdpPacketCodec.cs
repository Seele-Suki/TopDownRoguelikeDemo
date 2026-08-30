using System;

namespace TopDownRoguelike.Networking.Protocol
{
    public enum UdpPacketError : byte
    {
        InvalidMagic,
        UnsupportedVersion,
        UnknownMessageType,
        NonUdpMessageType
    }

    public sealed class UdpPacketDecodeException
        : Exception
    {
        public UdpPacketDecodeException(
            UdpPacketError code,
            string message)
            : base(message)
        {
            Code = code;
        }

        public UdpPacketError Code { get; }
    }

    public readonly struct UdpMessageHeader
    {
        public UdpMessageHeader(
            MessageType type,
            byte[] sessionToken,
            uint playerId,
            uint sequence)
        {
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

            Type = type;

            SessionToken =
                (byte[])sessionToken.Clone();

            PlayerId = playerId;
            Sequence = sequence;
        }

        public MessageType Type { get; }

        public byte[] SessionToken { get; }

        public uint PlayerId { get; }

        public uint Sequence { get; }
    }

    public readonly struct DecodedUdpPacket
    {
        public DecodedUdpPacket(
            UdpMessageHeader header,
            byte[] payload)
        {
            Header = header;

            Payload = payload
                ?? throw new ArgumentNullException(
                    nameof(payload));
        }

        public UdpMessageHeader Header { get; }

        public byte[] Payload { get; }
    }

    public static class UdpPacketCodec
    {
        public const uint ProtocolMagic =
            0x54445255u;

        public const ushort ProtocolVersion =
            1;

        public const int MagicOffset =
            0;

        public const int VersionOffset =
            4;

        public const int MessageTypeOffset =
            6;

        public const int SessionTokenOffset =
            8;

        public const int SessionTokenSize =
            16;

        public const int PlayerIdOffset =
            24;

        public const int SequenceOffset =
            28;

        public const int MessageHeaderSize =
            32;

        public static byte[] Encode(
            UdpMessageHeader header,
            byte[] payload)
        {
            if (payload == null)
            {
                throw new ArgumentNullException(
                    nameof(payload));
            }

            ValidateUdpMessageType(
                header.Type,
                nameof(header));

            if (header.SessionToken == null ||
                header.SessionToken.Length !=
                SessionTokenSize)
            {
                throw new ArgumentException(
                    "UDP session token must contain 16 bytes.",
                    nameof(header));
            }

            var packet =
                new byte[
                    MessageHeaderSize +
                    payload.Length];

            PacketCodec.WriteNetworkUInt32(
                packet,
                MagicOffset,
                ProtocolMagic);

            PacketCodec.WriteNetworkUInt16(
                packet,
                VersionOffset,
                ProtocolVersion);

            PacketCodec.WriteNetworkUInt16(
                packet,
                MessageTypeOffset,
                (ushort)header.Type);

            Buffer.BlockCopy(
                header.SessionToken,
                0,
                packet,
                SessionTokenOffset,
                SessionTokenSize);

            PacketCodec.WriteNetworkUInt32(
                packet,
                PlayerIdOffset,
                header.PlayerId);

            PacketCodec.WriteNetworkUInt32(
                packet,
                SequenceOffset,
                header.Sequence);

            if (payload.Length > 0)
            {
                Buffer.BlockCopy(
                    payload,
                    0,
                    packet,
                    MessageHeaderSize,
                    payload.Length);
            }

            return packet;
        }

        public static DecodedUdpPacket Decode(
            byte[] datagram)
        {
            if (datagram == null)
            {
                throw new ArgumentNullException(
                    nameof(datagram));
            }

            if (datagram.Length <
                MessageHeaderSize)
            {
                throw new ArgumentException(
                    "UDP datagram is smaller than its header.",
                    nameof(datagram));
            }

            uint magic =
                PacketCodec.ReadNetworkUInt32(
                    datagram,
                    MagicOffset);

            if (magic != ProtocolMagic)
            {
                throw new UdpPacketDecodeException(
                    UdpPacketError.InvalidMagic,
                    "Invalid UDP protocol magic.");
            }

            ushort version =
                PacketCodec.ReadNetworkUInt16(
                    datagram,
                    VersionOffset);

            if (version != ProtocolVersion)
            {
                throw new UdpPacketDecodeException(
                    UdpPacketError.UnsupportedVersion,
                    "Unsupported UDP protocol version.");
            }

            var messageType =
                (MessageType)
                PacketCodec.ReadNetworkUInt16(
                    datagram,
                    MessageTypeOffset);

            if (!Enum.IsDefined(
                typeof(MessageType),
                messageType))
            {
                throw new UdpPacketDecodeException(
                    UdpPacketError.UnknownMessageType,
                    "Unknown UDP message type.");
            }

            if (!IsUdpMessageType(messageType))
            {
                throw new UdpPacketDecodeException(
                    UdpPacketError.NonUdpMessageType,
                    "Message type is not valid for UDP.");
            }

            var sessionToken =
                new byte[SessionTokenSize];

            Buffer.BlockCopy(
                datagram,
                SessionTokenOffset,
                sessionToken,
                0,
                SessionTokenSize);

            uint playerId =
                PacketCodec.ReadNetworkUInt32(
                    datagram,
                    PlayerIdOffset);

            uint sequence =
                PacketCodec.ReadNetworkUInt32(
                    datagram,
                    SequenceOffset);

            int payloadSize =
                datagram.Length -
                MessageHeaderSize;

            var payload =
                new byte[payloadSize];

            if (payloadSize > 0)
            {
                Buffer.BlockCopy(
                    datagram,
                    MessageHeaderSize,
                    payload,
                    0,
                    payloadSize);
            }

            var header =
                new UdpMessageHeader(
                    messageType,
                    sessionToken,
                    playerId,
                    sequence);

            return new DecodedUdpPacket(
                header,
                payload);
        }

        private static void ValidateUdpMessageType(
            MessageType messageType,
            string parameterName)
        {
            if (!Enum.IsDefined(
                typeof(MessageType),
                messageType))
            {
                throw new ArgumentException(
                    "Unknown UDP message type.",
                    parameterName);
            }

            if (!IsUdpMessageType(messageType))
            {
                throw new ArgumentException(
                    "Message type is not valid for UDP.",
                    parameterName);
            }
        }

        private static bool IsUdpMessageType(
            MessageType messageType)
        {
            switch (messageType)
            {
                case MessageType.UdpBindRequest:
                case MessageType.UdpBindAccepted:
                case MessageType.UdpPing:
                case MessageType.UdpPong:
                case MessageType.PlayerInput:
                case MessageType.PlayerStateSnapshot:
                case MessageType.PlayerShotEvent:
                case MessageType.PlayerShotgunEvent:
                    return true;

                default:
                    return false;
            }
        }
    }
}