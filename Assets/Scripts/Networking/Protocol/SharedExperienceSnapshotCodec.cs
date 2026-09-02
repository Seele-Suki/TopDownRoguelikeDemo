using System;

namespace TopDownRoguelike.Networking.Protocol
{
    public sealed class SharedExperienceSnapshotPayload
    {
        public SharedExperienceSnapshotPayload(
            uint sequence,
            int currentLevel,
            int currentExperience,
            int experienceToNextLevel)
        {
            if (sequence == 0u || currentLevel < 1 ||
                currentExperience < 0 || experienceToNextLevel <= 0 ||
                currentExperience >= experienceToNextLevel)
            {
                throw new ArgumentOutOfRangeException(nameof(sequence));
            }

            Sequence = sequence;
            CurrentLevel = currentLevel;
            CurrentExperience = currentExperience;
            ExperienceToNextLevel = experienceToNextLevel;
        }

        public uint Sequence { get; }
        public int CurrentLevel { get; }
        public int CurrentExperience { get; }
        public int ExperienceToNextLevel { get; }
    }

    public static class SharedExperienceSnapshotCodec
    {
        public const int PayloadSize = 16;

        public static byte[] Encode(
            SharedExperienceSnapshotPayload snapshot)
        {
            if (snapshot == null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }

            var payload = new byte[PayloadSize];
            PacketCodec.WriteNetworkUInt32(payload, 0, snapshot.Sequence);
            PacketCodec.WriteNetworkUInt32(
                payload, 4, checked((uint)snapshot.CurrentLevel));
            PacketCodec.WriteNetworkUInt32(
                payload, 8, checked((uint)snapshot.CurrentExperience));
            PacketCodec.WriteNetworkUInt32(
                payload, 12, checked((uint)snapshot.ExperienceToNextLevel));
            return payload;
        }

        public static SharedExperienceSnapshotPayload Decode(byte[] payload)
        {
            if (payload == null || payload.Length != PayloadSize)
            {
                throw new ArgumentException(
                    "Shared experience snapshot payload must be 16 bytes.",
                    nameof(payload));
            }

            return new SharedExperienceSnapshotPayload(
                PacketCodec.ReadNetworkUInt32(payload, 0),
                checked((int)PacketCodec.ReadNetworkUInt32(payload, 4)),
                checked((int)PacketCodec.ReadNetworkUInt32(payload, 8)),
                checked((int)PacketCodec.ReadNetworkUInt32(payload, 12)));
        }
    }
}
