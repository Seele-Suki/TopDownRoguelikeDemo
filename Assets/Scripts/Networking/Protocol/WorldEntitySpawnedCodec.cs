using System;

namespace TopDownRoguelike.Networking.Protocol
{
    public static class WorldEntitySpawnedCodec
    {
        public const int PayloadSize =
            WorldStateSnapshotCodec.RecordSize;

        public static byte[] Encode(
            WorldEntityRecord record)
        {
            ValidateSpawnRecord(record);

            byte[] snapshotPayload =
                WorldStateSnapshotCodec.Encode(
                    new WorldStateSnapshotPayload(
                        new[] { record }));

            var payload =
                new byte[PayloadSize];

            Buffer.BlockCopy(
                snapshotPayload,
                WorldStateSnapshotCodec.PrefixSize,
                payload,
                0,
                PayloadSize);

            return payload;
        }

        public static WorldEntityRecord Decode(
            byte[] payload)
        {
            if (payload == null)
            {
                throw new ArgumentNullException(
                    nameof(payload));
            }

            if (payload.Length != PayloadSize)
            {
                throw new ArgumentException(
                    "World entity spawn payload must contain " +
                    "exactly one entity record.",
                    nameof(payload));
            }

            var snapshotPayload =
                new byte[
                    WorldStateSnapshotCodec.PrefixSize +
                    PayloadSize];

            PacketCodec.WriteNetworkUInt32(
                snapshotPayload,
                0,
                1u);

            Buffer.BlockCopy(
                payload,
                0,
                snapshotPayload,
                WorldStateSnapshotCodec.PrefixSize,
                PayloadSize);

            WorldEntityRecord record =
                WorldStateSnapshotCodec.Decode(
                    snapshotPayload)
                .Entities[0];

            ValidateSpawnRecord(record);
            return record;
        }

        private static void ValidateSpawnRecord(
            WorldEntityRecord record)
        {
            if (record == null)
            {
                throw new ArgumentNullException(
                    nameof(record));
            }

            if (record.Lifecycle !=
                WorldEntityLifecycle.Spawn)
            {
                throw new ArgumentException(
                    "World entity spawn record must use " +
                    "the Spawn lifecycle.",
                    nameof(record));
            }

            if ((record.Flags & WorldEntityFlags.Dead) != 0)
            {
                throw new ArgumentException(
                    "World entity spawn record cannot be dead.",
                    nameof(record));
            }
        }
    }
}
