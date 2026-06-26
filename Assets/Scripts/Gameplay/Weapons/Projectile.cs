using System.Collections;
using System.Collections.Generic;
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

        public void Initialize(Vector2 direction, GameObject source)
        {
            moveDirection = direction.normalized;
            owner = source;

            Destroy(gameObject, lifeTime);
        }

        private void Update()
        {
            transform.position += (Vector3)(moveDirection * speed * Time.deltaTime);
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

                Destroy(gameObject);
            }
        }
    }
}