using TopDownRoguelike.Gameplay.Combat;
using TopDownRoguelike.Gameplay.Experience;
using TopDownRoguelike.Infrastructure;
using TopDownRoguelike.Networking.Protocol;
using UnityEngine;
using System;

namespace TopDownRoguelike.Gameplay.Enemies
{
    public class EnemyHealth : MonoBehaviour, IDamageable
    {
        [SerializeField] private EnemyData enemyData;

        [SerializeField] private int currentHealth;
        [SerializeField] private float healthMultiplier = 1f;
        private int maxHealth;
        private bool isDead;

        public int CurrentHealth =>
            currentHealth;

        public int MaxHealth =>
            maxHealth;

        public bool IsDead =>
            isDead;

        public NetworkEnemyArchetype NetworkArchetype =>
            enemyData != null
                ? enemyData.NetworkArchetype
                : NetworkEnemyArchetype.Basic;

        public event Action OnDied;

        private void Awake()
        {
            ResetHealth();
        }

        public void TakeDamage(DamageInfo damageInfo)
        {
            if (GameSession.IsClient ||
                isDead)
            {
                return;
            }

            currentHealth =
                Mathf.Max(
                    0,
                    currentHealth -
                    damageInfo.Damage);

            Debug.Log($"Enemy took {damageInfo.Damage} damage. Current health: {currentHealth}");

            if (currentHealth <= 0)
            {
                Die();
            }
        }

        public bool ApplyAuthoritativeState(
            int authoritativeCurrentHealth,
            int authoritativeMaxHealth,
            bool authoritativeIsDead)
        {
            if (!GameSession.IsClient)
            {
                return false;
            }

            if (authoritativeMaxHealth < 1 ||
                authoritativeMaxHealth > ushort.MaxValue)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(authoritativeMaxHealth));
            }

            if (authoritativeCurrentHealth < 0 ||
                authoritativeCurrentHealth >
                    authoritativeMaxHealth)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(authoritativeCurrentHealth));
            }

            if (authoritativeIsDead !=
                (authoritativeCurrentHealth == 0))
            {
                throw new ArgumentException(
                    "Enemy death state must match current health.",
                    nameof(authoritativeIsDead));
            }

            maxHealth = authoritativeMaxHealth;
            currentHealth = authoritativeCurrentHealth;
            isDead = authoritativeIsDead;

            return true;
        }

        private void Die()
        {
            if (isDead)
            {
                return;
            }

            isDead = true;
            OnDied?.Invoke();
            DropExperience();
            Destroy(gameObject);
        }

        private void DropExperience()
        {
            if (ExperienceOrbPool.Instance == null)
            {
                Debug.LogWarning("No ExperienceOrbPool found in the scene.");
                return;
            }

            int experienceReward = enemyData != null ? enemyData.ExperienceReward : 1;
            ExperienceOrbPool.Instance.GetOrb(transform.position, experienceReward);
        }

        public void ApplyDifficulty(float multiplier)
        {
            healthMultiplier = Mathf.Max(1f, multiplier);
            ResetHealth();
        }

        private void ResetHealth()
        {
            int baseHealth =
                enemyData != null
                    ? enemyData.MaxHealth
                    : 3;

            maxHealth =
                Mathf.Max(
                    1,
                    Mathf.RoundToInt(
                        baseHealth *
                        healthMultiplier));

            currentHealth =
                maxHealth;
        }
    }
}
