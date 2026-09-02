using System.Collections;
using System.Collections.Generic;
using TopDownRoguelike.Gameplay.Combat;
using TopDownRoguelike.Infrastructure;
using UnityEngine;
using System;

namespace TopDownRoguelike.Gameplay.Characters
{
    public class PlayerHealth : MonoBehaviour, IDamageable
    {
        [SerializeField] private int maxHealth = 10;

        private int currentHealth;
        private bool isDead;
        public event Action OnDied;
        public event Action<int, int> OnHealthChanged;

        [SerializeField] private bool isInvulnerable;

        public bool IsDead => isDead;
        public bool IsInvulnerable => isInvulnerable;
        public int CurrentHealth => currentHealth;
        public int MaxHealth => maxHealth;

        private void Awake()
        {
            currentHealth = maxHealth;
        }

        public void TakeDamage(DamageInfo damageInfo)
        {
            if (GameSession.IsClient)
            {
                return;
            }

            if (isDead)
            {
                return;
            }

            if (isInvulnerable)
            {
                Debug.Log("Player ignored damage while invulnerable.");
                return;
            }

            currentHealth -= damageInfo.Damage;

            Debug.Log($"Player took {damageInfo.Damage} damage. Current health: {currentHealth}");

            if (currentHealth <= 0)
            {
                Die();
                return;
            }

            NotifyHealthChanged();
        }

        private void Die()
        {
            if (isDead)
            {
                return;
            }

            isDead = true;
            currentHealth = 0;

            NotifyHealthChanged();

            Debug.Log("Player died.");

            OnDied?.Invoke();
        }

        public void SetInvulnerable(bool value)
        {
            isInvulnerable = value;
        }

        public void AddMaxHealth(int amount)
        {
            if (GameSession.IsClient)
            {
                return;
            }

            ApplyMaxHealthUpgrade(amount);
        }

        public void ApplyAuthoritativeMaxHealthUpgrade(int amount)
        {
            ApplyMaxHealthUpgrade(amount);
        }

        private void ApplyMaxHealthUpgrade(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            maxHealth += amount;
            currentHealth += amount;

            if (maxHealth < 1)
            {
                maxHealth = 1;
            }

            currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

            NotifyHealthChanged();
        }

        public bool ApplyAuthoritativeState(
            int authoritativeCurrentHealth,
            int authoritativeMaxHealth)
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

            bool wasDead = isDead;
            bool healthChanged =
                currentHealth != authoritativeCurrentHealth ||
                maxHealth != authoritativeMaxHealth;

            maxHealth = authoritativeMaxHealth;
            currentHealth = authoritativeCurrentHealth;
            isDead = authoritativeCurrentHealth == 0;

            if (healthChanged)
            {
                NotifyHealthChanged();
            }

            if (!wasDead && isDead)
            {
                OnDied?.Invoke();
            }

            return true;
        }

        private void NotifyHealthChanged()
        {
            OnHealthChanged?.Invoke(
                currentHealth,
                maxHealth);
        }
    }
}
