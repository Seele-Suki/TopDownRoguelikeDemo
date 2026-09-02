using TopDownRoguelike.Infrastructure;
using TopDownRoguelike.Networking.Gameplay;
using TopDownRoguelike.Gameplay.Networking;
using UnityEngine;

namespace TopDownRoguelike.Gameplay.Enemies
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class EnemyMovement : MonoBehaviour
    {
        [SerializeField] private EnemyData enemyData;

        private Rigidbody2D rb;
        private Transform target;
        private NetworkPlayerRegistry playerRegistry;
        private Vector2 moveDirection;

        public Vector2 MoveDirection =>
            moveDirection;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
        }

        private void Start()
        {
            ResolveTarget();
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

            ResolveTarget();
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

        public void ConfigureTargetRegistry(NetworkPlayerRegistry registry)
        {
            playerRegistry = registry;
            ResolveTarget();
        }

        private void ResolveTarget()
        {
            if (playerRegistry != null &&
                NetworkCombatTargetSelector.TrySelectNearest(
                    playerRegistry,
                    rb != null ? rb.position : (Vector2)transform.position,
                    out _,
                    out Transform selected))
            {
                target = selected;
                return;
            }

            if (target != null)
                return;

            GameObject player = GameObject.FindGameObjectWithTag("Player");
            target = player != null ? player.transform : null;
        }

        private void StopMovement()
        {
            moveDirection = Vector2.zero;
            rb.velocity = Vector2.zero;
        }
    }
}
