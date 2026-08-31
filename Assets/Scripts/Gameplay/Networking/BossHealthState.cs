using System;

namespace TopDownRoguelike.Gameplay.Networking
{
    public readonly struct BossHealthState
    {
        public BossHealthState(
            uint entityId,
            byte phase,
            int currentHealth,
            int maxHealth,
            bool isDead)
        {
            if (entityId == 0u)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(entityId));
            }

            if (phase < 1 ||
                phase > 2)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(phase));
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

            if (isDead != (currentHealth == 0))
            {
                throw new ArgumentException(
                    "Boss death state does not match health.",
                    nameof(isDead));
            }

            EntityId =
                entityId;

            Phase =
                phase;

            CurrentHealth =
                (ushort)currentHealth;

            MaxHealth =
                (ushort)maxHealth;

            IsDead =
                isDead;
        }

        public uint EntityId { get; }

        public byte Phase { get; }

        public ushort CurrentHealth { get; }

        public ushort MaxHealth { get; }

        public bool IsDead { get; }
    }
}