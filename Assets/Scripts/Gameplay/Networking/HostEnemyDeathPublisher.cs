using System;
using System.Collections.Generic;
using TopDownRoguelike.Gameplay.Enemies;
using TopDownRoguelike.Infrastructure;
using TopDownRoguelike.Networking.Gameplay;
using TopDownRoguelike.Networking.Protocol;
using UnityEngine;

namespace TopDownRoguelike.Gameplay.Networking
{
    public sealed class HostEnemyDeathPublisher : MonoBehaviour
    {
        private readonly Dictionary<EnemyHealth, Action>
            deathHandlers = new Dictionary<EnemyHealth, Action>();

        private EnemySpawner enemySpawner;
        private Action<WorldEntityRemovedPayload> sendRemoval;

        public void Configure(
            EnemySpawner newEnemySpawner,
            Action<WorldEntityRemovedPayload> newSendRemoval)
        {
            if (newEnemySpawner == null)
            {
                throw new ArgumentNullException(nameof(newEnemySpawner));
            }

            if (newSendRemoval == null)
            {
                throw new ArgumentNullException(nameof(newSendRemoval));
            }

            Unsubscribe();

            enemySpawner = newEnemySpawner;
            sendRemoval = newSendRemoval;
            enemySpawner.EnemySpawned += HandleEnemySpawned;

            foreach (GameObject enemy
                in enemySpawner.EnumerateSpawnedEnemies())
            {
                if (enemy != null)
                {
                    SubscribeEnemy(enemy);
                }
            }
        }

        private void HandleEnemySpawned(GameObject enemy)
        {
            SubscribeEnemy(enemy);
        }

        private void SubscribeEnemy(GameObject enemy)
        {
            if (enemy == null ||
                !enemy.TryGetComponent(out EnemyHealth health) ||
                !enemy.TryGetComponent(out NetworkEntityId identifier) ||
                !identifier.IsAssigned ||
                identifier.EntityType != NetworkEntityType.Enemy)
            {
                throw new InvalidOperationException(
                    "Tracked enemy must have EnemyHealth and " +
                    "an assigned Enemy NetworkEntityId.");
            }

            if (deathHandlers.ContainsKey(health))
            {
                return;
            }

            Action handler = () => HandleEnemyDied(health, identifier);
            deathHandlers.Add(health, handler);
            health.OnDied += handler;
        }

        private void HandleEnemyDied(
            EnemyHealth health,
            NetworkEntityId identifier)
        {
            if (GameSession.IsHost &&
                sendRemoval != null &&
                identifier != null &&
                identifier.IsAssigned)
            {
                sendRemoval(
                    new WorldEntityRemovedPayload(
                        identifier.EntityId,
                        NetworkEntityType.Enemy,
                        WorldEntityRemovalReason.Died));
            }

            UnsubscribeEnemy(health);
        }

        private void UnsubscribeEnemy(EnemyHealth health)
        {
            if (health == null ||
                !deathHandlers.TryGetValue(health, out Action handler))
            {
                return;
            }

            health.OnDied -= handler;
            deathHandlers.Remove(health);
        }

        private void Unsubscribe()
        {
            if (enemySpawner != null)
            {
                enemySpawner.EnemySpawned -= HandleEnemySpawned;
            }

            foreach (KeyValuePair<EnemyHealth, Action> entry
                in deathHandlers)
            {
                if (entry.Key != null)
                {
                    entry.Key.OnDied -= entry.Value;
                }
            }

            deathHandlers.Clear();
            enemySpawner = null;
            sendRemoval = null;
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }
    }
}
