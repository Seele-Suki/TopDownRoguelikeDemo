using System;
using TopDownRoguelike.Gameplay.Combat;
using TopDownRoguelike.Infrastructure;
using UnityEngine;

namespace TopDownRoguelike.Gameplay.Bosses
{
    public class BossHealth : MonoBehaviour, IDamageable
    {
        [SerializeField] private BossData bossData;

        [Header("Runtime Debug")]
        [SerializeField] private int currentHealth;
        [SerializeField] private bool isDead;

        public int CurrentHealth => currentHealth;
        public int MaxHealth =>
            bossData != null ? bossData.MaxHealth : 1;
        public bool IsDead => isDead;
        public float HealthNormalized =>
            MaxHealth > 0
                ? (float)currentHealth / MaxHealth
                : 0f;

        public event Action<int, int> OnHealthChanged;
        public event Action OnDied;

        private void Awake()
        {
            if (bossData == null)
            {
                Debug.LogError(
                    "BossHealth: BossData is not assigned.");

                enabled = false;
                return;
            }

            currentHealth = bossData.MaxHealth;
            isDead = false;
        }

        public void TakeDamage(DamageInfo damageInfo)
        {
            if (GameSession.IsClient ||
                isDead || damageInfo.Damage <= 0)
            {
                return;
            }

            currentHealth = Mathf.Max(
                0,
                currentHealth - damageInfo.Damage);

            Debug.Log(
                $"Boss took {damageInfo.Damage} damage. " +
                $"Current health: {currentHealth}");

            OnHealthChanged?.Invoke(
                currentHealth,
                MaxHealth);

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
            if (authoritativeMaxHealth != MaxHealth ||
                authoritativeMaxHealth < 1 ||
                authoritativeCurrentHealth < 0 ||
                authoritativeCurrentHealth > authoritativeMaxHealth ||
                authoritativeIsDead !=
                    (authoritativeCurrentHealth == 0))
            {
                return false;
            }

            bool wasDead = isDead;
            currentHealth = authoritativeCurrentHealth;
            isDead = authoritativeIsDead;

            OnHealthChanged?.Invoke(
                currentHealth,
                MaxHealth);

            if (!wasDead && isDead)
            {
                OnDied?.Invoke();
            }

            return true;
        }

        private void Die()
        {
            if (isDead)
            {
                return;
            }

            isDead = true;
            Debug.Log("Boss died.");

            OnDied?.Invoke();
            Destroy(gameObject);
        }
    }
}
