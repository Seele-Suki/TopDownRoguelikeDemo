using System.Collections;
using System.Collections.Generic;
using TopDownRoguelike.Gameplay.Combat;
using TopDownRoguelike.Infrastructure;
using TopDownRoguelike.Networking.Gameplay;
using TopDownRoguelike.Gameplay.Networking;
using UnityEngine;

namespace TopDownRoguelike.Gameplay.Enemies
{
    public class EnemyAttack : MonoBehaviour
    {
        [SerializeField] private EnemyData enemyData;
        [SerializeField] private float attackCooldownMultiplier = 1f;

        private Transform target;
        private NetworkPlayerRegistry playerRegistry;
        private float nextAttackTime;

        public float AttackRange => enemyData != null ? enemyData.AttackRange : 1.3f;

        private void Start()
        {
            ResolveTarget();
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

            ResolveTarget();
            if (target == null || Time.time < nextAttackTime)
            {
                return;
            }

            float distance = Vector2.Distance(transform.position, target.position);
            if (distance > attackRange)
            {
                return;
            }

            IDamageable damageable =
                target.GetComponentInParent<IDamageable>(true);
            if (damageable != null)
            {
                Vector2 hitDirection = (target.position - transform.position).normalized;
                DamageInfo damageInfo = new DamageInfo(attackDamage, hitDirection, gameObject);

                damageable.TakeDamage(damageInfo);
                nextAttackTime = Time.time + attackCooldown;
            }
        }

        public void ConfigureTargetRegistry(NetworkPlayerRegistry registry)
        {
            playerRegistry = registry;
            ResolveTarget();
        }

        private void ResolveTarget()
        {
            if (playerRegistry != null &&
                NetworkCombatTargetSelector.TrySelectNearest(
                    playerRegistry,
                    transform.position,
                    out _,
                    out Transform selected))
            {
                target = selected;
                return;
            }

            if (target != null)
                return;

            GameObject player = GameObject.FindGameObjectWithTag("Player");
            target = player != null ? player.transform : null;
        }

        public void ApplyDifficulty(float cooldownMultiplier)
        {
            attackCooldownMultiplier =
                Mathf.Clamp(cooldownMultiplier, 0.2f, 1f);
        }
    }
}
