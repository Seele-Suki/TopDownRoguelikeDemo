using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TopDownRoguelike.Gameplay.Enemies
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class EnemyMovement : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 2f;
        [SerializeField] private float stopDistance = 1.1f;

        private Rigidbody2D rb;
        private Transform target;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
        }

        private void Start()
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");

            if (player != null)
            {
                target = player.transform;
            }
            else
            {
                Debug.LogWarning("EnemyMovement could not find Player tag.");
            }
        }

        private void FixedUpdate()
        {
            if (target == null)
            {
                rb.velocity = Vector2.zero;
                return;
            }

            Vector2 toTarget = (Vector2)target.position - rb.position;
            float distance = toTarget.magnitude;

            if (distance <= stopDistance)
            {
                rb.velocity = Vector2.zero;
                return;
            }

            Vector2 direction = toTarget.normalized;
            rb.velocity = direction * moveSpeed;
        }
    }
}