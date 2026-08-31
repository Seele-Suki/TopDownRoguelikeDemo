using System;

namespace TopDownRoguelike.Gameplay.Networking
{
    public readonly struct PlayerHealthState
    {
        public PlayerHealthState(
            uint playerId,
            int currentHealth,
            int maxHealth)
        {
            if (playerId == 0u)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(playerId));
            }

            if (maxHealth < 1 ||
                maxHealth > ushort.MaxValue)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maxHealth));
            }

            if (currentHealth < 0 ||
                currentHealth > maxHealth)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(currentHealth));
            }

            PlayerId =
                playerId;

            CurrentHealth =
                (ushort)currentHealth;

            MaxHealth =
                (ushort)maxHealth;
        }

        public uint PlayerId { get; }

        public ushort CurrentHealth { get; }

        public ushort MaxHealth { get; }

        public bool IsDead =>
            CurrentHealth == 0;
    }
}