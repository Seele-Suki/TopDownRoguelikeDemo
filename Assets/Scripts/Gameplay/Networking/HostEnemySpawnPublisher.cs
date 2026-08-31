using System;
using TopDownRoguelike.Gameplay.Enemies;
using TopDownRoguelike.Infrastructure;
using TopDownRoguelike.Networking.Gameplay;
using TopDownRoguelike.Networking.Protocol;
using UnityEngine;

namespace TopDownRoguelike.Gameplay.Networking
{
    public sealed class HostEnemySpawnPublisher
        : MonoBehaviour
    {
        private EnemySpawner enemySpawner;
        private Action<WorldEntityRecord> sendSpawn;

        public void Configure(
            EnemySpawner newEnemySpawner,
            Action<WorldEntityRecord> newSendSpawn)
        {
            if (newEnemySpawner == null)
            {
                throw new ArgumentNullException(
                    nameof(newEnemySpawner));
            }

            if (newSendSpawn == null)
            {
                throw new ArgumentNullException(
                    nameof(newSendSpawn));
            }

            Unsubscribe();

            enemySpawner =
                newEnemySpawner;

            sendSpawn =
                newSendSpawn;

            enemySpawner.EnemySpawned +=
                HandleEnemySpawned;
        }

        private void HandleEnemySpawned(
            GameObject enemy)
        {
            if (!GameSession.IsHost ||
                sendSpawn == null)
            {
                return;
            }

            sendSpawn(
                CreateSpawnRecord(enemy));
        }

        private static WorldEntityRecord CreateSpawnRecord(
            GameObject enemy)
        {
            if (enemy == null)
            {
                throw new ArgumentNullException(
                    nameof(enemy));
            }

            if (!enemy.TryGetComponent(
                    out NetworkEntityId identifier) ||
                !identifier.IsAssigned ||
                identifier.EntityType !=
                    NetworkEntityType.Enemy)
            {
                throw new InvalidOperationException(
                    "Spawned enemy must have an assigned " +
                    "Enemy NetworkEntityId.");
            }

            if (!enemy.TryGetComponent(
                    out EnemyHealth enemyHealth) ||
                enemyHealth.IsDead)
            {
                throw new InvalidOperationException(
                    "Spawned enemy must have live EnemyHealth.");
            }

            if (enemyHealth.CurrentHealth < 1 ||
                enemyHealth.CurrentHealth > ushort.MaxValue ||
                enemyHealth.MaxHealth < 1 ||
                enemyHealth.MaxHealth > ushort.MaxValue)
            {
                throw new InvalidOperationException(
                    "Spawned enemy health is outside " +
                    "the network range.");
            }

            Vector3 position =
                enemy.transform.position;

            WorldEntityFlags flags =
                enemy.activeSelf
                    ? WorldEntityFlags.Active
                    : WorldEntityFlags.None;

            return new WorldEntityRecord(
                identifier.EntityId,
                NetworkEntityType.Enemy,
                WorldEntityLifecycle.Spawn,
                flags,
                position.x,
                position.y,
                enemy.transform.eulerAngles.z,
                (ushort)enemyHealth.CurrentHealth,
                (ushort)enemyHealth.MaxHealth,
                0,
                enemyHealth.NetworkArchetype);
        }

        private void Unsubscribe()
        {
            if (enemySpawner != null)
            {
                enemySpawner.EnemySpawned -=
                    HandleEnemySpawned;
            }

            enemySpawner = null;
            sendSpawn = null;
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }
    }
}
