using System.Collections;
using System.Collections.Generic;
using TopDownRoguelike.Gameplay.Combat;
using TopDownRoguelike.Gameplay.Experience;
using UnityEngine;

namespace TopDownRoguelike.Gameplay.Enemies
{
    public class EnemyHealth : MonoBehaviour, IDamageable
    {
        [SerializeField] private EnemyData enemyData;
        [SerializeField] private ExperienceOrb experienceOrbPrefab;

        private int currentHealth;
        private bool isDead;

        private void Awake()
        {
            currentHealth = enemyData != null ? enemyData.MaxHealth : 3;
        }

        public void TakeDamage(DamageInfo damageInfo)
        {
            if (isDead)
            {
                return;
            }

            currentHealth -= damageInfo.Damage;

            Debug.Log($"Enemy took {damageInfo.Damage} damage. Current health: {currentHealth}");

            if (currentHealth <= 0)
            {
                Die();
            }
        }
        private void Die()
        {
            isDead = true;
            DropExperience();
            Destroy(gameObject);
        }

        private void DropExperience()
        {
            if (experienceOrbPrefab == null)
            {
                Debug.LogWarning($"{name} has no ExperienceOrb prefab assigned.");
                return;
            }

            int experienceReward = enemyData != null ? enemyData.ExperienceReward : 1;
            ExperienceOrb orb = Instantiate(experienceOrbPrefab, transform.position, Quaternion.identity);
            orb.Initialize(experienceReward);
        }
    }
}