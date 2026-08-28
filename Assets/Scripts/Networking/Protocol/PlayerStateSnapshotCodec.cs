using System;
using System.Collections.Generic;

namespace TopDownRoguelike.Networking.Protocol
{
    public sealed class PlayerStateRecord
    {
        public PlayerStateRecord(
            uint playerId,
            float positionX,
            float positionY,
            float aimX,
            float aimY,
            bool fireHeld = false)
        {
            PlayerId = playerId;
            PositionX = positionX;
            PositionY = positionY;
            AimX = aimX;
            AimY = aimY;
            FireHeld = fireHeld;
        }

        public uint PlayerId { get; }
        public float PositionX { get; }
        public float PositionY { get; }
        public float AimX { get; }
        public float AimY { get; }
        public bool FireHeld { get; }
    }

    public sealed class PlayerStateSnapshotPayload
    {
        private readonly List<PlayerStateRecord>
            players;

        public PlayerStateSnapshotPayload(
            IReadOnlyList<PlayerStateRecord> players)
        {
            if (players == null)
            {
                throw new ArgumentNullException(
                    nameof(players));
            }

            this.players =
                new List<PlayerStateRecord>(
                    players.Count);

            for (int index = 0;
                index < players.Count;
                index++)
            {
                this.players.Add(players[index]);
            }
        }

        public IReadOnlyList<PlayerStateRecord>
            Players => players;
    }

    public static class PlayerStateSnapshotCodec
    {
        public const int PrefixSize = 4;
        public const int RecordSize = 24;
        public const int MaxPlayerCount = 4;

        private const int FlagsOffset = 20;
        private const uint FireHeldFlag = 1u;
        private const uint KnownFlags = FireHeldFlag;

        public static byte[] Encode(
            PlayerStateSnapshotPayload snapshot)
        {
            if (snapshot == null)
            {
                throw new ArgumentNullException(
                    nameof(snapshot));
            }

            ValidatePlayerCount(
                snapshot.Players.Count);

            ValidatePlayers(
                snapshot.Players,
                false);

            var orderedPlayers =
                new List<PlayerStateRecord>(
                    snapshot.Players.Count);

            for (int index = 0;
                index < snapshot.Players.Count;
                index++)
            {
                orderedPlayers.Add(
                    snapshot.Players[index]);
            }

            orderedPlayers.Sort(
                (left, right) =>
                    left.PlayerId.CompareTo(
                        right.PlayerId));

            var payload = new byte[
                PrefixSize +
                orderedPlayers.Count * RecordSize];

            PacketCodec.WriteNetworkUInt32(
                payload,
                0,
                (uint)orderedPlayers.Count);

            int offset = PrefixSize;

            foreach (PlayerStateRecord player
                in orderedPlayers)
            {
                PacketCodec.WriteNetworkUInt32(
                    payload,
                    offset,
                    player.PlayerId);

                WriteNetworkFloat(
                    payload,
                    offset + 4,
                    player.PositionX);

                WriteNetworkFloat(
                    payload,
                    offset + 8,
                    player.PositionY);

                WriteNetworkFloat(
                    payload,
                    offset + 12,
                    player.AimX);

                WriteNetworkFloat(
                    payload,
                    offset + 16,
                    player.AimY);

                uint flags =
                    player.FireHeld
                        ? FireHeldFlag
                        : 0u;

                PacketCodec.WriteNetworkUInt32(
                    payload,
                    offset + FlagsOffset,
                    flags);

                offset += RecordSize;
            }

            return payload;
        }

        public static PlayerStateSnapshotPayload Decode(
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
                    "Player state snapshot is truncated.",
                    nameof(payload));
            }

            uint rawPlayerCount =
                PacketCodec.ReadNetworkUInt32(
                    payload,
                    0);

            if (rawPlayerCount == 0u ||
                rawPlayerCount > MaxPlayerCount)
            {
                throw new ArgumentException(
                    "Player state snapshot has an invalid player count.",
                    nameof(payload));
            }

            int playerCount =
                checked((int)rawPlayerCount);

            int expectedSize =
                PrefixSize +
                playerCount * RecordSize;

            if (payload.Length != expectedSize)
            {
                throw new ArgumentException(
                    "Player state snapshot has an invalid size.",
                    nameof(payload));
            }

            var players =
                new List<PlayerStateRecord>(
                    playerCount);

            int offset = PrefixSize;

            for (int index = 0;
                index < playerCount;
                index++)
            {
                uint playerId =
                    PacketCodec.ReadNetworkUInt32(
                        payload,
                        offset);

                float positionX =
                    ReadNetworkFloat(
                        payload,
                        offset + 4);

                float positionY =
                    ReadNetworkFloat(
                        payload,
                        offset + 8);

                float aimX =
                    ReadNetworkFloat(
                        payload,
                        offset + 12);

                float aimY =
                    ReadNetworkFloat(
                        payload,
                        offset + 16);

                uint flags =
                    PacketCodec.ReadNetworkUInt32(
                        payload,
                        offset + FlagsOffset);

                if ((flags & ~KnownFlags) != 0u)
                {
                    throw new ArgumentException(
                        "Player state contains unknown flags.",
                        nameof(payload));
                }

                bool fireHeld =
                    (flags & FireHeldFlag) != 0u;

                players.Add(
                    new PlayerStateRecord(
                        playerId,
                        positionX,
                        positionY,
                        aimX,
                        aimY,
                        fireHeld)
                );

                offset += RecordSize;
            }

            ValidatePlayers(
                players,
                true);

            return new PlayerStateSnapshotPayload(
                players);
        }

        private static void ValidatePlayers(
            IReadOnlyList<PlayerStateRecord> players,
            bool requireAscendingOrder)
        {
            for (int index = 0;
                index < players.Count;
                index++)
            {
                PlayerStateRecord player =
                    players[index];

                if (player == null)
                {
                    throw new ArgumentException(
                        "Player state snapshot contains a null record.");
                }

                if (player.PlayerId == 0u)
                {
                    throw new ArgumentException(
                        "Player state ID must be non-zero.");
                }

                if (!IsFinite(player.PositionX) ||
                    !IsFinite(player.PositionY) ||
                    !IsFinite(player.AimX) ||
                    !IsFinite(player.AimY))
                {
                    throw new ArgumentException(
                        "Player state contains a non-finite value.");
                }

                for (int otherIndex = index + 1;
                    otherIndex < players.Count;
                    otherIndex++)
                {
                    PlayerStateRecord other =
                        players[otherIndex];

                    if (other != null &&
                        player.PlayerId ==
                            other.PlayerId)
                    {
                        throw new ArgumentException(
                            "Player state contains duplicate IDs.");
                    }
                }

                if (requireAscendingOrder &&
                    index > 0 &&
                    players[index - 1].PlayerId >
                        player.PlayerId)
                {
                    throw new ArgumentException(
                        "Player states are not ordered by ID.");
                }
            }
        }

        private static bool IsFinite(
            float value)
        {
            return !float.IsNaN(value) &&
                !float.IsInfinity(value);
        }

        private static void ValidatePlayerCount(
            int playerCount)
        {
            if (playerCount < 1 ||
                playerCount > MaxPlayerCount)
            {
                throw new ArgumentException(
                    "Player state snapshot has an invalid player count.");
            }
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