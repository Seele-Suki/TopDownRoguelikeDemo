using System;
using System.Collections.Generic;
using System.Text;
using TopDownRoguelike.Infrastructure;

namespace TopDownRoguelike.Networking.Protocol
{
    public enum RoomStateStatus : byte
    {
        Waiting = 0,
        Started = 1
    }

    public sealed class RoomPlayerSnapshot
    {
        public RoomPlayerSnapshot(
            uint playerId,
            bool isHost,
            bool isReady,
            CharacterId character,
            string nickname)
        {
            PlayerId = playerId;
            IsHost = isHost;
            IsReady = isReady;
            Character = character;
            Nickname = nickname
                ?? throw new ArgumentNullException(
                    nameof(nickname));
        }

        public uint PlayerId { get; }
        public bool IsHost { get; }
        public bool IsReady { get; }
        public CharacterId Character { get; }
        public string Nickname { get; }
    }

    public sealed class RoomStateSnapshot
    {
        private readonly
            List<RoomPlayerSnapshot> players;

        public RoomStateSnapshot(
            string roomId,
            RoomStateStatus status,
            DifficultyId selectedDifficulty,
            IReadOnlyList<RoomPlayerSnapshot> players)
        {
            RoomId = roomId
                ?? throw new ArgumentNullException(
                    nameof(roomId));

            Status = status;
            SelectedDifficulty = selectedDifficulty;

            if (players == null)
            {
                throw new ArgumentNullException(
                    nameof(players));
            }

            this.players =
                new List<RoomPlayerSnapshot>(players);
        }

        public string RoomId { get; }
        public RoomStateStatus Status { get; }
        public DifficultyId SelectedDifficulty { get; }

        public IReadOnlyList<RoomPlayerSnapshot>
            Players => players;
    }

    public static class RoomStateSnapshotCodec
    {
        public const int MaxPlayerCount = 4;
        private const byte HostFlag = 0x01;
        private const byte ReadyFlag = 0x02;
        private const byte KnownFlags =
            HostFlag | ReadyFlag;

        private static readonly Encoding StrictUtf8 =
            new UTF8Encoding(
                false,
                true);

        public static byte[] Encode(
            RoomStateSnapshot snapshot)
        {
            if (snapshot == null)
            {
                throw new ArgumentNullException(
                    nameof(snapshot));
            }

            ValidateSnapshot(snapshot);

            var output = new List<byte>();

            AppendString(output, snapshot.RoomId);
            output.Add((byte)snapshot.Status);
            output.Add(
                (byte)snapshot.SelectedDifficulty);
            output.Add(
                checked((byte)snapshot.Players.Count));

            foreach (RoomPlayerSnapshot player
                in snapshot.Players)
            {
                AppendUInt32(output, player.PlayerId);

                byte flags = 0;

                if (player.IsHost)
                {
                    flags |= HostFlag;
                }

                if (player.IsReady)
                {
                    flags |= ReadyFlag;
                }

                output.Add(flags);
                output.Add((byte)player.Character);
                AppendString(output, player.Nickname);
            }

            return output.ToArray();
        }

        public static RoomStateSnapshot Decode(
            byte[] payload)
        {
            if (payload == null)
            {
                throw new ArgumentNullException(
                    nameof(payload));
            }

            var reader = new Reader(payload);

            string roomId = reader.ReadString();

            var status =
                (RoomStateStatus)reader.ReadByte();

            if (status != RoomStateStatus.Waiting &&
                status != RoomStateStatus.Started)
            {
                throw new ArgumentException(
                    "Snapshot contains an invalid room status.",
                    nameof(payload));
            }

            var difficulty =
                (DifficultyId)reader.ReadByte();

            if (difficulty != DifficultyId.None &&
                difficulty != DifficultyId.Normal &&
                difficulty != DifficultyId.Hard &&
                difficulty != DifficultyId.Hell)
            {
                throw new ArgumentException(
                    "Snapshot contains an invalid difficulty.",
                    nameof(payload));
            }

            int playerCount = reader.ReadByte();

            if (playerCount < 1 ||
                playerCount > MaxPlayerCount)
            {
                throw new ArgumentException(
                    "Snapshot contains an invalid player count.",
                    nameof(payload));
            }

            var players =
                new List<RoomPlayerSnapshot>(
                    playerCount);

            for (int index = 0;
                index < playerCount;
                index++)
            {
                uint playerId =
                    reader.ReadUInt32();

                byte flags =
                    reader.ReadByte();

                if ((flags & ~KnownFlags) != 0)
                {
                    throw new ArgumentException(
                        "Snapshot contains unknown player flags.",
                        nameof(payload));
                }

                var character =
                    (CharacterId)reader.ReadByte();

                string nickname =
                    reader.ReadString();

                players.Add(
                    new RoomPlayerSnapshot(
                        playerId,
                        (flags & HostFlag) != 0,
                        (flags & ReadyFlag) != 0,
                        character,
                        nickname));
            }

            reader.RequireEnd();

            var snapshot =
                new RoomStateSnapshot(
                    roomId,
                    status,
                    difficulty,
                    players);

            ValidateSnapshot(snapshot);

            return snapshot;
        }

