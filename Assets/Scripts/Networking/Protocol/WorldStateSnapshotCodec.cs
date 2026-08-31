using System;
using System.Collections.Generic;

namespace TopDownRoguelike.Networking.Protocol
{
    public enum WorldEntityLifecycle : byte
    {
        Snapshot = 0,
        Spawn = 1,
        Update = 2,
        Dead = 3,
        Removed = 4
    }

    [Flags]
    public enum WorldEntityFlags : ushort
    {
        None = 0,
        Active = 1 << 0,
        Dead = 1 << 1
    }

    public sealed class WorldEntityRecord
    {
        public WorldEntityRecord(
            uint entityId,
            NetworkEntityType entityType,
            WorldEntityLifecycle lifecycle,
            WorldEntityFlags flags,
            float positionX,
            float positionY,
            float rotationDegrees,
            ushort currentHealth,
            ushort maxHealth,
            byte bossPhase = 0,
            NetworkEnemyArchetype enemyArchetype =
                NetworkEnemyArchetype.Invalid)
        {
            EntityId = entityId;
            EntityType = entityType;
            Lifecycle = lifecycle;
            Flags = flags;
            PositionX = positionX;
            PositionY = positionY;
            RotationDegrees = rotationDegrees;
            CurrentHealth = currentHealth;
            MaxHealth = maxHealth;
            BossPhase = bossPhase;
            EnemyArchetype = enemyArchetype;
        }

        public uint EntityId { get; }
        public NetworkEntityType EntityType { get; }
        public WorldEntityLifecycle Lifecycle { get; }
        public WorldEntityFlags Flags { get; }
        public bool IsDead =>
            (Flags & WorldEntityFlags.Dead) != 0;
        public float PositionX { get; }
        public float PositionY { get; }
        public float RotationDegrees { get; }
        public ushort CurrentHealth { get; }
        public ushort MaxHealth { get; }
        public byte BossPhase { get; }
        public NetworkEnemyArchetype EnemyArchetype { get; }
    }

    public sealed class WorldStateSnapshotPayload
    {
        private readonly List<WorldEntityRecord> entities;

        public WorldStateSnapshotPayload(
            IReadOnlyList<WorldEntityRecord> entities)
        {
            if (entities == null)
            {
                throw new ArgumentNullException(
                    nameof(entities));
            }

            this.entities =
                new List<WorldEntityRecord>(
                    entities.Count);

            for (int index = 0;
                index < entities.Count;
                index++)
            {
                this.entities.Add(
                    entities[index]);
            }
        }

        public IReadOnlyList<WorldEntityRecord> Entities => entities;
    }

    public static class WorldStateSnapshotCodec
    {
        public const int PrefixSize = 4;
        public const int RecordSize = 32;
        public const int MaxEntityCount = 64;

        private const ushort ActiveFlag = 1 << 0;
        private const ushort DeadFlag = 1 << 1;
        private const ushort KnownFlags = ActiveFlag | DeadFlag;

        public static byte[] Encode(
            WorldStateSnapshotPayload snapshot)
        {
            if (snapshot == null)
            {
                throw new ArgumentNullException(
                    nameof(snapshot));
            }

            ValidateEntityCount(snapshot.Entities.Count);

            var orderedEntities =
                new List<WorldEntityRecord>(
                    snapshot.Entities);

            ValidateEntities(
                orderedEntities,
                false);

            orderedEntities.Sort(
                (left, right) => left.EntityId.CompareTo(
                    right.EntityId));

            ValidateEntities(
                orderedEntities,
                true);

            var payload = new byte[
                PrefixSize +
                orderedEntities.Count * RecordSize];

            PacketCodec.WriteNetworkUInt32(
                payload,
                0,
                (uint)orderedEntities.Count);

            int offset = PrefixSize;

            foreach (WorldEntityRecord entity
                in orderedEntities)
            {
                PacketCodec.WriteNetworkUInt32(
                    payload,
                    offset,
                    entity.EntityId);

                payload[offset + 4] =
                    (byte)entity.EntityType;

                payload[offset + 5] =
                    (byte)entity.Lifecycle;

                PacketCodec.WriteNetworkUInt16(
                    payload,
                    offset + 6,
                    (ushort)entity.Flags);

                WriteNetworkFloat(
                    payload,
                    offset + 8,
                    entity.PositionX);

                WriteNetworkFloat(
                    payload,
                    offset + 12,
                    entity.PositionY);

                WriteNetworkFloat(
                    payload,
                    offset + 16,
                    entity.RotationDegrees);

                PacketCodec.WriteNetworkUInt16(
                    payload,
                    offset + 20,
                    entity.CurrentHealth);

                PacketCodec.WriteNetworkUInt16(
                    payload,
                    offset + 22,
                    entity.MaxHealth);

                payload[offset + 24] =
                    entity.BossPhase;

                payload[offset + 25] =
                    (byte)entity.EnemyArchetype;

                offset += RecordSize;
            }

            return payload;
        }

