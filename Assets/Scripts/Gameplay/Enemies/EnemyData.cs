using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TopDownRoguelike.Gameplay.Enemies
{
    [CreateAssetMenu(fileName = "EnemyData", menuName = "TopDown Roguelike/Enemy Data")]
    public class EnemyData : ScriptableObject
    {
        [Header("Health")]
        [SerializeField] private int maxHealth = 3;

        [Header("Movement")]
        [SerializeField] private float moveSpeed = 2f;
        [SerializeField] private float stopDistance = 1.1f;

        [Header("Attack")]
        [SerializeField] private int attackDamage = 1;
        [SerializeField] private float attackRange = 1.3f;
        [SerializeField] private float attackCooldown = 1f;

        [Header("Reward")]
        [SerializeField] private int experienceReward = 1;

        public int MaxHealth => maxHealth;
        public float MoveSpeed => moveSpeed;
        public float StopDistance => stopDistance;
        public int AttackDamage => attackDamage;
        public float AttackRange => attackRange;
        public float AttackCooldown => attackCooldown;
        public int ExperienceReward => experienceReward;
    }
}