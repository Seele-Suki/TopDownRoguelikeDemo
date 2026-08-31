using TopDownRoguelike.Infrastructure;
using UnityEngine;

namespace TopDownRoguelike.Gameplay.Enemies
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class EnemyMovement : MonoBehaviour
    {
        [SerializeField] private EnemyData enemyData;

        private Rigidbody2D rb;
        private Transform target;
        private Vector2 moveDirection;

        public Vector2 MoveDirection =>
            moveDirection;

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
            if (GameSession.IsClient)
            {
                StopMovement();
                return;
            }

            float moveSpeed = enemyData != null ? enemyData.MoveSpeed : 2f;
            float stopDistance = enemyData != null ? enemyData.StopDistance : 1.1f;

            if (target == null)
            {
                StopMovement();
                return;
            }

            Vector2 toTarget = (Vector2)target.position - rb.position;
            float distance = toTarget.magnitude;

            if (distance <= stopDistance)
            {
                StopMovement();
                return;
            }

            Vector2 direction = toTarget.normalized;
            moveDirection = direction;
            rb.velocity = direction * moveSpeed;
        }

        private void StopMovement()
        {
            moveDirection = Vector2.zero;
            rb.velocity = Vector2.zero;
        }
    }
}
