using System.Collections;
using System.Collections.Generic;
using TopDownRoguelike.Gameplay.Combat;
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

        [SerializeField] private bool isInvulnerable;

        public bool IsDead => isDead;
        public bool IsInvulnerable => isInvulnerable;

        private void Awake()
        {
            currentHealth = maxHealth;
        }

        public void TakeDamage(DamageInfo damageInfo)
        {
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
            }
        }

        private void Die()
        {
            if (isDead)
            {
                return;
            }

            isDead = true;
            currentHealth = 0;

            Debug.Log("Player died.");

            OnDied?.Invoke();
        }

        public void SetInvulnerable(bool value)
        {
            isInvulnerable = value;
        }

        public void AddMaxHealth(int amount)
        {
            maxHealth += amount;
            currentHealth += amount;

            if (maxHealth < 1)
            {
                maxHealth = 1;
            }

            currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        }
    }
}