using System;
using TopDownRoguelike.Gameplay.Networking;
using TopDownRoguelike.Gameplay.Skills;
using TopDownRoguelike.Gameplay.Weapons;
using UnityEngine;

namespace TopDownRoguelike.Gameplay.Characters
{
    public class ShotgunSkill : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private ShotgunData shotgunData;
        [SerializeField] private ProjectilePool projectilePool;
        [SerializeField] private Transform firePoint;

        [Header("Runtime Debug")]
        [SerializeField] private int projectileCount;
        [SerializeField] private float spreadAngle;
        [SerializeField] private int projectileDamage;
        [SerializeField] private int penetrationCount;
        [SerializeField] private float cooldown;
        [SerializeField] private float cooldownRemaining;

        private PlayerHealth playerHealth;
        private IPlayerInputSource inputSource;
        private uint lastProcessedShotgunRequestSequence;
        private PlayerShotgunEventSource shotgunEventSource;

        public bool IsReady =>
            cooldownRemaining <= 0f;

        public float CooldownRemaining =>
            cooldownRemaining;

        public float CooldownNormalized =>
            cooldown > 0f
                ? Mathf.Clamp01(
                    cooldownRemaining / cooldown)
                : 0f;

        private void Awake()
        {
            playerHealth =
                GetComponent<PlayerHealth>();

            inputSource =
                GetComponent<IPlayerInputSource>();

            if (inputSource == null)
            {
                Debug.LogError(
                    "ShotgunSkill requires an " +
                    "IPlayerInputSource component.",
                    this);

                enabled = false;
                return;
            }

            lastProcessedShotgunRequestSequence =
                inputSource.ShotgunRequestSequence;

            if (shotgunData == null ||
                projectilePool == null ||
                firePoint == null ||
                playerHealth == null)
            {
                Debug.LogError(
                    "ShotgunSkill: Required references " +
                    "are missing.",
                    this);

                enabled = false;
                return;
            }

            cooldown =
                Mathf.Max(
                    0f,
                    shotgunData.Cooldown);

            cooldownRemaining = 0f;

            projectileCount =
                Mathf.Max(
                    1,
                    shotgunData.ProjectileCount);

            spreadAngle =
                shotgunData.SpreadAngle;

            projectileDamage =
                shotgunData.ProjectileDamage;

            penetrationCount =
                Mathf.Max(
                    0,
                    shotgunData.PenetrationCount);
        }

        private void Update()
        {
            bool hasShotgunRequest =
                TryConsumeShotgunRequest();

            if (Time.timeScale <= 0f ||
                (playerHealth != null &&
                 playerHealth.IsDead))
            {
                return;
            }

            if (cooldownRemaining > 0f)
            {
                cooldownRemaining =
                    Mathf.Max(
                        0f,
                        cooldownRemaining -
                        Time.deltaTime);
            }

            if (hasShotgunRequest && IsReady)
            {
                FireShotgun();
            }
        }

        public void SetInputSource(
            IPlayerInputSource newInputSource)
        {
            if (newInputSource == null)
            {
                throw new ArgumentNullException(
                    nameof(newInputSource));
            }

            inputSource =
                newInputSource;

            lastProcessedShotgunRequestSequence =
                inputSource.ShotgunRequestSequence;

            enabled = true;
        }

        public void SetShotgunEventSource(
            PlayerShotgunEventSource newEventSource)
        {
            if (newEventSource == null)
            {
                throw new ArgumentNullException(
                    nameof(newEventSource));
            }

            shotgunEventSource =
                newEventSource;
        }

        private bool TryConsumeShotgunRequest()
        {
            if (inputSource == null)
            {
                return false;
            }

            uint currentSequence =
                inputSource.ShotgunRequestSequence;

            uint difference =
                unchecked(
                    currentSequence -
                    lastProcessedShotgunRequestSequence);

            if (difference == 0u ||
                difference >= 0x80000000u)
            {
                return false;
            }

            lastProcessedShotgunRequestSequence =
                currentSequence;

            return true;
        }

        private Vector2 GetShotgunDirection()
        {
            if (inputSource == null)
            {
                return Vector2.zero;
            }

            Vector2 aimDirection =
                inputSource.AimDirection;

            if (aimDirection.sqrMagnitude < 0.01f)
            {
                return Vector2.zero;
            }

            return aimDirection.normalized;
        }

        public void AddProjectileDamage(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            long upgradedProjectileDamage =
                (long)projectileDamage +
                amount;

            projectileDamage =
                (int)Math.Min(
                    int.MaxValue,
                    Math.Max(
                        1L,
                        upgradedProjectileDamage));

            Debug.Log(
                $"Shotgun projectile damage: " +
                $"{projectileDamage}");
        }

        public void AddProjectileCount(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            long upgradedProjectileCount =
                (long)projectileCount +
                amount;

            projectileCount =
                (int)Math.Min(
                    shotgunData.MaxProjectileCount,
                    upgradedProjectileCount);

            Debug.Log(
                $"Shotgun projectile count: " +
                $"{projectileCount}");
        }

        public void ReduceCooldown(float amount)
        {
            if (amount <= 0f ||
                float.IsNaN(amount) ||
                float.IsInfinity(amount))
            {
                return;
            }

            cooldown =
                Mathf.Max(
                    shotgunData.MinCooldown,
                    cooldown - amount);

            cooldownRemaining =
                Mathf.Min(
                    cooldownRemaining,
                    cooldown);

            Debug.Log(
                $"Shotgun cooldown: " +
                $"{cooldown:F2}");
        }

        public void AddPenetration(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            long upgradedPenetrationCount =
                (long)penetrationCount +
                amount;

            penetrationCount =
                (int)Math.Min(
                    shotgunData.MaxPenetrationCount,
                    upgradedPenetrationCount);

            Debug.Log(
                $"Shotgun penetration: " +
                $"{penetrationCount}");
        }

        private void FireShotgun()
        {
            Vector2 centerDirection =
                GetShotgunDirection();

            if (centerDirection.sqrMagnitude < 0.01f)
            {
                return;
            }

            float angleStep =
                projectileCount > 1
                    ? spreadAngle /
                      (projectileCount - 1)
                    : 0f;

            float startAngle =
                projectileCount > 1
                    ? -spreadAngle * 0.5f
                    : 0f;

            for (int i = 0;
                 i < projectileCount;
                 i++)
            {
                float currentAngle =
                    startAngle +
                    angleStep * i;

                Vector2 projectileDirection =
                    (Vector2)(
                        Quaternion.Euler(
                            0f,
                            0f,
                            currentAngle) *
                        centerDirection);

                FireProjectile(
                    projectileDirection);
            }

            cooldownRemaining =
                cooldown;

            shotgunEventSource?.NotifyShotgun(
                centerDirection,
                (uint)projectileCount,
                spreadAngle,
                cooldown);
        }

        private void FireProjectile(
            Vector2 direction)
        {
            Projectile projectile =
                projectilePool.GetProjectile(
                    firePoint.position,
                    Quaternion.identity);

            projectile.Initialize(
                direction,
                gameObject,
                projectileDamage,
                penetrationCount);
        }
    }
}