        private static void ValidateSnapshot(
            RoomStateSnapshot snapshot)
        {
            if (string.IsNullOrEmpty(
                snapshot.RoomId))
            {
                throw new ArgumentException(
                    "Snapshot room ID cannot be empty.");
            }

            if (snapshot.Status !=
                    RoomStateStatus.Waiting &&
                snapshot.Status !=
                    RoomStateStatus.Started)
            {
                throw new ArgumentException(
                    "Snapshot contains an invalid room status.");
            }

            if (snapshot.SelectedDifficulty !=
                    DifficultyId.None &&
                snapshot.SelectedDifficulty !=
                    DifficultyId.Normal &&
                snapshot.SelectedDifficulty !=
                    DifficultyId.Hard &&
                snapshot.SelectedDifficulty !=
                    DifficultyId.Hell)
            {
                throw new ArgumentException(
                    "Snapshot contains an invalid difficulty.");
            }

            if (snapshot.Players.Count < 1 ||
                snapshot.Players.Count >
                    MaxPlayerCount)
            {
                throw new ArgumentException(
                    "Snapshot contains an invalid player count.");
            }

            int hostCount = 0;

            for (int index = 0;
                index < snapshot.Players.Count;
                index++)
            {
                RoomPlayerSnapshot player =
                    snapshot.Players[index];

                if (player == null)
                {
                    throw new ArgumentException(
                        "Snapshot contains a null player.");
                }

                if (player.PlayerId == 0u)
                {
                    throw new ArgumentException(
                        "Snapshot player ID must be non-zero.");
                }

                if (string.IsNullOrEmpty(
                    player.Nickname))
                {
                    throw new ArgumentException(
                        "Snapshot nickname cannot be empty.");
                }

                if (player.Character !=
                        CharacterId.None &&
                    player.Character !=
                        CharacterId.Ranged &&
                    player.Character !=
                        CharacterId.Melee)
                {
                    throw new ArgumentException(
                        "Snapshot contains an invalid character.");
                }

                if (player.IsHost)
                {
                    hostCount++;
                }

                for (int otherIndex = index + 1;
                    otherIndex <
                        snapshot.Players.Count;
                    otherIndex++)
                {
                    RoomPlayerSnapshot otherPlayer =
                        snapshot.Players[otherIndex];

                    if (otherPlayer != null &&
                        player.PlayerId ==
                            otherPlayer.PlayerId)
                    {
                        throw new ArgumentException(
                            "Snapshot contains duplicate player IDs.");
                    }
                }
            }

            if (hostCount != 1)
            {
                throw new ArgumentException(
                    "Snapshot must contain exactly one host.");
            }
        }

        private static void AppendUInt16(
            List<byte> output,
            ushort value)
        {
            var bytes = new byte[sizeof(ushort)];

            PacketCodec.WriteNetworkUInt16(
                bytes,
                0,
                value);

            output.AddRange(bytes);
        }

        private static void AppendUInt32(
            List<byte> output,
            uint value)
        {
            var bytes = new byte[sizeof(uint)];

            PacketCodec.WriteNetworkUInt32(
                bytes,
                0,
                value);

            output.AddRange(bytes);
        }

        private static void AppendString(
            List<byte> output,
            string value)
        {
            byte[] bytes =
                StrictUtf8.GetBytes(value);

            if (bytes.Length > ushort.MaxValue)
            {
                throw new ArgumentException(
                    "Snapshot string is too long.");
            }

            AppendUInt16(
                output,
                (ushort)bytes.Length);

            output.AddRange(bytes);
        }

        private sealed class Reader
        {
            private readonly byte[] data;
            private int offset;

            public Reader(byte[] data)
            {
                this.data = data;
            }

            public byte ReadByte()
            {
                Require(sizeof(byte));
                return data[offset++];
            }

            public uint ReadUInt32()
            {
                Require(sizeof(uint));

                uint value =
                    PacketCodec.ReadNetworkUInt32(
                        data,
                        offset);

                offset += sizeof(uint);
                return value;
            }

            public string ReadString()
            {
                ushort length = ReadUInt16();
                Require(length);

                string value;

                try
                {
                    value =
                        StrictUtf8.GetString(
                            data,
                            offset,
                            length);
                }
                catch (DecoderFallbackException exception)
                {
                    throw new ArgumentException(
                        "Snapshot contains invalid UTF-8.",
                        exception);
                }

                offset += length;
                return value;
            }

            public void RequireEnd()
            {
                if (offset != data.Length)
                {
                    throw new ArgumentException(
                        "Snapshot contains trailing bytes.");
                }
            }

            private ushort ReadUInt16()
            {
                Require(sizeof(ushort));

                ushort value =
                    PacketCodec.ReadNetworkUInt16(
                        data,
                        offset);

                offset += sizeof(ushort);
                return value;
            }

            private void Require(int count)
            {
                if (count < 0 ||
                    offset > data.Length - count)
                {
                    throw new ArgumentException(
                        "Snapshot payload is truncated.");
                }
            }
        }
    }
}