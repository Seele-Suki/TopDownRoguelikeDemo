using System.Collections.Generic;
using TopDownRoguelike.Infrastructure;

namespace TopDownRoguelike.Networking.Room
{
    public sealed class RoomState
    {
        public const int MaxPlayerCount = 4;
        public const int RequiredPlayerCount = 2;

        private readonly RoomPlayerState[] players;

        public IReadOnlyList<RoomPlayerState> Players => players;

        public DifficultyId SelectedDifficulty { get; private set; } =
            DifficultyId.None;

        public int PlayerCount
        {
            get
            {
                int count = 0;

                foreach (RoomPlayerState player in players)
                {
                    if (player.IsOccupied)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        public bool CanStartGame
        {
            get
            {
                if (SelectedDifficulty == DifficultyId.None)
                {
                    return false;
                }

                if (PlayerCount < RequiredPlayerCount)
                {
                    return false;
                }

                bool hasHost = false;

                foreach (RoomPlayerState player in players)
                {
                    if (!player.IsOccupied)
                    {
                        continue;
                    }

                    if (player.Role == RoomRole.Host)
                    {
                        hasHost = true;
                    }

                    if (player.SelectedCharacter == CharacterId.None)
                    {
                        return false;
                    }

                    if (!player.IsReady)
                    {
                        return false;
                    }
                }

                return hasHost;
            }
        }

        public RoomState()
        {
            players = new RoomPlayerState[MaxPlayerCount];

            for (int i = 0; i < players.Length; i++)
            {
                players[i] = new RoomPlayerState();
            }
        }

        public bool TryAddPlayer(
            int playerId,
            string nickname,
            RoomRole role)
        {
            if (playerId <= 0 ||
                string.IsNullOrWhiteSpace(nickname) ||
                role == RoomRole.None)
            {
                return false;
            }

            if (GetPlayer(playerId) != null)
            {
                return false;
            }

            if (role == RoomRole.Host && HasHost())
            {
                return false;
            }

            RoomPlayerState emptySlot = FindEmptySlot();

            if (emptySlot == null)
            {
                return false;
            }

            emptySlot.Assign(playerId, nickname, role);
            return true;
        }

        public bool TrySelectCharacter(
            int playerId,
            CharacterId character)
        {
            if (character == CharacterId.None)
            {
                return false;
            }

            RoomPlayerState player = GetPlayer(playerId);

            if (player == null)
            {
                return false;
            }

            player.SelectCharacter(character);
            return true;
        }

        public bool TrySetReady(int playerId, bool ready)
        {
            RoomPlayerState player = GetPlayer(playerId);

            if (player == null)
            {
                return false;
            }

            if (ready &&
                player.SelectedCharacter == CharacterId.None)
            {
                return false;
            }

            player.SetReady(ready);
            return true;
        }

        public bool TrySelectDifficulty(
            int requesterPlayerId,
            DifficultyId difficulty)
        {
            if (difficulty == DifficultyId.None)
            {
                return false;
            }

            RoomPlayerState requester =
                GetPlayer(requesterPlayerId);

            if (requester == null ||
                requester.Role != RoomRole.Host)
            {
                return false;
            }

            SelectedDifficulty = difficulty;
            return true;
        }

        public bool RemovePlayer(int playerId)
        {
            RoomPlayerState player = GetPlayer(playerId);

            if (player == null)
            {
                return false;
            }

            if (player.Role == RoomRole.Host)
            {
                Reset();
                return true;
            }

            player.Clear();
            return true;
        }

        public RoomPlayerState GetPlayer(int playerId)
        {
            foreach (RoomPlayerState player in players)
            {
                if (player.PlayerId == playerId)
                {
                    return player;
                }
            }

            return null;
        }

        public void Reset()
        {
            foreach (RoomPlayerState player in players)
            {
                player.Clear();
            }

            SelectedDifficulty = DifficultyId.None;
        }

        private bool HasHost()
        {
            foreach (RoomPlayerState player in players)
            {
                if (player.IsOccupied &&
                    player.Role == RoomRole.Host)
                {
                    return true;
                }
            }

            return false;
        }

        private RoomPlayerState FindEmptySlot()
        {
            foreach (RoomPlayerState player in players)
            {
                if (!player.IsOccupied)
                {
                    return player;
                }
            }

            return null;
        }
    }
}