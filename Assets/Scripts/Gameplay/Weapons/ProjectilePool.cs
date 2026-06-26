using System.Collections.Generic;
using UnityEngine;

namespace TopDownRoguelike.Gameplay.Weapons
{
    public class ProjectilePool : MonoBehaviour
    {
        [SerializeField] private Projectile projectilePrefab;
        [SerializeField] private int initialSize = 20;

        private readonly Queue<Projectile> availableProjectiles = new Queue<Projectile>();

        private void Awake()
        {
            for (int i = 0; i < initialSize; i++)
            {
                availableProjectiles.Enqueue(CreateProjectile());
            }
        }

        public Projectile GetProjectile(Vector3 position, Quaternion rotation)
        {
            Projectile projectile = availableProjectiles.Count > 0
                ? availableProjectiles.Dequeue()
                : CreateProjectile();

            projectile.transform.SetPositionAndRotation(position, rotation);
            projectile.gameObject.SetActive(true);

            return projectile;
        }

        public void ReleaseProjectile(Projectile projectile)
        {
            projectile.gameObject.SetActive(false);
            availableProjectiles.Enqueue(projectile);
        }

        private Projectile CreateProjectile()
        {
            Projectile projectile = Instantiate(projectilePrefab, transform);
            projectile.SetPool(this);
            projectile.gameObject.SetActive(false);

            return projectile;
        }
    }
}
