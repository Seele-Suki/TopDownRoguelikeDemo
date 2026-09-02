using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using TopDownRoguelike.Gameplay.Experience;
using TopDownRoguelike.Gameplay.Enemies;
using TopDownRoguelike.Gameplay.Bosses;
using TopDownRoguelike.Networking.Gameplay;
using TopDownRoguelike.Networking.Protocol;
using UnityEngine;

namespace TopDownRoguelike.Gameplay.Networking
{
    public sealed class ClientWorldSnapshotConsumer
        : MonoBehaviour
    {
        private NetworkEntityRegistry entityRegistry;

        private Func<WorldEntityRecord, GameObject>
            entityFactory;

        private Action<GameObject>
            entityRemover;

        private uint authoritativeHostPlayerId;

        private WorldEntityRecord
            authorizedCreationRecord;

        private readonly
            ConcurrentQueue<PendingSnapshot>
            pendingSnapshots =
                new ConcurrentQueue<PendingSnapshot>();

        private readonly
            ConcurrentQueue<WorldEntityRecord>
            pendingSpawns =
                new ConcurrentQueue<WorldEntityRecord>();

        private readonly
            ConcurrentQueue<WorldEntityRemovedPayload>
            pendingRemovals =
                new ConcurrentQueue<WorldEntityRemovedPayload>();

        private readonly HashSet<uint> removedEntityIds =
            new HashSet<uint>();

        private int unityMainThreadId;

        private readonly UdpSequenceTracker
            snapshotSequenceTracker =
            new UdpSequenceTracker();

        private void Awake()
        {
            CaptureOrValidateUnityMainThread();
        }

        private void Update()
        {
            ProcessPendingSnapshots();
        }

        private void OnDestroy()
        {
            while (pendingSpawns.TryDequeue(
                out _))
            {
            }

            while (pendingSnapshots.TryDequeue(
                out _))
            {
            }

            while (pendingRemovals.TryDequeue(out _))
            {
            }
        }

        public void ConfigureAuthoritativeHost(
            uint hostPlayerId)
        {
            CaptureOrValidateUnityMainThread();

            if (hostPlayerId == 0u)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(hostPlayerId));
            }

