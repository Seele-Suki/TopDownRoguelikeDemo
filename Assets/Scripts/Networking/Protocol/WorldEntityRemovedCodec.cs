using System;

namespace TopDownRoguelike.Networking.Protocol
{
    public enum WorldEntityRemovalReason : byte
    {
        Invalid = 0,
        Died = 1,
        Cleared = 2,
        Despawned = 3
    }

    public sealed class WorldEntityRemovedPayload
    {
        public WorldEntityRemovedPayload(
            uint entityId,
            NetworkEntityType entityType,
            WorldEntityRemovalReason reason)
        {
            EntityId = entityId;
            EntityType = entityType;
            Reason = reason;
        }

        public uint EntityId { get; }
        public NetworkEntityType EntityType { get; }
        public WorldEntityRemovalReason Reason { get; }
    }

    public static class WorldEntityRemovedCodec
    {
        public const int PayloadSize = 8;

        public static byte[] Encode(
            WorldEntityRemovedPayload removed)
        {
            Validate(removed);

            var payload = new byte[PayloadSize];

            PacketCodec.WriteNetworkUInt32(
                payload,
                0,
                removed.EntityId);

            payload[4] = (byte)removed.EntityType;
            payload[5] = (byte)removed.Reason;

            return payload;
        }

        public static WorldEntityRemovedPayload Decode(
            byte[] payload)
        {
            if (payload == null)
            {
                throw new ArgumentNullException(nameof(payload));
            }

            if (payload.Length != PayloadSize)
            {
                throw new ArgumentException(
                    "World entity removal payload must be 8 bytes.",
                    nameof(payload));
            }

            if (payload[6] != 0 || payload[7] != 0)
            {
                throw new ArgumentException(
                    "World entity removal reserved bytes must be zero.",
                    nameof(payload));
            }

            var removed =
                new WorldEntityRemovedPayload(
                    PacketCodec.ReadNetworkUInt32(payload, 0),
                    (NetworkEntityType)payload[4],
                    (WorldEntityRemovalReason)payload[5]);

            Validate(removed);
            return removed;
        }

        private static void Validate(
            WorldEntityRemovedPayload removed)
        {
            if (removed == null)
            {
                throw new ArgumentNullException(nameof(removed));
            }

            if (removed.EntityId == 0u)
            {
                throw new ArgumentException(
                    "Removed entity ID must be non-zero.",
                    nameof(removed));
            }

            int entityType = (int)removed.EntityType;

            if (entityType < (int)NetworkEntityType.Player ||
                entityType > (int)NetworkEntityType.ExperienceOrb)
            {
                throw new ArgumentException(
                    "Removed entity type is invalid.",
                    nameof(removed));
            }

            int reason = (int)removed.Reason;

            if (reason < (int)WorldEntityRemovalReason.Died ||
                reason > (int)WorldEntityRemovalReason.Despawned)
            {
                throw new ArgumentException(
                    "Entity removal reason is invalid.",
                    nameof(removed));
            }
        }
    }
}
