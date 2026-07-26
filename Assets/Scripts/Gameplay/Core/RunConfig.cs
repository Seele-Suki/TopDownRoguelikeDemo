using UnityEngine;

namespace TopDownRoguelike.Gameplay.Core
{
    [CreateAssetMenu(
        fileName = "RunConfig",
        menuName = "TopDown Roguelike/Run Config")]
    public class RunConfig : ScriptableObject
    {
        [Header("Run")]
        [SerializeField, Min(1f)]
        private float bossStartTime = 240f;

        [Header("Difficulty")]
        [SerializeField]
        private AnimationCurve spawnIntervalCurve =
            new AnimationCurve(
                new Keyframe(0f, 2f),
                new Keyframe(0.25f, 1.6f),
                new Keyframe(0.5f, 1.2f),
                new Keyframe(0.75f, 0.9f),
                new Keyframe(1f, 0.75f));

        [SerializeField]
        private AnimationCurve maxAliveEnemyCurve =
            new AnimationCurve(
                new Keyframe(0f, 8f),
                new Keyframe(0.25f, 12f),
                new Keyframe(0.5f, 18f),
                new Keyframe(0.75f, 24f),
                new Keyframe(1f, 30f));

        [SerializeField]
        private AnimationCurve enemyHealthMultiplierCurve =
            new AnimationCurve(
                new Keyframe(0f, 1f),
                new Keyframe(0.5f, 1f),
                new Keyframe(0.75f, 1.1f),
                new Keyframe(1f, 1.2f));

        [SerializeField]
        private AnimationCurve enemyAttackCooldownMultiplierCurve =
            new AnimationCurve(
                new Keyframe(0f, 1f),
                new Keyframe(0.5f, 1f),
                new Keyframe(0.75f, 0.95f),
                new Keyframe(1f, 0.9f));

        public float BossStartTime => bossStartTime;

        public float GetSpawnInterval(float elapsedTime)
        {
            float normalizedTime = GetRunProgress(elapsedTime);

            return Mathf.Max(
                0.1f,
                spawnIntervalCurve.Evaluate(normalizedTime));
        }

        public int GetMaxAliveEnemies(float elapsedTime)
        {
            float normalizedTime = GetRunProgress(elapsedTime);

            return Mathf.Max(
                1,
                Mathf.RoundToInt(
                    maxAliveEnemyCurve.Evaluate(normalizedTime)));
        }

        public float GetRunProgress(float elapsedTime)
        {
            return Mathf.Clamp01(elapsedTime / bossStartTime);
        }

        public float GetEnemyHealthMultiplier(float elapsedTime)
        {
            float progress = GetRunProgress(elapsedTime);

            return Mathf.Max(
                1f,
                enemyHealthMultiplierCurve.Evaluate(progress));
        }

        public float GetEnemyAttackCooldownMultiplier(float elapsedTime)
        {
            float progress = GetRunProgress(elapsedTime);

            return Mathf.Clamp(
                enemyAttackCooldownMultiplierCurve.Evaluate(progress),
                0.2f,
                1f);
        }
    }
}