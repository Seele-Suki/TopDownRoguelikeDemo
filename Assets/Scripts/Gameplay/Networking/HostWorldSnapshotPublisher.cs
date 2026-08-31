using System;
using System.Collections.Generic;
using TopDownRoguelike.Gameplay.Bosses;
using TopDownRoguelike.Gameplay.Enemies;
using TopDownRoguelike.Networking.Gameplay;
using TopDownRoguelike.Networking.Protocol;
using UnityEngine;
using TopDownRoguelike.Gameplay.Characters;

namespace TopDownRoguelike.Gameplay.Networking
{
    public sealed class HostWorldSnapshotPublisher
        : MonoBehaviour
    {
        private const float SendIntervalSeconds =
            1f / 20f;

        private NetworkPlayerRegistry playerRegistry;
        private EnemySpawner enemySpawner;
        private BossEncounterController bossEncounterController;
        private Action<WorldStateSnapshotPayload> sendSnapshot;
        private float elapsedSeconds;

        public void ConfigureWorldSources(
            NetworkPlayerRegistry newPlayerRegistry,
            EnemySpawner newEnemySpawner,
            BossEncounterController
        newBossEncounterController)
        {
            playerRegistry =
                newPlayerRegistry ??
                throw new ArgumentNullException(
                    nameof(newPlayerRegistry));

            enemySpawner =
                newEnemySpawner ??
                throw new ArgumentNullException(
                    nameof(newEnemySpawner));

            bossEncounterController =
                newBossEncounterController ??
                throw new ArgumentNullException(
                    nameof(newBossEncounterController));

            enabled = true;
        }

        public void ConfigureSnapshotSender(
            Action<WorldStateSnapshotPayload> newSendSnapshot)
        {
            sendSnapshot =
                newSendSnapshot ??
                throw new ArgumentNullException(
                    nameof(newSendSnapshot));

            elapsedSeconds = 0f;
            enabled = true;
        }

        private void Update()
        {
            Advance(Time.deltaTime);
        }

        private void Advance(float deltaTime)
        {
            if (sendSnapshot == null ||
                playerRegistry == null ||
                enemySpawner == null ||
                bossEncounterController == null ||
                deltaTime <= 0f)
            {
                return;
            }

            elapsedSeconds += deltaTime;

            if (elapsedSeconds < SendIntervalSeconds)
            {
                return;
            }

            elapsedSeconds %= SendIntervalSeconds;

            sendSnapshot(
                BuildCurrentWorldSnapshot());
        }

        public WorldStateSnapshotPayload
            BuildCurrentWorldSnapshot()
        {
            var records =
                new List<WorldEntityRecord>();

            IReadOnlyDictionary<
                NetworkEntityId,
                Vector2> positions =
                CollectCurrentWorldEntityPositions();

            IReadOnlyDictionary<
                NetworkEntityId,
                float> rotations =
                CollectCurrentWorldEntityRotations();

            IReadOnlyDictionary<
                NetworkEntityId,
                bool> activity =
                CollectCurrentWorldEntityActivity();

            var enemyHealthById =
                new Dictionary<uint, EnemyHealthState>();

            foreach (EnemyHealthState state
                in CollectCurrentEnemyHealth())
            {
                enemyHealthById.Add(
                    state.EntityId,
                    state);
            }

            var bossHealthById =
                new Dictionary<uint, BossHealthState>();

            foreach (BossHealthState state
                in CollectCurrentBossHealth())
            {
                bossHealthById.Add(
                    state.EntityId,
                    state);
            }

            foreach (NetworkEntityId identifier
                in CollectCurrentWorldEntityIds())
            {
                if (!positions.TryGetValue(
                        identifier,
                        out Vector2 position) ||
                    !rotations.TryGetValue(
                        identifier,
                        out float rotation) ||
                    !activity.TryGetValue(
                        identifier,
                        out bool isActive))
                {
                    throw new InvalidOperationException(
                        "World entity transform data is incomplete.");
                }

                WorldEntityFlags flags =
                    isActive
                        ? WorldEntityFlags.Active
                        : WorldEntityFlags.None;

                ushort currentHealth = 0;
                ushort maxHealth = 0;
                byte bossPhase = 0;
                NetworkEnemyArchetype enemyArchetype =
                    NetworkEnemyArchetype.Invalid;

                if (identifier.EntityType ==
                    NetworkEntityType.Player)
                {
                    if (!TryGetPlayerHealthState(
                            identifier,
                            out PlayerHealthState state))
                    {
                        throw new InvalidOperationException(
                            "Player health data is incomplete.");
                    }

                    currentHealth = state.CurrentHealth;
                    maxHealth = state.MaxHealth;

                    if (state.IsDead)
                    {
                        flags |= WorldEntityFlags.Dead;
                    }
                }
                else if (identifier.EntityType ==
                    NetworkEntityType.Enemy &&
                    enemyHealthById.TryGetValue(
                        identifier.EntityId,
                        out EnemyHealthState enemyState))
                {
                    currentHealth = enemyState.CurrentHealth;
                    maxHealth = enemyState.MaxHealth;
                    enemyArchetype =
                        enemyState.NetworkArchetype;

                    if (enemyState.IsDead)
                    {
                        flags |= WorldEntityFlags.Dead;
                    }
                }
                else if (identifier.EntityType ==
                    NetworkEntityType.Boss &&
                    bossHealthById.TryGetValue(
                        identifier.EntityId,
                        out BossHealthState bossState))
                {
                    currentHealth = bossState.CurrentHealth;
                    maxHealth = bossState.MaxHealth;
                    bossPhase = bossState.Phase;

                    if (bossState.IsDead)
                    {
                        flags |= WorldEntityFlags.Dead;
                    }
                }

                records.Add(
                    new WorldEntityRecord(
                        identifier.EntityId,
                        identifier.EntityType,
                        WorldEntityLifecycle.Snapshot,
                        flags,
                        position.x,
                        position.y,
                        rotation,
                        currentHealth,
                        maxHealth,
                        bossPhase,
                        enemyArchetype));
            }

            return new WorldStateSnapshotPayload(records);
        }

        public IReadOnlyList<GameObject>
            CollectCurrentWorldEntities()
        {
            var entities =
                new List<GameObject>();

            foreach (KeyValuePair<uint, GameObject> entry
                in playerRegistry.EnumeratePlayers())
            {
                if (entry.Value != null)
                {
                    entities.Add(entry.Value);
                }
            }

            foreach (GameObject enemy
                in enemySpawner.EnumerateSpawnedEnemies())
            {
                if (enemy != null)
                {
                    entities.Add(enemy);
                }
            }

            if (bossEncounterController.CurrentBoss != null)
            {
                entities.Add(
                    bossEncounterController.CurrentBoss);
            }

            return entities;
        }

        public IReadOnlyList<NetworkEntityId>
            CollectCurrentWorldEntityIds()
        {
            var identifiers =
                new List<NetworkEntityId>();

            foreach (KeyValuePair<uint, GameObject> entry
                in playerRegistry.EnumeratePlayers())
            {
                AddIdentifier(
                    identifiers,
                    entry.Value,
                    NetworkEntityType.Player);
            }

            foreach (GameObject enemy
                in enemySpawner.EnumerateSpawnedEnemies())
            {
                AddIdentifier(
                    identifiers,
                    enemy,
                    NetworkEntityType.Enemy);
            }

            GameObject boss =
                bossEncounterController.CurrentBoss;

            if (boss != null)
            {
                AddIdentifier(
                    identifiers,
                    boss,
                    NetworkEntityType.Boss);
            }

            return identifiers;
        }

        public IReadOnlyDictionary<
            NetworkEntityId,
            Vector2>
            CollectCurrentWorldEntityPositions()
        {
            var positions =
                new Dictionary<
                    NetworkEntityId,
                    Vector2>();

            foreach (NetworkEntityId identifier
                in CollectCurrentWorldEntityIds())
            {
                if (identifier == null)
                {
                    continue;
                }

                Vector3 worldPosition =
                    identifier.transform.position;

                positions.Add(
                    identifier,
                    new Vector2(
                        worldPosition.x,
                        worldPosition.y));
            }

            return positions;
        }

        public IReadOnlyDictionary<
            NetworkEntityId,
            float>
            CollectCurrentWorldEntityRotations()
        {
            var rotations =
                new Dictionary<
                    NetworkEntityId,
                    float>();

            foreach (NetworkEntityId identifier
                in CollectCurrentWorldEntityIds())
            {
                if (identifier == null)
                {
                    continue;
                }

                rotations.Add(
                    identifier,
                    ResolveWorldEntityRotation(
                        identifier));
            }

            return rotations;
        }

        private static float ResolveWorldEntityRotation(
            NetworkEntityId identifier)
        {
            float transformRotation =
                identifier.transform
                    .eulerAngles
                    .z;

            if (identifier.EntityType !=
                    NetworkEntityType.Enemy ||
                !identifier.TryGetComponent(
                    out EnemyMovement movement))
            {
                return transformRotation;
            }

            Vector2 moveDirection =
                movement.MoveDirection;

            if (moveDirection.sqrMagnitude <
                0.0001f)
            {
                return transformRotation;
            }

            float directionDegrees =
                Mathf.Atan2(
                    moveDirection.y,
                    moveDirection.x) *
                Mathf.Rad2Deg;

            return Mathf.Repeat(
                directionDegrees,
                360f);
        }

        public IReadOnlyDictionary<
            NetworkEntityId,
            bool>
            CollectCurrentWorldEntityActivity()
        {
            var activity =
                new Dictionary<
                    NetworkEntityId,
                    bool>();

            foreach (NetworkEntityId identifier
                in CollectCurrentWorldEntityIds())
            {
                if (identifier == null)
                {
                    continue;
                }

                activity.Add(
                    identifier,
                    identifier.gameObject
                        .activeInHierarchy);
            }

            return activity;
        }

        public IReadOnlyList<PlayerHealthState>
            CollectCurrentPlayerHealth()
        {
            var healthStates =
                new List<PlayerHealthState>();

            foreach (KeyValuePair<uint, GameObject> entry
                in playerRegistry.EnumeratePlayers())
            {
                GameObject player =
                    entry.Value;

                if (player == null)
                {
                    continue;
                }

                if (!player.TryGetComponent(
                        out PlayerHealth playerHealth))
                {
                    throw new InvalidOperationException(
                        "Player is missing PlayerHealth.");
                }

                healthStates.Add(
                    new PlayerHealthState(
                        entry.Key,
                        playerHealth.CurrentHealth,
                        playerHealth.MaxHealth));
            }

            return healthStates;
        }

        public IReadOnlyList<EnemyHealthState>
            CollectCurrentEnemyHealth()
        {
            var states =
                new List<EnemyHealthState>();

            foreach (GameObject enemy
                in enemySpawner.EnumerateSpawnedEnemies())
            {
                if (enemy == null)
                {
                    continue;
                }

                if (!enemy.TryGetComponent(
                        out NetworkEntityId identifier) ||
                    !identifier.IsAssigned)
                {
                    throw new InvalidOperationException(
                        "Enemy is missing an assigned " +
                        "NetworkEntityId.");
                }

                if (identifier.EntityType !=
                    NetworkEntityType.Enemy)
                {
                    throw new InvalidOperationException(
                        "Enemy entity type must be Enemy.");
                }

                if (!enemy.TryGetComponent(
                        out EnemyHealth enemyHealth))
                {
                    throw new InvalidOperationException(
                        "Enemy is missing EnemyHealth.");
                }

                states.Add(
                    new EnemyHealthState(
                        identifier.EntityId,
                        enemyHealth.CurrentHealth,
                        enemyHealth.MaxHealth,
                        enemyHealth.IsDead,
                        enemyHealth.NetworkArchetype));
            }

            return states;
        }

        public IReadOnlyList<BossHealthState>
    CollectCurrentBossHealth()
        {
            var states =
                new List<BossHealthState>();

            GameObject boss =
                bossEncounterController.CurrentBoss;

            if (boss == null)
            {
                return states;
            }

            if (!boss.TryGetComponent(
                    out NetworkEntityId identifier) ||
                !identifier.IsAssigned)
            {
                throw new InvalidOperationException(
                    "Boss is missing an assigned " +
                    "NetworkEntityId.");
            }

            if (identifier.EntityType !=
                NetworkEntityType.Boss)
            {
                throw new InvalidOperationException(
                    "Boss entity type must be Boss.");
            }

            if (!boss.TryGetComponent(
                    out BossHealth bossHealth))
            {
                throw new InvalidOperationException(
                    "Boss is missing BossHealth.");
            }

            if (!boss.TryGetComponent(
                    out BossController bossController))
            {
                throw new InvalidOperationException(
                    "Boss is missing BossController.");
            }

            states.Add(
                new BossHealthState(
                    identifier.EntityId,
                    bossController.CurrentPhase,
                    bossHealth.CurrentHealth,
                    bossHealth.MaxHealth,
                    bossHealth.IsDead));

            return states;
        }

        private bool TryGetPlayerHealthState(
            NetworkEntityId identifier,
            out PlayerHealthState state)
        {
            foreach (KeyValuePair<uint, GameObject> entry
                in playerRegistry.EnumeratePlayers())
            {
                if (entry.Value != identifier.gameObject ||
                    !entry.Value.TryGetComponent(
                        out PlayerHealth playerHealth))
                {
                    continue;
                }

                state =
                    new PlayerHealthState(
                        entry.Key,
                        playerHealth.CurrentHealth,
                        playerHealth.MaxHealth);

                return true;
            }

            state = default;
            return false;
        }

        private void OnDisable()
        {
            elapsedSeconds = 0f;
        }

        private static void AddIdentifier(
            List<NetworkEntityId> identifiers,
            GameObject entity,
            NetworkEntityType expectedType)
        {
            if (entity == null)
            {
                return;
            }

            if (!entity.TryGetComponent(
                    out NetworkEntityId identifier) ||
                !identifier.IsAssigned)
            {
                throw new InvalidOperationException(
                    "World entity is missing an assigned " +
                    "NetworkEntityId.");
            }

            if (identifier.EntityType != expectedType)
            {
                throw new InvalidOperationException(
                    "World entity type mismatch: " +
                    $"expected {expectedType}, " +
                    $"but was {identifier.EntityType}.");
            }

            identifiers.Add(
                identifier);
        }
    }
}
