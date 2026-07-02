using System.Collections;
using System.Collections.Generic;
using TopDownRoguelike.Gameplay.Combat;
using UnityEngine;

namespace TopDownRoguelike.Gameplay.Characters
{
    public class PlayerHealth : MonoBehaviour, IDamageable
    {
        [SerializeField] private int maxHealth = 10;

        private int currentHealth;
        private bool isDead;

        public bool IsDead => isDead;

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

            currentHealth -= damageInfo.Damage;

            Debug.Log($"Player took {damageInfo.Damage} damage. Current health: {currentHealth}");

            if (currentHealth <= 0)
            {
                Die();
            }
        }

        private void Die()
        {
            isDead = true;
            currentHealth = 0;

            Debug.Log("Player died.");

            Time.timeScale = 0f;
        }
    }
}