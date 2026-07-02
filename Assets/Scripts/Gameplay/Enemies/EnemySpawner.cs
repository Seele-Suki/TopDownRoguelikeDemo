using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TopDownRoguelike.Gameplay.Enemies
{
    public class EnemySpawner : MonoBehaviour
    {
        [SerializeField] private GameObject[] enemyPrefabs;
        [SerializeField] private Transform player;
        [SerializeField] private float spawnInterval = 2f;
        [SerializeField] private float spawnDistance = 8f;

        private float nextSpawnTime;

        private void Update()
        {
            if (enemyPrefabs == null || enemyPrefabs.Length == 0 || player == null)
            {
                return;
            }

            if (Time.time < nextSpawnTime)
            {
                return;
            }

            SpawnEnemy();
            nextSpawnTime = Time.time + spawnInterval;
        }

        private void SpawnEnemy()
        {
            Vector2 randomDirection = Random.insideUnitCircle.normalized;
            Vector3 spawnPosition = player.position + (Vector3)(randomDirection * spawnDistance);
            spawnPosition.z = 0f;

            GameObject enemyPrefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];
            Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);
        }
    }
}