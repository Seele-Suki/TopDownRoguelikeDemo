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

        private Camera mainCamera;
        private PlayerHealth playerHealth;
        public bool IsReady => cooldownRemaining <= 0f;
        public float CooldownRemaining => cooldownRemaining;
        public float CooldownNormalized =>
            cooldown > 0f
                ? Mathf.Clamp01(cooldownRemaining / cooldown)
                : 0f;

        private void Awake()
        {
            mainCamera = Camera.main;
            playerHealth = GetComponent<PlayerHealth>();

            if (shotgunData == null ||
                projectilePool == null ||
                firePoint == null ||
                mainCamera == null ||
                playerHealth == null)
            {
                Debug.LogError(
                    "ShotgunSkill: Required references are missing.");

                enabled = false;
                return;
            }

            cooldown = Mathf.Max(0f, shotgunData.Cooldown);
            cooldownRemaining = 0f;
            projectileCount =
                Mathf.Max(1, shotgunData.ProjectileCount);

            spreadAngle = shotgunData.SpreadAngle;
            projectileDamage = shotgunData.ProjectileDamage;

            penetrationCount =
                Mathf.Max(0, shotgunData.PenetrationCount);
        }

        private void Update()
        {
            if (Time.timeScale <= 0f || playerHealth.IsDead)
            {
                return;
            }

            if (cooldownRemaining > 0f)
            {
                cooldownRemaining = Mathf.Max(
                    0f,
                    cooldownRemaining - Time.deltaTime);
            }

            if (Input.GetMouseButtonDown(1) && IsReady)
            {
                FireShotgun();
            }
        }

        public void AddProjectileDamage(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            projectileDamage =
                Mathf.Max(1, projectileDamage + amount);

            Debug.Log(
                $"Shotgun projectile damage: {projectileDamage}");
        }

        public void AddProjectileCount(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            projectileCount = Mathf.Min(
                shotgunData.MaxProjectileCount,
                projectileCount + amount);

            Debug.Log($"Shotgun projectile count: {projectileCount}");
        }

        public void ReduceCooldown(float amount)
        {
            if (amount <= 0f)
            {
                return;
            }

            cooldown = Mathf.Max(
                shotgunData.MinCooldown,
                cooldown - amount);

            cooldownRemaining = Mathf.Min(
                cooldownRemaining,
                cooldown);

            Debug.Log($"Shotgun cooldown: {cooldown:F2}");
        }

        public void AddPenetration(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            penetrationCount = Mathf.Min(
                shotgunData.MaxPenetrationCount,
                penetrationCount + amount);

            Debug.Log($"Shotgun penetration: {penetrationCount}");
        }

        private void FireShotgun()
        {
            Vector3 mouseWorldPosition =
                mainCamera.ScreenToWorldPoint(Input.mousePosition);

            mouseWorldPosition.z = 0f;

            Vector2 centerDirection =
                ((Vector2)mouseWorldPosition -
                 (Vector2)firePoint.position).normalized;

            if (centerDirection.sqrMagnitude < 0.01f)
            {
                return;
            }

            float angleStep = projectileCount > 1
                ? spreadAngle / (projectileCount - 1)
                : 0f;

            float startAngle = projectileCount > 1
                ? -spreadAngle * 0.5f
                : 0f;

            for (int i = 0; i < projectileCount; i++)
            {
                float currentAngle = startAngle + angleStep * i;

                Vector2 projectileDirection =
                    (Vector2)(Quaternion.Euler(
                        0f,
                        0f,
                        currentAngle) * centerDirection);

                FireProjectile(projectileDirection);
            }
            cooldownRemaining = cooldown;
        }

        private void FireProjectile(Vector2 direction)
        {
            Projectile projectile = projectilePool.GetProjectile(
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