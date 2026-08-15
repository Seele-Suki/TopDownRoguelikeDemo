using System;
using System.Collections.Generic;

namespace TopDownRoguelike.Networking.Protocol
{
    public enum PacketError : byte
    {
        InvalidMagic,
        UnsupportedVersion,
        UnknownMessageType,
        PayloadTooLarge,
        ReceiveBufferOverflow
    }

    public sealed class PacketDecodeException
        : Exception
    {
        public PacketDecodeException(
            PacketError code,
            string message)
            : base(message)
        {
            Code = code;
        }

        public PacketError Code { get; }
    }

    public static class PacketCodec
    {
        public const uint ProtocolMagic = 0x54445231;
        public const ushort ProtocolVersion = 1;

        public const int MagicOffset = 0;
        public const int VersionOffset = 4;
        public const int MessageTypeOffset = 6;
        public const int PayloadSizeOffset = 8;

        public const int MessageHeaderSize = 12;
        public const uint MaxPayloadSize = 64u * 1024u;
        public const int MaxPacketSize =
            MessageHeaderSize + (int)MaxPayloadSize;
        public const int MaxReceiveBufferSize =
            MaxPacketSize * 2;

        private static readonly List<byte> ReceiveBuffer =
            new List<byte>();

        public static void WriteNetworkUInt16(
            byte[] destination,
            int offset,
            ushort value)
        {
            ValidateWriteRange(
                destination,
                offset,
                sizeof(ushort));

            destination[offset] =
                (byte)(value >> 8);

            destination[offset + 1] =
                (byte)(value & 0xff);
        }

        public static void WriteNetworkUInt32(
            byte[] destination,
            int offset,
            uint value)
        {
            ValidateWriteRange(
                destination,
                offset,
                sizeof(uint));

            destination[offset] =
                (byte)(value >> 24);

            destination[offset + 1] =
                (byte)(value >> 16);

            destination[offset + 2] =
                (byte)(value >> 8);

            destination[offset + 3] =
                (byte)(value & 0xff);
        }

        public static ushort ReadNetworkUInt16(
            byte[] source,
            int offset)
        {
            ValidateReadRange(
                source,
                offset,
                sizeof(ushort));

            return (ushort)(
                (source[offset] << 8)
                | source[offset + 1]);
        }

        public static uint ReadNetworkUInt32(
            byte[] source,
            int offset)
        {
            ValidateReadRange(
                source,
                offset,
                sizeof(uint));

            return (uint)(
                (source[offset] << 24)
                | (source[offset + 1] << 16)
                | (source[offset + 2] << 8)
                | source[offset + 3]);
        }

        public static byte[] Encode(
            MessageType type,
            byte[] payload)
        {
            if (!IsKnownMessageType(type))
            {
                throw new ArgumentException(
                    "Unknown message type.",
                    nameof(type));
            }

            if (payload == null)
            {
                throw new ArgumentNullException(
                    nameof(payload));
            }

            if ((uint)payload.Length > MaxPayloadSize)
            {
                throw new ArgumentException(
                    "Payload exceeds the maximum allowed size.",
                    nameof(payload));
            }

            var packet = new byte[
                MessageHeaderSize + payload.Length];

            WriteNetworkUInt32(
                packet,
                MagicOffset,
                ProtocolMagic);

            WriteNetworkUInt16(
                packet,
                VersionOffset,
                ProtocolVersion);

            WriteNetworkUInt16(
                packet,
                MessageTypeOffset,
                (ushort)type);

            WriteNetworkUInt32(
                packet,
                PayloadSizeOffset,
                (uint)payload.Length);

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

        public static void Append(
            byte[] data,
            int offset,
            int count)
        {
            if (count == 0)
            {
                return;
            }

            if (data == null)
            {
                throw new ArgumentNullException(
                    nameof(data));
            }

            if (offset < 0
                || count < 0
                || offset > data.Length - count)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(offset));
            }

            if (count > MaxReceiveBufferSize
                - ReceiveBuffer.Count)
            {
                throw new PacketDecodeException(
                    PacketError.ReceiveBufferOverflow,
                    "TCP receive buffer exceeds the allowed size.");
            }

