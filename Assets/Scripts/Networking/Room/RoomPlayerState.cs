using System;
using TopDownRoguelike.Infrastructure;

namespace TopDownRoguelike.Networking.Room
{
    public sealed class RoomPlayerState
    {
        public int PlayerId { get; private set; }
        public string Nickname { get; private set; } = string.Empty;
        public RoomRole Role { get; private set; } = RoomRole.None;
        public CharacterId SelectedCharacter { get; private set; } =
            CharacterId.None;
        public bool IsReady { get; private set; }

        public bool IsOccupied => PlayerId > 0;

        public void Assign(
            int playerId,
            string nickname,
            RoomRole role)
        {
            if (playerId <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(playerId),
                    "玩家 ID 必须大于 0。");
            }

            if (string.IsNullOrWhiteSpace(nickname))
            {
                throw new ArgumentException(
                    "玩家昵称不能为空。",
                    nameof(nickname));
            }

            if (role == RoomRole.None)
            {
                throw new ArgumentException(
                    "玩家必须拥有房主或加入者身份。",
                    nameof(role));
            }

            PlayerId = playerId;
            Nickname = nickname.Trim();
            Role = role;
            SelectedCharacter = CharacterId.None;
            IsReady = false;
        }

        public void SelectCharacter(CharacterId character)
        {
            if (SelectedCharacter == character)
            {
                return;
            }

            SelectedCharacter = character;
            IsReady = false;
        }

        public void SetReady(bool ready)
        {
            IsReady = ready;
        }

        public void Clear()
        {
            PlayerId = 0;
            Nickname = string.Empty;
            Role = RoomRole.None;
            SelectedCharacter = CharacterId.None;
            IsReady = false;
        }
    }
}