using TopDownRoguelike.Gameplay.Combat;
using UnityEngine;

namespace TopDownRoguelike.Gameplay.Bosses
{
    public class BossProjectile : MonoBehaviour
    {
        private Vector2 moveDirection;
        private GameObject owner;
        private int damage;
        private float speed;
        private float remainingLifetime;
        private bool initialized;

        public void Initialize(
            Vector2 direction,
            GameObject source,
            int projectileDamage,
            float projectileSpeed,
            float lifetime)
        {
            moveDirection = direction.normalized;
            owner = source;
            damage = Mathf.Max(1, projectileDamage);
            speed = Mathf.Max(0.1f, projectileSpeed);
            remainingLifetime = Mathf.Max(0.1f, lifetime);
            initialized = true;

            float angle = Mathf.Atan2(
                moveDirection.y,
                moveDirection.x) * Mathf.Rad2Deg;

            transform.rotation =
                Quaternion.Euler(0f, 0f, angle);
        }

        private void Update()
        {
            if (!initialized)
            {
                return;
            }

            transform.position +=
                (Vector3)(moveDirection * speed * Time.deltaTime);

            remainingLifetime -= Time.deltaTime;

            if (remainingLifetime <= 0f)
            {
                Destroy(gameObject);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!initialized)
            {
                return;
            }

            GameObject target =
                other.transform.root.gameObject;

            if (target == owner ||
                !target.CompareTag("Player"))
            {
                return;
            }

            IDamageable damageable =
                target.GetComponent<IDamageable>();

            if (damageable == null)
            {
                return;
            }

            Vector2 hitDirection =
                ((Vector2)target.transform.position -
                 (Vector2)transform.position).normalized;

            damageable.TakeDamage(
                new DamageInfo(
                    damage,
                    hitDirection,
                    owner));

            Destroy(gameObject);
        }
    }
}
