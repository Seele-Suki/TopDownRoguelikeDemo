using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TopDownRoguelike.Gameplay.Weapons
{
    public class PlayerShooter : MonoBehaviour
    {
        [SerializeField] private Projectile projectilePrefab;
        [SerializeField] private Transform firePoint;
        [SerializeField] private float fireCooldown = 0.2f;

        private Camera mainCamera;
        private float nextFireTime;

        private void Awake()
        {
            mainCamera = Camera.main;
        }

        private void Update()
        {
            if (Input.GetMouseButton(0) && Time.time >= nextFireTime)
            {
                Fire();
                nextFireTime = Time.time + fireCooldown;
            }
        }

        private void Fire()
        {
            Vector3 mouseWorldPosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            mouseWorldPosition.z = 0f;

            Vector2 fireDirection = (mouseWorldPosition - firePoint.position).normalized;

            Projectile projectile = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
            projectile.Initialize(fireDirection, gameObject);
        }
    }
}