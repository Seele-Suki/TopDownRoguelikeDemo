using System.Collections;
using System.Collections.Generic;
using TopDownRoguelike.Gameplay.Combat;
using UnityEngine;

namespace TopDownRoguelike.Gameplay.Enemies
{
    public class EnemyAttack : MonoBehaviour
    {
        [SerializeField] private int attackDamage = 1;
        [SerializeField] private float attackRange = 1.3f;
        [SerializeField] private float attackCooldown = 1f;

        private Transform target;
        private float nextAttackTime;

        public float AttackRange => attackRange;

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
    }
}