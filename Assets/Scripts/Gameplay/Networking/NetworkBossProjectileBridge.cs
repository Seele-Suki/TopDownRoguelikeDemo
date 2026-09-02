using System;
using System.Collections.Generic;
using TopDownRoguelike.Gameplay.Bosses;
using TopDownRoguelike.Infrastructure;
using TopDownRoguelike.Networking.Gameplay;
using TopDownRoguelike.Networking.Protocol;
using UnityEngine;

namespace TopDownRoguelike.Gameplay.Networking
{
    public sealed class NetworkBossProjectileBridge : MonoBehaviour
    {
        [SerializeField] private BossProjectile projectilePrefab;
        private Action<WorldEntityRecord> sendSpawn;
        private Action<WorldEntityRemovedPayload> sendRemoval;
        private readonly Dictionary<BossProjectile, uint> tracked =
            new Dictionary<BossProjectile, uint>();
        private readonly Dictionary<BossProjectile, uint> sequences =
            new Dictionary<BossProjectile, uint>();
        private uint nextEntityId = 0x30000001u;
        private uint nextSequence = 1u;

        public void Configure(
            BossProjectile newProjectilePrefab,
            Action<WorldEntityRecord> newSendSpawn,
            Action<WorldEntityRemovedPayload> newSendRemoval)
        {
            projectilePrefab = newProjectilePrefab;
            sendSpawn = newSendSpawn ?? throw new ArgumentNullException(nameof(newSendSpawn));
            sendRemoval = newSendRemoval ?? throw new ArgumentNullException(nameof(newSendRemoval));
        }

        public void SetProjectilePrefab(BossProjectile newProjectilePrefab)
        {
            projectilePrefab = newProjectilePrefab;
        }

        public BossProjectile CreateClientProjectile(WorldEntityRecord record)
        {
            if (!GameSession.IsClient || projectilePrefab == null ||
                record == null || record.EntityType != NetworkEntityType.BossProjectile)
                return null;

            BossProjectile projectile = Instantiate(
                projectilePrefab,
                new Vector3(record.PositionX, record.PositionY, 0f),
                Quaternion.Euler(0f, 0f, record.RotationDegrees));
            projectile.enabled = false;
            return projectile;
        }

        private void Update()
        {
            if (!GameSession.IsHost || sendSpawn == null)
                return;

            BossEncounterController encounter =
                FindObjectOfType<BossEncounterController>();
            if (encounter != null && encounter.CurrentBoss == null &&
                tracked.Count > 0)
            {
                ClearTrackedProjectiles(
                    WorldEntityRemovalReason.Cleared);
                return;
            }

            foreach (BossProjectile projectile in FindObjectsOfType<BossProjectile>())
            {
                if (projectile == null || !projectile.IsInitialized || tracked.ContainsKey(projectile))
                    continue;

                uint entityId = nextEntityId++;
                NetworkEntityId identifier =
                    projectile.GetComponent<NetworkEntityId>();
                if (identifier == null)
                    identifier = projectile.gameObject.AddComponent<NetworkEntityId>();
                if (!identifier.IsAssigned &&
                    !identifier.TryAssign(entityId, NetworkEntityType.BossProjectile))
                    continue;
                if (identifier.EntityType != NetworkEntityType.BossProjectile)
                    continue;
                tracked.Add(projectile, entityId);
                uint sequence = nextSequence++;
                sequences.Add(projectile, sequence);
                sendSpawn(new WorldEntityRecord(
                    entityId,
                    NetworkEntityType.BossProjectile,
                    WorldEntityLifecycle.Spawn,
                    WorldEntityFlags.Active,
                    projectile.transform.position.x,
                    projectile.transform.position.y,
                    projectile.transform.eulerAngles.z,
                    0,
                    0,
                    0,
                    NetworkEnemyArchetype.Invalid,
                    0,
                    projectile.MoveDirection.x,
                    projectile.MoveDirection.y,
                    projectile.Speed,
                    (ushort)Mathf.Clamp(projectile.Damage, 1, ushort.MaxValue),
                    sequence));
            }

            var removed = new List<BossProjectile>();
            foreach (KeyValuePair<BossProjectile, uint> entry in tracked)
            {
                if (entry.Key == null)
                {
                    sendRemoval(new WorldEntityRemovedPayload(
                        entry.Value,
                        NetworkEntityType.BossProjectile,
                        WorldEntityRemovalReason.Despawned));
                    removed.Add(entry.Key);
                }
            }

            foreach (BossProjectile projectile in removed)
            {
                tracked.Remove(projectile);
                sequences.Remove(projectile);
            }
        }

        public bool TryGetNetworkState(
            BossProjectile projectile,
            out uint entityId,
            out uint sequence)
        {
            entityId = 0u;
            sequence = 0u;
            return projectile != null &&
                tracked.TryGetValue(projectile, out entityId) &&
                sequences.TryGetValue(projectile, out sequence);
        }

        public void ClearTrackedProjectiles(
            WorldEntityRemovalReason reason)
        {
            if (!GameSession.IsHost)
                return;

            var entries = new List<KeyValuePair<BossProjectile, uint>>(tracked);
            tracked.Clear();
            sequences.Clear();
            foreach (KeyValuePair<BossProjectile, uint> entry in entries)
            {
                if (sendRemoval != null)
                {
                    sendRemoval(new WorldEntityRemovedPayload(
                        entry.Value,
                        NetworkEntityType.BossProjectile,
                        reason));
                }

                if (entry.Key != null)
                    Destroy(entry.Key.gameObject);
            }
        }

        private void OnDisable()
        {
            tracked.Clear();
            sequences.Clear();
        }
    }
}