        public static WorldStateSnapshotPayload Decode(
            byte[] payload)
        {
            if (payload == null)
            {
                throw new ArgumentNullException(
                    nameof(payload));
            }

            if (payload.Length < PrefixSize)
            {
                throw new ArgumentException(
                    "World snapshot is truncated.",
                    nameof(payload));
            }

            uint rawEntityCount =
                PacketCodec.ReadNetworkUInt32(
                    payload,
                    0);

            if (rawEntityCount == 0u ||
                rawEntityCount > MaxEntityCount)
            {
                throw new ArgumentException(
                    "World snapshot has an invalid entity count.",
                    nameof(payload));
            }

            int entityCount =
                checked((int)rawEntityCount);

            int expectedSize =
                PrefixSize +
                entityCount * RecordSize;

            if (payload.Length != expectedSize)
            {
                throw new ArgumentException(
                    "World snapshot has an invalid size.",
                    nameof(payload));
            }

            var entities =
                new List<WorldEntityRecord>(
                    entityCount);

            int offset = PrefixSize;

            for (int index = 0;
                index < entityCount;
                index++)
            {
                uint entityId =
                    PacketCodec.ReadNetworkUInt32(
                        payload,
                        offset);

                NetworkEntityType entityType =
                    (NetworkEntityType)payload[offset + 4];

                WorldEntityLifecycle lifecycle =
                    (WorldEntityLifecycle)payload[offset + 5];

                WorldEntityFlags flags =
                    (WorldEntityFlags)
                    PacketCodec.ReadNetworkUInt16(
                        payload,
                        offset + 6);

                float positionX =
                    ReadNetworkFloat(payload, offset + 8);

                float positionY =
                    ReadNetworkFloat(payload, offset + 12);

                float rotationDegrees =
                    ReadNetworkFloat(payload, offset + 16);

                ushort currentHealth =
                    PacketCodec.ReadNetworkUInt16(
                        payload,
                        offset + 20);

                ushort maxHealth =
                    PacketCodec.ReadNetworkUInt16(
                        payload,
                        offset + 22);

                byte bossPhase =
                    payload[offset + 24];

                NetworkEnemyArchetype enemyArchetype =
                    (NetworkEnemyArchetype)
                    payload[offset + 25];

                for (int reservedOffset = 26;
                    reservedOffset < RecordSize;
                    reservedOffset++)
                {
                    if (payload[offset + reservedOffset] != 0)
                    {
                        throw new ArgumentException(
                            "World entity reserved byte must be zero.",
                            nameof(payload));
                    }
                }

                entities.Add(
                    new WorldEntityRecord(
                        entityId,
                        entityType,
                        lifecycle,
                        flags,
                        positionX,
                        positionY,
                        rotationDegrees,
                        currentHealth,
                        maxHealth,
                        bossPhase,
                        enemyArchetype));

                offset += RecordSize;
            }

            ValidateEntities(entities, true);

            return new WorldStateSnapshotPayload(entities);
        }

