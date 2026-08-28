using System;
using UnityEngine;
using TopDownRoguelike.Gameplay.Characters;
using TopDownRoguelike.Gameplay.Networking;

namespace TopDownRoguelike.Gameplay.Weapons
{
    public class PlayerShooter : MonoBehaviour
    {
        [SerializeField] private ProjectilePool projectilePool;
        [SerializeField] private Transform firePoint;
        [SerializeField] private float fireCooldown = 0.2f;
        [SerializeField] private int projectileDamage = 1;

        private float nextFireTime;
        private PlayerHealth playerHealth;
        private IPlayerInputSource inputSource;
        private PlayerShooterShotEventSource shotEventSource;

        private void Awake()
        {
            playerHealth =
                GetComponent<PlayerHealth>();

            inputSource =
                GetComponent<IPlayerInputSource>();

            shotEventSource =
                GetComponent<
                    TopDownRoguelike.Gameplay.Networking
                        .PlayerShooterShotEventSource>();

            if (inputSource == null)
            {
                Debug.LogError(
                    "PlayerShooter requires an " +
                    "IPlayerInputSource component.");

                enabled =
                    false;
            }
        }

        private void Update()
        {
            if (Time.timeScale <= 0f || (playerHealth != null && playerHealth.IsDead))
            {
                return;
            }
            if (inputSource != null &&
                inputSource.IsFireHeld &&
                Time.time >= nextFireTime)
            {
                Fire();

                nextFireTime =
                    Time.time + fireCooldown;
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

            enabled =
                true;
        }

        public void AddProjectileDamage(int amount)
        {
            projectileDamage += amount;
            projectileDamage = Mathf.Max(1, projectileDamage);
        }

        private void Fire()
        {
            if (projectilePool == null)
            {
                Debug.LogWarning("PlayerShooter needs a ProjectilePool reference.");
                return;
            }

            Vector2 fireDirection =
                inputSource.AimDirection;

            if (fireDirection.sqrMagnitude < 0.0001f)
            {
                return;
            }

            Projectile projectile = projectilePool.GetProjectile(firePoint.position, Quaternion.identity);
            projectile.Initialize(fireDirection, gameObject, projectileDamage,0);

            if (shotEventSource == null)
            {
                shotEventSource =
                    GetComponent<
                        TopDownRoguelike.Gameplay.Networking
                            .PlayerShooterShotEventSource>();
            }

            shotEventSource?.NotifyShot(
                fireDirection);
        }

        public void AddFireRate(float amount)
        {
            fireCooldown -= amount;
            fireCooldown = Mathf.Max(0.05f, fireCooldown);
        }
    }
}
