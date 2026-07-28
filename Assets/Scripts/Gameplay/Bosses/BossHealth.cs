using System;
using TopDownRoguelike.Gameplay.Combat;
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
            if (isDead || damageInfo.Damage <= 0)
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