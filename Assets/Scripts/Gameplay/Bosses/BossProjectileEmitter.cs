using UnityEngine;

namespace TopDownRoguelike.Gameplay.Bosses
{
    public class BossProjectileEmitter : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private BossData bossData;
        [SerializeField] private BossProjectile projectilePrefab;
        [SerializeField] private Transform firePoint;

#if UNITY_EDITOR
        [Header("Editor Test")]
        [SerializeField]
        private KeyCode normalTestKey =
            KeyCode.B;

        [SerializeField]
        private KeyCode phaseTwoTestKey =
            KeyCode.N;
#endif

        private void Awake()
        {
            if (bossData == null ||
                projectilePrefab == null ||
                firePoint == null)
            {
                Debug.LogError(
                    "BossProjectileEmitter: " +
                    "Required references are missing.");

                enabled = false;
            }
        }

        public void FireRadial(bool isPhaseTwo)
        {
            if (bossData == null ||
                projectilePrefab == null ||
                firePoint == null)
            {
                return;
            }

            int projectileCount =
                bossData.ProjectileCount;

            if (isPhaseTwo)
            {
                projectileCount +=
                    bossData.PhaseTwoProjectileBonus;
            }

            projectileCount =
                Mathf.Max(1, projectileCount);

            float angleStep =
                360f / projectileCount;

            // 二阶段旋转半个间隔，避免两轮弹道完全重合。
            float startAngle =
                isPhaseTwo ? angleStep * 0.5f : 0f;

            for (int i = 0; i < projectileCount; i++)
            {
                float angle =
                    startAngle + angleStep * i;

                Vector2 direction =
                    Quaternion.Euler(0f, 0f, angle) *
                    Vector2.right;

                BossProjectile projectile =
                    Instantiate(
                        projectilePrefab,
                        firePoint.position,
                        Quaternion.identity);

                projectile.Initialize(
                    direction,
                    gameObject,
                    bossData.ProjectileDamage,
                    bossData.ProjectileSpeed,
                    bossData.ProjectileLifetime);
            }
        }

#if UNITY_EDITOR
        private void Update()
        {
            if (Input.GetKeyDown(normalTestKey))
            {
                FireRadial(false);
            }

            if (Input.GetKeyDown(phaseTwoTestKey))
            {
                FireRadial(true);
            }
        }
#endif
    }
}