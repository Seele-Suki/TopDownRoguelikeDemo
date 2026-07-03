using TopDownRoguelike.Gameplay.Combat;
using TopDownRoguelike.Gameplay.Experience;
using UnityEngine;

namespace TopDownRoguelike.Gameplay.Enemies
{
    public class EnemyHealth : MonoBehaviour, IDamageable
    {
        [SerializeField] private EnemyData enemyData;

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
            if (ExperienceOrbPool.Instance == null)
            {
                Debug.LogWarning("No ExperienceOrbPool found in the scene.");
                return;
            }

            int experienceReward = enemyData != null ? enemyData.ExperienceReward : 1;
            ExperienceOrbPool.Instance.GetOrb(transform.position, experienceReward);
        }
    }
}