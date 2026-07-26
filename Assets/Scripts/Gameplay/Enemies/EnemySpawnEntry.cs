using System;
using UnityEngine;

namespace TopDownRoguelike.Gameplay.Enemies
{
    [Serializable]
    public class EnemySpawnEntry
    {
        [SerializeField] private GameObject enemyPrefab;

        [SerializeField, Range(0f, 1f)]
        private float unlockProgress;

        [SerializeField]
        private AnimationCurve weightCurve =
            AnimationCurve.Linear(0f, 1f, 1f, 1f);

        public GameObject EnemyPrefab => enemyPrefab;

        public float GetWeight(float runProgress)
        {
            if (enemyPrefab == null ||
                runProgress < unlockProgress)
            {
                return 0f;
            }

            return Mathf.Max(
                0f,
                weightCurve.Evaluate(runProgress));
        }
    }
}