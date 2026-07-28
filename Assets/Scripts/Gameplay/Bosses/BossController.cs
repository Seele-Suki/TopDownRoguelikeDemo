using System.Collections;
using TopDownRoguelike.Gameplay.Combat;
using UnityEngine;

namespace TopDownRoguelike.Gameplay.Bosses
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class BossController : MonoBehaviour
    {
        private enum BossState
        {
            Chase,
            Windup,
            RadialAttack,
            Charging,
            Recovery,
            Dead
        }

        [Header("References")]
        [SerializeField] private BossData bossData;
        [SerializeField] private BossHealth bossHealth;
        [SerializeField]
        private BossProjectileEmitter projectileEmitter;

        [Header("Runtime Debug")]
        [SerializeField] private BossState currentState;
        [SerializeField] private bool isPhaseTwo;
        [SerializeField] private float nextAttackTime;

        private Rigidbody2D rb;
        private Transform target;
        private bool useProjectileNext = true;
        private float nextContactDamageTime;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();

            if (bossData == null ||
                bossHealth == null ||
                projectileEmitter == null)
            {
                Debug.LogError(
                    "BossController: " +
                    "Required references are missing.");

                enabled = false;
                return;
            }
        }

        private void OnEnable()
        {
            if (bossHealth != null)
            {
                bossHealth.OnDied += HandleBossDied;
            }
        }

        private void OnDisable()
        {
            if (bossHealth != null)
            {
                bossHealth.OnDied -= HandleBossDied;
            }

            StopAllCoroutines();

            if (rb != null)
            {
                rb.velocity = Vector2.zero;
            }
        }

        private void Start()
        {
            FindPlayer();

            currentState = BossState.Chase;
            ScheduleNextAttack();
        }

        private void Update()
        {
            if (bossHealth == null ||
                bossHealth.IsDead)
            {
                return;
            }

            if (target == null)
            {
                FindPlayer();
                return;
            }

            UpdatePhase();

            if (currentState == BossState.Chase &&
                Time.time >= nextAttackTime)
            {
                StartNextAttack();
            }
        }

        private void FixedUpdate()
        {
            if (currentState == BossState.Chase)
            {
                ChasePlayer();
            }
            else if (currentState != BossState.Charging)
            {
                rb.velocity = Vector2.zero;
            }
        }

        private void FindPlayer()
        {
            GameObject player =
                GameObject.FindGameObjectWithTag("Player");

            if (player != null)
            {
                target = player.transform;
            }
            else
            {
                Debug.LogWarning(
                    "BossController could not find Player.");
            }
        }

        private void ChasePlayer()
        {
            if (target == null)
            {
                rb.velocity = Vector2.zero;
                return;
            }

            Vector2 toTarget =
                (Vector2)target.position - rb.position;

            if (toTarget.magnitude <=
                bossData.StopDistance)
            {
                rb.velocity = Vector2.zero;
                return;
            }

            float moveSpeed = bossData.MoveSpeed;

            if (isPhaseTwo)
            {
                moveSpeed *=
                    bossData.PhaseTwoMoveSpeedMultiplier;
            }

            rb.velocity =
                toTarget.normalized * moveSpeed;
        }

        private void StartNextAttack()
        {
            rb.velocity = Vector2.zero;

            if (useProjectileNext)
            {
                StartCoroutine(
                    RadialAttackCoroutine());
            }
            else
            {
                StartCoroutine(
                    ChargeAttackCoroutine());
            }

            useProjectileNext = !useProjectileNext;
        }

        private IEnumerator RadialAttackCoroutine()
        {
            SetState(BossState.Windup);

            yield return new WaitForSeconds(
                bossData.WindupDuration);

            if (!CanAct())
            {
                yield break;
            }

            SetState(BossState.RadialAttack);

            projectileEmitter.FireRadial(
                isPhaseTwo);

            yield return null;

            SetState(BossState.Recovery);

            yield return new WaitForSeconds(
                bossData.RecoveryDuration);

            FinishAttack();
        }

        private IEnumerator ChargeAttackCoroutine()
        {
            SetState(BossState.Windup);
            rb.velocity = Vector2.zero;

            yield return new WaitForSeconds(
                bossData.WindupDuration);

            if (!CanAct())
            {
                yield break;
            }

            Vector2 chargeDirection =
                ((Vector2)target.position -
                 rb.position).normalized;

            if (chargeDirection.sqrMagnitude < 0.01f)
            {
                chargeDirection = Vector2.right;
            }

            SetState(BossState.Charging);

            float elapsedTime = 0f;

            while (elapsedTime <
                   bossData.ChargeDuration &&
                   CanAct())
            {
                rb.velocity =
                    chargeDirection *
                    bossData.ChargeSpeed;

                elapsedTime += Time.fixedDeltaTime;

                yield return new WaitForFixedUpdate();
            }

            rb.velocity = Vector2.zero;

            if (!CanAct())
            {
                yield break;
            }

            SetState(BossState.Recovery);

            yield return new WaitForSeconds(
                bossData.RecoveryDuration);

            FinishAttack();
        }

        private void FinishAttack()
        {
            if (!CanAct())
            {
                return;
            }

            SetState(BossState.Chase);
            ScheduleNextAttack();
        }

        private void ScheduleNextAttack()
        {
            float cooldown =
                bossData.AttackCooldown;

            if (isPhaseTwo)
            {
                cooldown *=
                    bossData.PhaseTwoCooldownMultiplier;
            }

            nextAttackTime =
                Time.time + cooldown;
        }

        private void UpdatePhase()
        {
            if (isPhaseTwo)
            {
                return;
            }

            if (bossHealth.HealthNormalized <=
                bossData.PhaseTwoHealthRatio)
            {
                isPhaseTwo = true;

                Debug.Log(
                    "Boss entered phase two.");
            }
        }

        private bool CanAct()
        {
            return enabled &&
                   target != null &&
                   bossHealth != null &&
                   !bossHealth.IsDead;
        }

        private void SetState(
            BossState newState)
        {
            currentState = newState;

            Debug.Log(
                $"Boss state changed to: {newState}");
        }

        private void OnCollisionStay2D(
            Collision2D collision)
        {
            bool canDealContactDamage =
                currentState == BossState.Chase ||
                currentState == BossState.Charging;

            if (!canDealContactDamage ||
                Time.time < nextContactDamageTime)
            {
                return;
            }

            GameObject targetObject =
                collision.gameObject
                    .transform.root.gameObject;

            if (!targetObject.CompareTag("Player"))
            {
                return;
            }

            IDamageable damageable =
                targetObject.GetComponent<IDamageable>();

            if (damageable == null)
            {
                return;
            }

            bool isCharging =
                currentState == BossState.Charging;

            int damage = isCharging
                ? bossData.ChargeDamage
                : bossData.ContactDamage;

            Vector2 hitDirection =
                ((Vector2)targetObject.transform.position -
                 rb.position).normalized;

            damageable.TakeDamage(
                new DamageInfo(
                    damage,
                    hitDirection,
                    gameObject));

            nextContactDamageTime =
                Time.time +
                bossData.ContactDamageCooldown;
        }

        private void HandleBossDied()
        {
            SetState(BossState.Dead);
            StopAllCoroutines();

            if (rb != null)
            {
                rb.velocity = Vector2.zero;
            }
        }
    }
}