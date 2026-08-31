using System;
using TopDownRoguelike.Networking.Protocol;

namespace TopDownRoguelike.Gameplay.Networking
{
    public readonly struct EnemyHealthState
    {
        public EnemyHealthState(
            uint entityId,
            int currentHealth,
            int maxHealth,
            bool isDead,
            NetworkEnemyArchetype networkArchetype)
        {
            if (entityId == 0u)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(entityId));
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
                    "Enemy death state does not match health.",
                    nameof(isDead));
            }

            if (networkArchetype !=
                    NetworkEnemyArchetype.Basic &&
                networkArchetype !=
                    NetworkEnemyArchetype.Fast)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(networkArchetype));
            }

            EntityId =
                entityId;

            CurrentHealth =
                (ushort)currentHealth;

            MaxHealth =
                (ushort)maxHealth;

            IsDead =
                isDead;

            NetworkArchetype =
                networkArchetype;
        }

        public uint EntityId { get; }

        public ushort CurrentHealth { get; }

        public ushort MaxHealth { get; }

        public bool IsDead { get; }

        public NetworkEnemyArchetype NetworkArchetype { get; }
    }
}
