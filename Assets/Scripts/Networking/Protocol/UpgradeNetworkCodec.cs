using System;
using System.Collections.Generic;

namespace TopDownRoguelike.Networking.Protocol
{
    public sealed class UpgradeStartedPayload
    {
        public UpgradeStartedPayload(
            uint sequence,
            IReadOnlyList<ushort> upgradeIds)
        {
            if (sequence == 0u || upgradeIds == null ||
                upgradeIds.Count < 1 || upgradeIds.Count > 3)
            {
                throw new ArgumentException(
                    "Upgrade start payload is invalid.");
            }

            Sequence = sequence;
            UpgradeIds = new List<ushort>(upgradeIds);
        }

        public uint Sequence { get; }
        public IReadOnlyList<ushort> UpgradeIds { get; }
    }

    public sealed class UpgradeChoicePayload
    {
        public UpgradeChoicePayload(uint sequence, ushort upgradeId)
        {
            if (sequence == 0u || upgradeId == 0)
            {
                throw new ArgumentException(
                    "Upgrade choice payload is invalid.");
            }

            Sequence = sequence;
            UpgradeId = upgradeId;
        }

        public uint Sequence { get; }
        public ushort UpgradeId { get; }
    }

    public sealed class UpgradeCompletedPayload
    {
        public UpgradeCompletedPayload(
            uint sequence,
            IReadOnlyDictionary<uint, ushort> choices)
        {
            if (sequence == 0u || choices == null || choices.Count != 2)
            {
                throw new ArgumentException(
                    "Upgrade completion payload is invalid.");
            }

            Sequence = sequence;
            Choices = new Dictionary<uint, ushort>(choices);
        }

        public uint Sequence { get; }
        public IReadOnlyDictionary<uint, ushort> Choices { get; }
    }

    public static class UpgradeNetworkCodec
    {
        public static byte[] EncodeCompleted(
            UpgradeCompletedPayload payload)
        {
            if (payload == null) throw new ArgumentNullException(nameof(payload));
            var bytes = new byte[16];
            WriteUInt32(bytes, 0, payload.Sequence);
            int offset = 4;
            foreach (KeyValuePair<uint, ushort> choice in payload.Choices)
            {
                WriteUInt32(bytes, offset, choice.Key);
                WriteUInt16(bytes, offset + 4, choice.Value);
                offset += 6;
            }
            return bytes;
        }

        public static UpgradeCompletedPayload DecodeCompleted(byte[] bytes)
        {
            if (bytes == null || bytes.Length != 16)
                throw new ArgumentException("Upgrade completion payload length is invalid.");
            var choices = new Dictionary<uint, ushort>();
            for (int offset = 4; offset < 16; offset += 6)
            {
                uint playerId = ReadUInt32(bytes, offset);
                ushort upgradeId = ReadUInt16(bytes, offset + 4);
                if (playerId == 0u || upgradeId == 0 || !choices.TryAdd(playerId, upgradeId))
                    throw new ArgumentException("Upgrade completion choices are invalid.");
            }
            return new UpgradeCompletedPayload(ReadUInt32(bytes, 0), choices);
        }

        public static byte[] EncodeStarted(
            UpgradeStartedPayload payload)
        {
            if (payload == null) throw new ArgumentNullException(nameof(payload));
            var bytes = new byte[5 + payload.UpgradeIds.Count * 2];
            WriteUInt32(bytes, 0, payload.Sequence);
            bytes[4] = (byte)payload.UpgradeIds.Count;
            for (int i = 0; i < payload.UpgradeIds.Count; i++)
            {
                WriteUInt16(bytes, 5 + i * 2, payload.UpgradeIds[i]);
            }
            return bytes;
        }

        public static UpgradeStartedPayload DecodeStarted(byte[] bytes)
        {
            if (bytes == null || bytes.Length < 7 || bytes.Length > 11)
                throw new ArgumentException("Upgrade start payload length is invalid.");
            int count = bytes[4];
            if (count < 1 || count > 3 || bytes.Length != 5 + count * 2)
                throw new ArgumentException("Upgrade start option count is invalid.");
            var ids = new List<ushort>(count);
            for (int i = 0; i < count; i++) ids.Add(ReadUInt16(bytes, 5 + i * 2));
            return new UpgradeStartedPayload(ReadUInt32(bytes, 0), ids);
        }

        public static byte[] EncodeChoice(UpgradeChoicePayload payload)
        {
            if (payload == null) throw new ArgumentNullException(nameof(payload));
            var bytes = new byte[6];
            WriteUInt32(bytes, 0, payload.Sequence);
            WriteUInt16(bytes, 4, payload.UpgradeId);
            return bytes;
        }

        public static UpgradeChoicePayload DecodeChoice(byte[] bytes)
        {
            if (bytes == null || bytes.Length != 6)
                throw new ArgumentException("Upgrade choice payload length is invalid.");
            return new UpgradeChoicePayload(ReadUInt32(bytes, 0), ReadUInt16(bytes, 4));
        }

        private static void WriteUInt16(byte[] bytes, int offset, ushort value)
        {
            bytes[offset] = (byte)(value >> 8);
            bytes[offset + 1] = (byte)value;
        }

        private static void WriteUInt32(byte[] bytes, int offset, uint value)
        {
            bytes[offset] = (byte)(value >> 24);
            bytes[offset + 1] = (byte)(value >> 16);
            bytes[offset + 2] = (byte)(value >> 8);
            bytes[offset + 3] = (byte)value;
        }

        private static ushort ReadUInt16(byte[] bytes, int offset) =>
            (ushort)((bytes[offset] << 8) | bytes[offset + 1]);

        private static uint ReadUInt32(byte[] bytes, int offset) =>
            ((uint)bytes[offset] << 24) |
            ((uint)bytes[offset + 1] << 16) |
            ((uint)bytes[offset + 2] << 8) |
            bytes[offset + 3];
    }
}
