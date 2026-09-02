using System;
using TopDownRoguelike.Gameplay.Bosses;
using TopDownRoguelike.Infrastructure;
using TopDownRoguelike.Networking.Gameplay;
using TopDownRoguelike.Networking.Protocol;
using UnityEngine;

namespace TopDownRoguelike.Gameplay.Networking
{
    public sealed class HostBossSpawnPublisher : MonoBehaviour
    {
        private BossEncounterController encounter;
        private Action<WorldEntityRecord> sendSpawn;

        public void Configure(
            BossEncounterController newEncounter,
            Action<WorldEntityRecord> newSendSpawn)
        {
            if (newEncounter == null)
            {
                throw new ArgumentNullException(nameof(newEncounter));
            }

            if (newSendSpawn == null)
            {
                throw new ArgumentNullException(nameof(newSendSpawn));
            }

            Unsubscribe();
            encounter = newEncounter;
            sendSpawn = newSendSpawn;
            encounter.BossSpawned += HandleBossSpawned;
        }

        private void HandleBossSpawned(GameObject boss)
        {
            if (!GameSession.IsHost || sendSpawn == null)
            {
                return;
            }

            WorldEntityRecord record = CreateSpawnRecord(boss);
            Debug.Log(
                $"HostBossSpawnPublisher: sending Boss spawn " +
                $"entity={record.EntityId} position=({record.PositionX:F2}," +
                $"{record.PositionY:F2})");
            sendSpawn(record);
        }

        private static WorldEntityRecord CreateSpawnRecord(GameObject boss)
        {
            if (boss == null)
            {
                throw new ArgumentNullException(nameof(boss));
            }

            if (!boss.TryGetComponent(
                    out NetworkEntityId identifier) ||
                !identifier.IsAssigned ||
                identifier.EntityType != NetworkEntityType.Boss)
            {
                throw new InvalidOperationException(
                    "Spawned Boss must have an assigned Boss NetworkEntityId.");
            }

            if (!boss.TryGetComponent(out BossHealth health) ||
                health.IsDead ||
                health.CurrentHealth < 1 ||
                health.CurrentHealth > ushort.MaxValue ||
                health.MaxHealth < 1 ||
                health.MaxHealth > ushort.MaxValue)
            {
                throw new InvalidOperationException(
                    "Spawned Boss health is outside the network range.");
            }

            byte phase = 1;
            if (boss.TryGetComponent(
                    out BossController controller))
            {
                phase = controller.CurrentPhase;
            }

            Vector3 position = boss.transform.position;
            return new WorldEntityRecord(
                identifier.EntityId,
                NetworkEntityType.Boss,
                WorldEntityLifecycle.Spawn,
                WorldEntityFlags.Active,
                position.x,
                position.y,
                boss.transform.eulerAngles.z,
                (ushort)health.CurrentHealth,
                (ushort)health.MaxHealth,
                phase);
        }

        private void Unsubscribe()
        {
            if (encounter != null)
            {
                encounter.BossSpawned -= HandleBossSpawned;
            }

            encounter = null;
            sendSpawn = null;
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }
    }
}
