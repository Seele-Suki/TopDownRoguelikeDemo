using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TopDownRoguelike.Gameplay.Core;
using TopDownRoguelike.Infrastructure;
using TopDownRoguelike.Networking.Gameplay;
using TopDownRoguelike.Networking.Protocol;

namespace TopDownRoguelike.Gameplay.Enemies
{
    public class EnemySpawner : MonoBehaviour
    {
        [SerializeField] private EnemySpawnEntry[] enemyEntries;
        [SerializeField] private Transform player;
        [SerializeField] private RunConfig runConfig;
        [SerializeField] private float spawnDistance = 8f;
        [SerializeField] private GameManager gameManager;

        [SerializeField] private SpriteRenderer mapBounds;

        [SerializeField, Min(0f)]
        private float spawnPadding = 1f;

        [SerializeField, Min(1)]
        private int maxSpawnPositionAttempts = 16;

        private bool canSpawn;

        private float nextSpawnTime;

        private readonly List<GameObject> spawnedEnemies = new List<GameObject>();

        public event System.Action<GameObject> EnemySpawned;

        private uint nextNetworkEntityId =
            0x10000001u;

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
            if (!TryGetSpawnPosition(out Vector3 spawnPosition))
            {
                return;
            }

            GameObject enemyPrefab = SelectEnemyPrefab();

            if (enemyPrefab == null)
            {
                Debug.LogWarning(
                    "EnemySpawner: No available enemy prefab.");

                return;
            }
            TryCreateSpawnedEnemy(
                enemyPrefab,
                spawnPosition,
                out _);
        }

        private bool TryCreateSpawnedEnemy(
            GameObject enemyPrefab,
            Vector3 spawnPosition,
            out GameObject spawnedEnemy)
        {
            spawnedEnemy = null;

            if (GameSession.IsClient ||
                enemyPrefab == null)
            {
                return false;
            }

            spawnedEnemy = Instantiate(
                enemyPrefab,
                spawnPosition,
                Quaternion.identity);

            if (!EnsureNetworkEntityId(spawnedEnemy))
            {
                Destroy(spawnedEnemy);
                spawnedEnemy = null;
                return false;
            }

            if (spawnedEnemy.TryGetComponent(
                    out EnemyHealth enemyHealth))
            {
                enemyHealth.ApplyDifficulty(
                    currentHealthMultiplier);
                enemyHealth.OnDied += HandleEnemyDied;
            }

            if (spawnedEnemy.TryGetComponent(
                    out EnemyAttack enemyAttack))
            {
                enemyAttack.ApplyDifficulty(
                    currentAttackCooldownMultiplier);
            }

            spawnedEnemies.Add(spawnedEnemy);
            currentAliveEnemies = spawnedEnemies.Count;
            EnemySpawned?.Invoke(spawnedEnemy);
            return true;
        }

        private bool TryGetSpawnPosition(out Vector3 spawnPosition)
        {
            spawnPosition = Vector3.zero;

            if (mapBounds == null || player == null)
            {
                Debug.LogError(
                    "EnemySpawner: Map bounds or player is missing.");

                return false;
            }

            Bounds bounds = mapBounds.bounds;

            float minX = bounds.min.x + spawnPadding;
            float maxX = bounds.max.x - spawnPadding;
            float minY = bounds.min.y + spawnPadding;
            float maxY = bounds.max.y - spawnPadding;

            for (int i = 0; i < maxSpawnPositionAttempts; i++)
            {
                Vector2 direction = Random.insideUnitCircle;

                if (direction.sqrMagnitude < 0.0001f)
                {
                    continue;
                }

                direction.Normalize();

                Vector3 candidate =
                    player.position +
                    (Vector3)(direction * spawnDistance);

                candidate.z = 0f;

                bool isInside =
                    candidate.x >= minX &&
                    candidate.x <= maxX &&
                    candidate.y >= minY &&
                    candidate.y <= maxY;

                if (isInside)
                {
                    spawnPosition = candidate;
                    return true;
                }
            }

            Vector2 directionToCenter =
                (Vector2)bounds.center -
                (Vector2)player.position;

            if (directionToCenter.sqrMagnitude < 0.0001f)
            {
                directionToCenter = Vector2.up;
            }

            spawnPosition =
                player.position +
                (Vector3)(directionToCenter.normalized * spawnDistance);

            spawnPosition.x =
                Mathf.Clamp(spawnPosition.x, minX, maxX);

            spawnPosition.y =
                Mathf.Clamp(spawnPosition.y, minY, maxY);

            spawnPosition.z = 0f;
            return true;
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

        public void ClearSpawnedEnemies()
        {
            for (int i = spawnedEnemies.Count - 1;
                 i >= 0;
                 i--)
            {
                GameObject enemy =
                    spawnedEnemies[i];

                if (enemy != null)
                {
                    Destroy(enemy);
                }
            }

            spawnedEnemies.Clear();
            currentAliveEnemies = 0;

            Debug.Log(
                "EnemySpawner cleared all regular enemies.");
        }

        public IEnumerable<GameObject>
    EnumerateSpawnedEnemies()
        {
            foreach (GameObject enemy
                in spawnedEnemies)
            {
                if (enemy != null)
                {
                    yield return enemy;
                }
            }
        }

        private void HandleEnemyDied()
        {
            gameManager.NotifyEnemyKilled();
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

        private bool EnsureNetworkEntityId(
            GameObject enemy)
        {
            if (enemy == null)
            {
                return false;
            }

            NetworkEntityId identifier =
                enemy.GetComponent<NetworkEntityId>();

            if (identifier == null)
            {
                identifier =
                    enemy.AddComponent<NetworkEntityId>();
            }

            if (identifier.IsAssigned)
            {
                return identifier.EntityType ==
                    NetworkEntityType.Enemy;
            }

            uint entityId =
                nextNetworkEntityId++;

            return identifier.TryAssign(
                entityId,
                NetworkEntityType.Enemy);
        }
    }
}
