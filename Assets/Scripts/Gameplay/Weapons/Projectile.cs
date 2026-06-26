using TopDownRoguelike.Gameplay.Combat;
using UnityEngine;

namespace TopDownRoguelike.Gameplay.Weapons
{
    public class Projectile : MonoBehaviour
    {
        [SerializeField] private float speed = 12f;
        [SerializeField] private int damage = 1;
        [SerializeField] private float lifeTime = 2f;

        private Vector2 moveDirection;
        private GameObject owner;
        private ProjectilePool pool;
        private float remainingLifeTime;
        private bool isActive;

        public void SetPool(ProjectilePool projectilePool)
        {
            pool = projectilePool;
        }

        public void Initialize(Vector2 direction, GameObject source)
        {
            moveDirection = direction.normalized;
            owner = source;
            remainingLifeTime = lifeTime;
            isActive = true;
        }

        private void Update()
        {
            if (!isActive)
            {
                return;
            }

            transform.position += (Vector3)(moveDirection * speed * Time.deltaTime);

            remainingLifeTime -= Time.deltaTime;
            if (remainingLifeTime <= 0f)
            {
                Release();
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.gameObject == owner)
            {
                return;
            }

            if (other.TryGetComponent(out IDamageable damageable))
            {
                DamageInfo damageInfo = new DamageInfo(damage, moveDirection, owner);
                damageable.TakeDamage(damageInfo);

                Release();
            }
        }

        private void Release()
        {
            if (!isActive)
            {
                return;
            }

            isActive = false;
            owner = null;
            moveDirection = Vector2.zero;

            if (pool != null)
            {
                pool.ReleaseProjectile(this);
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
}