            authoritativeHostPlayerId =
                hostPlayerId;
        }

        public void ConfigureEntityRegistry(
            NetworkEntityRegistry newEntityRegistry)
        {
            CaptureOrValidateUnityMainThread();

            entityRegistry =
                newEntityRegistry ??
                throw new ArgumentNullException(
                    nameof(newEntityRegistry));
        }

        public void ConfigureEntityFactory(
            Func<WorldEntityRecord, GameObject>
            newEntityFactory)
        {
            CaptureOrValidateUnityMainThread();

            entityFactory =
                newEntityFactory ??
                throw new ArgumentNullException(
                    nameof(newEntityFactory));
        }

        public void ConfigureEntityRemover(
            Action<GameObject> newEntityRemover)
        {
            CaptureOrValidateUnityMainThread();

            entityRemover =
                newEntityRemover ??
                throw new ArgumentNullException(
                    nameof(newEntityRemover));
        }

        public bool ValidateSnapshotSequence(
            uint sequence)
        {
            return snapshotSequenceTracker.Accept(
                sequence);
        }

        public bool EnqueueSnapshot(
            uint senderPlayerId,
            uint sequence,
            WorldStateSnapshotPayload snapshot)
        {
            if (senderPlayerId == 0u ||
                authoritativeHostPlayerId == 0u ||
                senderPlayerId !=
                authoritativeHostPlayerId ||
                snapshot == null)
            {
                return false;
            }

            pendingSnapshots.Enqueue(
                new PendingSnapshot(
                    senderPlayerId,
                    sequence,
                    snapshot));

            return true;
        }

        public bool EnqueueSpawn(
            WorldEntityRecord record)
        {
            if (record == null ||
                record.EntityId == 0u ||
                (record.EntityType != NetworkEntityType.Enemy &&
                 record.EntityType != NetworkEntityType.Boss &&
                record.EntityType != NetworkEntityType.ExperienceOrb &&
                record.EntityType != NetworkEntityType.BossProjectile) ||
                record.Lifecycle !=
                    WorldEntityLifecycle.Spawn ||
                (record.Flags &
                    WorldEntityFlags.Dead) != 0)
            {
                return false;
            }

            pendingSpawns.Enqueue(record);
            return true;
        }

        public bool EnqueueRemoval(
            WorldEntityRemovedPayload removed)
        {
            if (removed == null ||
                removed.EntityId == 0u ||
                !IsSupportedEntityType(removed.EntityType) ||
                (removed.EntityType == NetworkEntityType.Enemy &&
                 removed.Reason != WorldEntityRemovalReason.Died) ||
                (removed.EntityType == NetworkEntityType.Boss &&
                 removed.Reason != WorldEntityRemovalReason.Died) ||
                (removed.EntityType == NetworkEntityType.ExperienceOrb &&
                 removed.Reason != WorldEntityRemovalReason.Despawned))
            {
                return false;
            }

            pendingRemovals.Enqueue(removed);
            return true;
        }

        public int ProcessPendingSnapshots()
        {
            if (!IsUnityMainThread())
            {
                return 0;
            }

            int processedCount =
                0;

            while (pendingSpawns.TryDequeue(
                out WorldEntityRecord spawnRecord))
            {
                if (TryConsumeSpawn(spawnRecord))
                {
                    processedCount++;
                }
            }

            PendingSnapshot latestPendingSnapshot = null;
            while (pendingSnapshots.TryDequeue(
                out PendingSnapshot pending))
            {
                latestPendingSnapshot = pending;
            }

            if (latestPendingSnapshot != null &&
                TryConsumeSnapshot(
                    latestPendingSnapshot.SenderPlayerId,
                    latestPendingSnapshot.Sequence,
                    latestPendingSnapshot.Snapshot))
            {
                processedCount++;
            }

            while (pendingRemovals.TryDequeue(
                out WorldEntityRemovedPayload removed))
            {
                if (TryRemoveEntity(removed))
                {
                    processedCount++;
                }
            }

            return processedCount;
        }

        private bool TryConsumeSpawn(
            WorldEntityRecord record)
        {
            if (!IsUnityMainThread() ||
                entityRegistry == null ||
                record == null ||
                !IsSupportedEntityType(record.EntityType) ||
                record.Lifecycle !=
                    WorldEntityLifecycle.Spawn)
            {
                return false;
            }

            if (TryFindEntityObject(
                    record.EntityId,
                    out _))
            {
                return TryUpdateExistingEntity(
                    record);
            }

            if (entityFactory == null)
            {
                return false;
            }

            bool wasCreated;

            authorizedCreationRecord =
                record;

            try
            {
                wasCreated =
                    TryCreateMissingEntity(
                        record,
                        out _);
            }
            finally
            {
                authorizedCreationRecord =
                    null;
            }

            return wasCreated &&
                TryUpdateExistingEntity(record);
        }

        public bool TryConsumeSnapshot(
            uint senderPlayerId,
            uint sequence,
            WorldStateSnapshotPayload snapshot)
        {
            if (!IsUnityMainThread() ||
                senderPlayerId == 0u ||
                authoritativeHostPlayerId == 0u ||
                senderPlayerId !=
                authoritativeHostPlayerId ||
                snapshot == null)
            {
                return false;
            }

            if (!ValidateSnapshotSequence(
                    sequence))
            {
                return false;
            }

            if (entityRegistry == null)
            {
                return true;
            }

            var seenExperienceOrbIds = new HashSet<uint>();
            var seenEnemyIds = new HashSet<uint>();
            var seenBossProjectileIds = new HashSet<uint>();

            foreach (WorldEntityRecord record
                in snapshot.Entities)
            {
                if (record == null)
                {
                    continue;
                }

                if (record.EntityType == NetworkEntityType.ExperienceOrb &&
                    record.Lifecycle != WorldEntityLifecycle.Removed)
                {
                    seenExperienceOrbIds.Add(record.EntityId);
                }

                if (record.EntityType == NetworkEntityType.Enemy &&
                    record.Lifecycle != WorldEntityLifecycle.Removed)
                {
                    seenEnemyIds.Add(record.EntityId);
                }

                if (record.EntityType == NetworkEntityType.BossProjectile &&
                    record.Lifecycle != WorldEntityLifecycle.Removed)
                {
                    seenBossProjectileIds.Add(record.EntityId);
                }

                if (record.Lifecycle ==
                    WorldEntityLifecycle.Removed)
                {
                    if (!TryRemoveEntity(record))
                    {
                        return false;
                    }

                    continue;
                }

                if (TryFindEntityObject(
                        record.EntityId,
                        out _))
                {
                    if (!TryUpdateExistingEntity(
                            record))
                    {
                        return false;
                    }

                    continue;
                }

                if (entityFactory == null)
                {
                    continue;
                }

                bool wasCreated;

                authorizedCreationRecord =
                    record;

                try
                {
                    wasCreated =
                        TryCreateMissingEntity(
                            record,
                            out _);
                }
                finally
                {
                    authorizedCreationRecord =
                        null;
                }

                if (!wasCreated ||
                    !TryUpdateExistingEntity(record))
                {
                    return false;
                }
            }

            var staleExperienceOrbIds = new List<uint>();
            var staleEnemyIds = new List<uint>();
            var staleBossProjectileIds = new List<uint>();
            foreach (NetworkEntityId identifier in
                entityRegistry.EnumerateEntities())
            {
                if (identifier != null &&
                    identifier.EntityType == NetworkEntityType.ExperienceOrb &&
                    !seenExperienceOrbIds.Contains(identifier.EntityId))
                {
                    staleExperienceOrbIds.Add(identifier.EntityId);
                }

                if (identifier != null &&
                    identifier.EntityType == NetworkEntityType.Enemy &&
                    !seenEnemyIds.Contains(identifier.EntityId))
                {
                    staleEnemyIds.Add(identifier.EntityId);
                }

                if (identifier != null &&
                    identifier.EntityType == NetworkEntityType.BossProjectile &&
                    !seenBossProjectileIds.Contains(identifier.EntityId))
                {
                    staleBossProjectileIds.Add(identifier.EntityId);
                }
            }

            foreach (uint staleId in staleExperienceOrbIds)
            {
                RemoveRegisteredEntity(
                    staleId,
                    NetworkEntityType.ExperienceOrb);
            }

            foreach (uint staleId in staleEnemyIds)
            {
                RemoveRegisteredEntity(
                    staleId,
                    NetworkEntityType.Enemy);
            }

            foreach (uint staleId in staleBossProjectileIds)
            {
                RemoveRegisteredEntity(
                    staleId,
                    NetworkEntityType.BossProjectile);
            }

            return true;
        }

        public bool TryRemoveEntity(
            WorldEntityRecord record)
        {
            if (!IsUnityMainThread() ||
                entityRegistry == null ||
                record == null ||
                record.EntityId == 0u ||
                record.Lifecycle !=
                WorldEntityLifecycle.Removed ||
                !IsSupportedEntityType(
                    record.EntityType))
            {
                return false;
            }

            return RemoveRegisteredEntity(
                record.EntityId,
                record.EntityType);
        }

        public bool TryRemoveEntity(
            WorldEntityRemovedPayload removed)
        {
            if (!IsUnityMainThread() ||
                entityRegistry == null ||
                removed == null ||
                removed.EntityId == 0u ||
                !IsSupportedEntityType(removed.EntityType) ||
                (removed.EntityType == NetworkEntityType.Enemy &&
                 removed.Reason != WorldEntityRemovalReason.Died) ||
                (removed.EntityType == NetworkEntityType.Boss &&
                 removed.Reason != WorldEntityRemovalReason.Died) ||
                (removed.EntityType == NetworkEntityType.ExperienceOrb &&
                 removed.Reason != WorldEntityRemovalReason.Despawned) ||
                (removed.EntityType == NetworkEntityType.BossProjectile &&
                 removed.Reason == WorldEntityRemovalReason.Invalid))
            {
                return false;
            }

            return RemoveRegisteredEntity(
                removed.EntityId,
                removed.EntityType);
        }

        public bool TryUpdateExistingEntity(
            WorldEntityRecord record)
        {
            if (!IsUnityMainThread() ||
                entityRegistry == null ||
                record == null ||
                record.EntityId == 0u ||
                record.Lifecycle ==
                WorldEntityLifecycle.Removed ||
                !entityRegistry.TryGet(
                    record.EntityId,
                    out NetworkEntityId identifier) ||
                identifier == null ||
                identifier.EntityType !=
                record.EntityType)
            {
                return false;
            }

            GameObject entityObject =
                identifier.gameObject;

            if (entityObject == null)
            {
                return false;
            }

            Vector3 position =
                entityObject.transform.position;

            position.x =
                record.PositionX;

            position.y =
                record.PositionY;

            entityObject.transform.SetPositionAndRotation(
                position,
                Quaternion.Euler(
                    0f,
                    0f,
                    record.RotationDegrees));

            if (record.EntityType ==
                    NetworkEntityType.Enemy &&
                entityObject.TryGetComponent(
                    out EnemyHealth enemyHealth) &&
                !enemyHealth.ApplyAuthoritativeState(
                    record.CurrentHealth,
                    record.MaxHealth,
                    record.IsDead))
            {
                return false;
            }

            if (record.EntityType == NetworkEntityType.Enemy &&
                record.IsDead)
            {
                return RemoveRegisteredEntity(
                    record.EntityId,
                    record.EntityType);
            }

            if (record.EntityType == NetworkEntityType.Boss &&
                entityObject.TryGetComponent(
                    out BossHealth bossHealth) &&
                !bossHealth.ApplyAuthoritativeState(
                    record.CurrentHealth,
                    record.MaxHealth,
                    record.IsDead))
            {
                return false;
            }

            if (record.EntityType == NetworkEntityType.Boss &&
                entityObject.TryGetComponent(
                    out BossController bossController) &&
                !bossController.ApplyAuthoritativePhase(
                    record.BossPhase))
            {
                return false;
            }

            if (record.EntityType == NetworkEntityType.Boss &&
                record.IsDead)
            {
                return RemoveRegisteredEntity(
                    record.EntityId,
                    record.EntityType);
            }

            if (record.EntityType ==
                NetworkEntityType.ExperienceOrb &&
                entityObject.TryGetComponent(
                    out ExperienceOrb orb))
            {
                orb.Initialize(record.ExperienceAmount);
            }

            bool shouldBeActive =
                (record.Flags &
                 WorldEntityFlags.Active) != 0;

            if (entityObject.activeSelf !=
                shouldBeActive)
            {
                entityObject.SetActive(
                    shouldBeActive);
            }

            return true;
        }

        public bool TryCreateMissingEntity(
            WorldEntityRecord record,
            out GameObject entityObject)
        {
            entityObject =
                null;

            if (!IsUnityMainThread() ||
                entityRegistry == null ||
                entityFactory == null ||
                record == null ||
                !object.ReferenceEquals(
                    record,
                    authorizedCreationRecord) ||
                record.EntityId == 0u ||
                removedEntityIds.Contains(record.EntityId) ||
                !IsSupportedEntityType(
                    record.EntityType) ||
                entityRegistry.TryGet(
                    record.EntityId,
                    out _))
            {
                return false;
            }

            authorizedCreationRecord =
                null;

            GameObject createdObject =
                entityFactory(record);

            if (createdObject == null)
            {
                return false;
            }

            NetworkEntityId identifier =
                createdObject.GetComponent<NetworkEntityId>();

            if (identifier == null)
            {
                identifier =
                    createdObject.AddComponent<NetworkEntityId>();
            }

            if (identifier.IsAssigned)
            {
                if (identifier.EntityId !=
                        record.EntityId ||
                    identifier.EntityType !=
                        record.EntityType)
                {
                    return false;
                }
            }
            else if (!identifier.TryAssign(
                         record.EntityId,
                         record.EntityType))
            {
                return false;
            }

            if (!entityRegistry.TryRegister(
                    identifier))
            {
                return false;
            }

            entityObject =
                createdObject;

            return true;
        }

        public bool TryFindEntityObject(
            uint entityId,
            out GameObject entityObject)
        {
            entityObject =
                null;

            if (!IsUnityMainThread() ||
                entityRegistry == null ||
                entityId == 0u ||
                !entityRegistry.TryGet(
                    entityId,
                    out NetworkEntityId entity) ||
                entity == null)
            {
                return false;
            }

            entityObject =
                entity.gameObject;

            return entityObject != null;
        }

        private bool RemoveRegisteredEntity(
            uint entityId,
            NetworkEntityType entityType)
        {
            if (removedEntityIds.Contains(entityId))
            {
                return true;
            }

            if (!entityRegistry.TryGet(
                    entityId,
                    out NetworkEntityId identifier))
            {
                removedEntityIds.Add(entityId);
                return true;
            }

            if (identifier == null)
            {
                removedEntityIds.Add(entityId);
                return entityRegistry.Remove(entityId);
            }

            if (identifier.EntityType != entityType)
            {
                return false;
            }

            GameObject entityObject = identifier.gameObject;

            if (!entityRegistry.Remove(entityId))
            {
                return false;
            }

            removedEntityIds.Add(entityId);
            identifier.Clear();

            if (entityObject == null)
            {
                return true;
            }

            if (entityRemover != null)
            {
                entityRemover(entityObject);
            }
            else
            {
                entityObject.SetActive(false);
            }

            return true;
        }

        private bool IsUnityMainThread()
        {
            return unityMainThreadId != 0 &&
                Thread.CurrentThread.ManagedThreadId ==
                unityMainThreadId;
        }

        private void CaptureOrValidateUnityMainThread()
        {
            int currentThreadId =
                Thread.CurrentThread.ManagedThreadId;

            if (unityMainThreadId == 0)
            {
                unityMainThreadId =
                    currentThreadId;

                return;
            }

            if (unityMainThreadId !=
                currentThreadId)
            {
                throw new InvalidOperationException(
                    "Client world snapshot configuration " +
                    "must run on the Unity main thread.");
            }
        }

        private static bool IsSupportedEntityType(
            NetworkEntityType entityType)
        {
            int rawValue =
                (int)entityType;

            return rawValue >=
                    (int)NetworkEntityType.Player &&
                rawValue <=
                    (int)NetworkEntityType.BossProjectile;
        }

        private sealed class PendingSnapshot
        {
            public PendingSnapshot(
                uint senderPlayerId,
                uint sequence,
                WorldStateSnapshotPayload snapshot)
            {
                SenderPlayerId =
                    senderPlayerId;

                Sequence =
                    sequence;

                Snapshot =
                    snapshot;
            }

            public uint SenderPlayerId { get; }

            public uint Sequence { get; }

            public WorldStateSnapshotPayload Snapshot { get; }
        }
    }
}