            for (var index = 0; index < count; index++)
            {
                ReceiveBuffer.Add(data[offset + index]);
            }
        }

        public static bool TryDecode(
            out DecodedPacket packet)
        {
            packet = default;

            if (ReceiveBuffer.Count < MessageHeaderSize)
            {
                return false;
            }

            var header = ReceiveBuffer.ToArray();

            var magic = ReadNetworkUInt32(
                header,
                MagicOffset);

            if (magic != ProtocolMagic)
            {
                throw new PacketDecodeException(
                    PacketError.InvalidMagic,
                    "Invalid protocol magic.");
            }

            var version = ReadNetworkUInt16(
                header,
                VersionOffset);

            if (version != ProtocolVersion)
            {
                throw new PacketDecodeException(
                    PacketError.UnsupportedVersion,
                    "Unsupported protocol version.");
            }

            var rawMessageType = ReadNetworkUInt16(
                header,
                MessageTypeOffset);

            var messageType =
                (MessageType)rawMessageType;

            if (!IsKnownMessageType(messageType))
            {
                throw new PacketDecodeException(
                    PacketError.UnknownMessageType,
                    "Unknown message type.");
            }

            var payloadSize = ReadNetworkUInt32(
                header,
                PayloadSizeOffset);

            if (payloadSize > MaxPayloadSize)
            {
                throw new PacketDecodeException(
                    PacketError.PayloadTooLarge,
                    "Received payload exceeds the maximum allowed size.");
            }

            var completePacketSize =
                MessageHeaderSize + (int)payloadSize;

            if (ReceiveBuffer.Count < completePacketSize)
            {
                return false;
            }

            var payload = ReceiveBuffer.GetRange(
                MessageHeaderSize,
                (int)payloadSize).ToArray();

            ReceiveBuffer.RemoveRange(
                0,
                completePacketSize);

            packet = new DecodedPacket(
                messageType,
                payload);

            return true;
        }

        public static List<DecodedPacket>
            DecodeAvailable()
        {
            var packets = new List<DecodedPacket>();

            while (TryDecode(out var packet))
            {
                packets.Add(packet);
            }

            return packets;
        }

        public static void Clear()
        {
            ReceiveBuffer.Clear();
        }

        private static bool IsKnownMessageType(
            MessageType type)
        {
            switch (type)
            {
                case MessageType.ClientHello:
                case MessageType.ServerHello:
                case MessageType.SetNickname:
                case MessageType.CreateRoomRequest:
                case MessageType.CreateRoomResponse:
                case MessageType.JoinRoomRequest:
                case MessageType.JoinRoomResponse:
                case MessageType.RoomStateSnapshot:
                case MessageType.SetPlayerSelection:
                case MessageType.SetReady:
                case MessageType.StartGameRequest:
                case MessageType.GameStarted:
                case MessageType.LeaveRoom:
                case MessageType.ErrorMessage:
                case MessageType.UdpBindRequest:
                case MessageType.UdpBindAccepted:
                case MessageType.UdpPing:
                case MessageType.UdpPong:
                    return true;

                case MessageType.Invalid:
                default:
                    return false;
            }
        }

        private static void ValidateWriteRange(
            byte[] destination,
            int offset,
            int byteCount)
        {
            if (destination == null)
            {
                throw new ArgumentNullException(
                    nameof(destination));
            }

            if (offset < 0
                || byteCount < 0
                || offset > destination.Length - byteCount)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(offset));
            }
        }

        private static void ValidateReadRange(
            byte[] source,
            int offset,
            int byteCount)
        {
            if (source == null)
            {
                throw new ArgumentNullException(
                    nameof(source));
            }

            if (offset < 0
                || byteCount < 0
                || offset > source.Length - byteCount)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(offset));
            }
        }
    }

    public readonly struct DecodedPacket
    {
        public DecodedPacket(
            MessageType type,
            byte[] payload)
        {
            Type = type;
            Payload = payload
                ?? throw new ArgumentNullException(
                    nameof(payload));
        }

        public MessageType Type { get; }

        public byte[] Payload { get; }
    }
}