using UnityEngine;
using TopDownRoguelike.Gameplay.Characters;

namespace TopDownRoguelike.Gameplay.Weapons
{
    public class PlayerShooter : MonoBehaviour
    {
        [SerializeField] private ProjectilePool projectilePool;
        [SerializeField] private Transform firePoint;
        [SerializeField] private float fireCooldown = 0.2f;

        private Camera mainCamera;
        private float nextFireTime;
        private PlayerHealth playerHealth;

        private void Awake()
        {
            playerHealth = GetComponent<PlayerHealth>();
            mainCamera = Camera.main;
        }

        private void Update()
        {
            if (playerHealth != null && playerHealth.IsDead)
            {
                return;
            }
            if (Input.GetMouseButton(0) && Time.time >= nextFireTime)
            {
                Fire();
                nextFireTime = Time.time + fireCooldown;
            }
        }

        private void Fire()
        {
            if (projectilePool == null)
            {
                Debug.LogWarning("PlayerShooter needs a ProjectilePool reference.");
                return;
            }

            Vector3 mouseWorldPosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            mouseWorldPosition.z = 0f;

            Vector2 fireDirection = (mouseWorldPosition - firePoint.position).normalized;

            Projectile projectile = projectilePool.GetProjectile(firePoint.position, Quaternion.identity);
            projectile.Initialize(fireDirection, gameObject);
        }
    }
}
