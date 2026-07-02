using System.Collections;
using System.Collections.Generic;
using TopDownRoguelike.Gameplay.Combat;
using UnityEngine;

namespace TopDownRoguelike.Gameplay.Enemies
{
    public class EnemyHealth : MonoBehaviour, IDamageable
    {
        [SerializeField] private EnemyData enemyData;

        private int currentHealth;

        private void Awake()
        {
            currentHealth = enemyData != null ? enemyData.MaxHealth : 3;
        }

        public void TakeDamage(DamageInfo damageInfo)
        {
            currentHealth -= damageInfo.Damage;

            Debug.Log($"Enemy took {damageInfo.Damage} damage. Current health: {currentHealth}");

            if (currentHealth <= 0)
            {
                Destroy(gameObject);
            }
        }
    }
}