        private static void ValidateEntities(
            IReadOnlyList<WorldEntityRecord> entities,
            bool requireAscendingOrder)
        {
            for (int index = 0;
                index < entities.Count;
                index++)
            {
                WorldEntityRecord entity =
                    entities[index];

                if (entity == null ||
                    entity.EntityId == 0u)
                {
                    throw new ArgumentException(
                        "World entity ID must be non-zero.");
                }

                byte entityTypeValue =
                    (byte)entity.EntityType;

                if (entityTypeValue <
                        (byte)NetworkEntityType.Player ||
                    entityTypeValue >
                        (byte)NetworkEntityType.ExperienceOrb)
                {
                    throw new ArgumentException(
                        "World entity type is invalid.");
                }

                if ((byte)entity.Lifecycle > 4)
                {
                    throw new ArgumentException(
                        "World entity lifecycle is invalid.");
                }

                if (entity.EntityType ==
                    NetworkEntityType.Enemy)
                {
                    if (entity.EnemyArchetype !=
                            NetworkEnemyArchetype.Basic &&
                        entity.EnemyArchetype !=
                            NetworkEnemyArchetype.Fast)
                    {
                        throw new ArgumentException(
                            "Enemy archetype is invalid.");
                    }
                }
                else if (entity.EnemyArchetype !=
                    NetworkEnemyArchetype.Invalid)
                {
                    throw new ArgumentException(
                        "Non-enemy entity contains an enemy archetype.");
                }

                ushort rawFlags =
                    (ushort)entity.Flags;

                if ((rawFlags & ~KnownFlags) != 0)
                {
                    throw new ArgumentException(
                        "World entity contains unknown flags.");
                }

                if (!IsFinite(entity.PositionX) ||
                    !IsFinite(entity.PositionY) ||
                    !IsFinite(entity.RotationDegrees))
                {
                    throw new ArgumentException(
                        "World entity contains a non-finite value.");
                }

                if (entity.EntityType ==
                    NetworkEntityType.ExperienceOrb)
                {
                    if (entity.CurrentHealth != 0 ||
                        entity.MaxHealth != 0 ||
                        entity.BossPhase != 0 ||
                        (rawFlags & DeadFlag) != 0)
                    {
                        throw new ArgumentException(
                            "Experience orb contains combat state.");
                    }
                }
                else
                {
                    if (entity.MaxHealth == 0 ||
                        entity.CurrentHealth > entity.MaxHealth)
                    {
                        throw new ArgumentException(
                            "World entity contains an invalid health range.");
                    }

                    bool isDead =
                        (rawFlags & DeadFlag) != 0;

                    if (isDead !=
                        (entity.CurrentHealth == 0))
                    {
                        throw new ArgumentException(
                            "World entity death flag does not match health.");
                    }

                    if (entity.EntityType ==
                        NetworkEntityType.Boss)
                    {
                        if (entity.BossPhase < 1 ||
                            entity.BossPhase > 2)
                        {
                            throw new ArgumentException(
                                "Boss phase is outside the supported range.");
                        }
                    }
                    else if (entity.BossPhase != 0)
                    {
                        throw new ArgumentException(
                            "Non-Boss entity contains a Boss phase.");
                    }
                }

                if (requireAscendingOrder &&
                    index > 0)
                {
                    WorldEntityRecord previous =
                        entities[index - 1];

                    if (previous.EntityId >=
                        entity.EntityId)
                    {
                        throw new ArgumentException(
                            "World entities are not ordered by ID.");
                    }
                }
            }
        }

        private static void ValidateEntityCount(
            int entityCount)
        {
            if (entityCount < 1 ||
                entityCount > MaxEntityCount)
            {
                throw new ArgumentException(
                    "World snapshot has an invalid entity count.");
            }
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) &&
                !float.IsInfinity(value);
        }

        private static void WriteNetworkFloat(
            byte[] destination,
            int offset,
            float value)
        {
            byte[] localBytes =
                BitConverter.GetBytes(value);

            uint bits =
                BitConverter.ToUInt32(
                    localBytes,
                    0);

            PacketCodec.WriteNetworkUInt32(
                destination,
                offset,
                bits);
        }

        private static float ReadNetworkFloat(
            byte[] source,
            int offset)
        {
            uint bits =
                PacketCodec.ReadNetworkUInt32(
                    source,
                    offset);

            byte[] localBytes =
                BitConverter.GetBytes(bits);

            return BitConverter.ToSingle(
                localBytes,
                0);
        }
    }
}
