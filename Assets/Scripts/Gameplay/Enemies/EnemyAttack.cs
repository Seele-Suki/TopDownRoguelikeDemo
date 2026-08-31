using System.Collections;
using System.Collections.Generic;
using TopDownRoguelike.Gameplay.Combat;
using TopDownRoguelike.Infrastructure;
using UnityEngine;

namespace TopDownRoguelike.Gameplay.Enemies
{
    public class EnemyAttack : MonoBehaviour
    {
        [SerializeField] private EnemyData enemyData;
        [SerializeField] private float attackCooldownMultiplier = 1f;

        private Transform target;
        private float nextAttackTime;

        public float AttackRange => enemyData != null ? enemyData.AttackRange : 1.3f;

        private void Start()
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");

            if (player != null)
            {
                target = player.transform;
            }
            else
            {
                Debug.LogWarning("EnemyAttack could not find Player tag.");
            }
        }

        private void Update()
        {
            if (GameSession.IsClient)
            {
                return;
            }

            int attackDamage = enemyData != null ? enemyData.AttackDamage : 1;
            float attackRange = enemyData != null ? enemyData.AttackRange : 1.3f;
            float baseAttackCooldown = enemyData != null ? enemyData.AttackCooldown : 1f;
            float attackCooldown = baseAttackCooldown * attackCooldownMultiplier;

            if (target == null || Time.time < nextAttackTime)
            {
                return;
            }

            float distance = Vector2.Distance(transform.position, target.position);
            if (distance > attackRange)
            {
                return;
            }

            if (target.TryGetComponent(out IDamageable damageable))
            {
                Vector2 hitDirection = (target.position - transform.position).normalized;
                DamageInfo damageInfo = new DamageInfo(attackDamage, hitDirection, gameObject);

                damageable.TakeDamage(damageInfo);
                nextAttackTime = Time.time + attackCooldown;
            }
        }

        public void ApplyDifficulty(float cooldownMultiplier)
        {
            attackCooldownMultiplier =
                Mathf.Clamp(cooldownMultiplier, 0.2f, 1f);
        }
    }
}
