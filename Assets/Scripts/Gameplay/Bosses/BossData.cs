using UnityEngine;

namespace TopDownRoguelike.Gameplay.Bosses
{
    [CreateAssetMenu(
        fileName = "BossData",
        menuName = "TopDown Roguelike/Boss Data")]
    public class BossData : ScriptableObject
    {
        [Header("Health")]
        [SerializeField, Min(1)]
        private int maxHealth = 100;

        [Header("Movement")]
        [SerializeField, Min(0f)]
        private float moveSpeed = 2f;

        [SerializeField, Min(0f)]
        private float stopDistance = 3.5f;

        [Header("Attack Timing")]
        [SerializeField, Min(0.1f)]
        private float attackCooldown = 1.8f;

        [SerializeField, Min(0f)]
        private float windupDuration = 0.5f;

        [SerializeField, Min(0f)]
        private float recoveryDuration = 0.6f;

        [Header("Projectile Attack")]
        [SerializeField, Min(1)]
        private int projectileCount = 12;

        [SerializeField, Min(1)]
        private int projectileDamage = 1;

        [SerializeField, Min(0.1f)]
        private float projectileSpeed = 7f;

        [SerializeField, Min(0.1f)]
        private float projectileLifetime = 4f;

        [Header("Contact Attack")]
        [SerializeField, Min(1)]
        private int contactDamage = 1;

        [SerializeField, Min(0.1f)]
        private float contactDamageCooldown = 1f;

        [Header("Charge Attack")]
        [SerializeField, Min(0.1f)]
        private float chargeSpeed = 10f;

        [SerializeField, Min(0.1f)]
        private float chargeDuration = 0.45f;

        [SerializeField, Min(1)]
        private int chargeDamage = 2;

        [Header("Phase Two")]
        [SerializeField, Range(0.1f, 0.9f)]
        private float phaseTwoHealthRatio = 0.5f;

        [SerializeField, Min(1f)]
        private float phaseTwoMoveSpeedMultiplier = 1.25f;

        [SerializeField, Range(0.2f, 1f)]
        private float phaseTwoCooldownMultiplier = 0.75f;

        [SerializeField, Min(0)]
        private int phaseTwoProjectileBonus = 6;

        public int MaxHealth => maxHealth;
        public float MoveSpeed => moveSpeed;
        public float StopDistance => stopDistance;
        public float AttackCooldown => attackCooldown;
        public float WindupDuration => windupDuration;
        public float RecoveryDuration => recoveryDuration;
        public int ProjectileCount => projectileCount;
        public int ProjectileDamage => projectileDamage;
        public float ProjectileSpeed => projectileSpeed;
        public float ProjectileLifetime => projectileLifetime;
        public float ChargeSpeed => chargeSpeed;
        public float ChargeDuration => chargeDuration;
        public int ChargeDamage => chargeDamage;
        public float PhaseTwoHealthRatio => phaseTwoHealthRatio;
        public float PhaseTwoMoveSpeedMultiplier =>
            phaseTwoMoveSpeedMultiplier;
        public float PhaseTwoCooldownMultiplier =>
            phaseTwoCooldownMultiplier;
        public int PhaseTwoProjectileBonus =>
            phaseTwoProjectileBonus;

        public int ContactDamage => contactDamage;
        public float ContactDamageCooldown =>
            contactDamageCooldown;
    }
}