using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TopDownRoguelike.Gameplay.Core;

namespace TopDownRoguelike.Gameplay.Enemies
{
    public class EnemySpawner : MonoBehaviour
    {
        [SerializeField] private EnemySpawnEntry[] enemyEntries;
        [SerializeField] private Transform player;
        [SerializeField] private RunConfig runConfig;
        [SerializeField] private float spawnDistance = 8f;
        [SerializeField] private GameManager gameManager;

        private bool canSpawn;

        private float nextSpawnTime;

        private readonly List<GameObject> spawnedEnemies = new List<GameObject>();

        [Header("Runtime Debug")]
        [SerializeField] private float currentSpawnInterval;
        [SerializeField] private int currentMaxAliveEnemies;
        [SerializeField] private int currentAliveEnemies;
        [SerializeField] private float currentHealthMultiplier;
        [SerializeField] private float currentAttackCooldownMultiplier;

        private void Update()
        {
            if (!canSpawn || runConfig == null)
            {
                return;
            }

            if (enemyEntries == null || enemyEntries.Length == 0 || player == null)
            {
                return;
            }

            RemoveDestroyedEnemies();
            UpdateDifficultyValues();

            if (currentAliveEnemies >= currentMaxAliveEnemies)
            {
                return;
            }

            if (Time.time < nextSpawnTime)
            {
                return;
            }

            SpawnEnemy();

            nextSpawnTime =
                Time.time + currentSpawnInterval;
        }

        private void SpawnEnemy()
        {
            Vector2 randomDirection = Random.insideUnitCircle.normalized;
            Vector3 spawnPosition = player.position + (Vector3)(randomDirection * spawnDistance);
            spawnPosition.z = 0f;

            GameObject enemyPrefab = SelectEnemyPrefab();

            if (enemyPrefab == null)
            {
                Debug.LogWarning(
                    "EnemySpawner: No available enemy prefab.");

                return;
            }
            GameObject spawnedEnemy = Instantiate(enemyPrefab,spawnPosition,Quaternion.identity);

            if (spawnedEnemy.TryGetComponent(
                    out EnemyHealth enemyHealth))
            {
                enemyHealth.ApplyDifficulty(
                    currentHealthMultiplier);
            }

            if (spawnedEnemy.TryGetComponent(
                    out EnemyAttack enemyAttack))
            {
                enemyAttack.ApplyDifficulty(
                    currentAttackCooldownMultiplier);
            }

            spawnedEnemies.Add(spawnedEnemy);
            currentAliveEnemies = spawnedEnemies.Count;
        }

        private void UpdateDifficultyValues()
        {
            float elapsedTime = gameManager.ElapsedTime;

            currentSpawnInterval =
                runConfig.GetSpawnInterval(elapsedTime);

            currentMaxAliveEnemies =
                runConfig.GetMaxAliveEnemies(elapsedTime);

            currentAliveEnemies = spawnedEnemies.Count;

            currentHealthMultiplier = 
                runConfig.GetEnemyHealthMultiplier(elapsedTime);

            currentAttackCooldownMultiplier =
                runConfig.GetEnemyAttackCooldownMultiplier(elapsedTime);
        }

        private void RemoveDestroyedEnemies()
        {
            for (int i = spawnedEnemies.Count - 1; i >= 0; i--)
            {
                if (spawnedEnemies[i] == null)
                {
                    spawnedEnemies.RemoveAt(i);
                }
            }

            currentAliveEnemies = spawnedEnemies.Count;
        }

        private void OnEnable()
        {
            if (gameManager == null || runConfig == null)
            {
                Debug.LogError(
                    "EnemySpawner: GameManager or RunConfig is not assigned.");

                canSpawn = false;
                return;
            }

            gameManager.OnStateChanged += HandleGameStateChanged;
            HandleGameStateChanged(gameManager.CurrentState);
        }

        private void OnDisable()
        {
            if (gameManager != null)
            {
                gameManager.OnStateChanged -= HandleGameStateChanged;
            }
        }

        private void HandleGameStateChanged(GameState gameState)
        {
            canSpawn = gameState == GameState.Playing;

            if (canSpawn && runConfig != null)
            {
                UpdateDifficultyValues();

                nextSpawnTime =
                    Time.time + currentSpawnInterval;
            }
        }

        private GameObject SelectEnemyPrefab()
        {
            float runProgress =
                runConfig.GetRunProgress(gameManager.ElapsedTime);

            float totalWeight = 0f;

            for (int i = 0; i < enemyEntries.Length; i++)
            {
                totalWeight +=
                    enemyEntries[i].GetWeight(runProgress);
            }

            if (totalWeight <= 0f)
            {
                return null;
            }

            float randomValue = Random.Range(0f, totalWeight);

            for (int i = 0; i < enemyEntries.Length; i++)
            {
                float weight =
                    enemyEntries[i].GetWeight(runProgress);

                if (randomValue < weight)
                {
                    return enemyEntries[i].EnemyPrefab;
                }

                randomValue -= weight;
            }

            return null;
        }
    }
}