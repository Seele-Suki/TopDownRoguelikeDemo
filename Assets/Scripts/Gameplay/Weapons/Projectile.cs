using TopDownRoguelike.Gameplay.Combat;
using TopDownRoguelike.Gameplay.Bosses;
using TopDownRoguelike.Gameplay.Enemies;
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
        private int remainingPenetrations;

        public void SetPool(ProjectilePool projectilePool)
        {
            pool = projectilePool;
        }

        public void Initialize(Vector2 direction,GameObject source,int projectileDamage, int penetrationCount)
        {
            moveDirection = direction.normalized;
            owner = source;
            damage = projectileDamage;
            remainingLifeTime = lifeTime;
            remainingPenetrations = Mathf.Max(0, penetrationCount);
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
            if (!isActive || other.gameObject == owner)
            {
                return;
            }

            EnemyHealth enemyHealth =
                other.GetComponentInParent<EnemyHealth>();

            BossHealth bossHealth =
                other.GetComponentInParent<BossHealth>();

            if (enemyHealth != null || bossHealth != null)
            {
                DamageInfo damageInfo =
                    new DamageInfo(damage, moveDirection, owner);

                if (enemyHealth != null)
                {
                    enemyHealth.TakeDamage(damageInfo);
                }
                else
                {
                    bossHealth.TakeDamage(damageInfo);
                }

                if (remainingPenetrations > 0)
                {
                    remainingPenetrations--;
                }
                else
                {
                    Release();
                }
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
            remainingPenetrations = 0;

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
