using System;
using TopDownRoguelike.Gameplay.Experience;
using TopDownRoguelike.Infrastructure;
using TopDownRoguelike.Networking.Gameplay;
using TopDownRoguelike.Networking.Protocol;
using UnityEngine;

namespace TopDownRoguelike.Gameplay.Networking
{
    public sealed class HostExperienceOrbSpawnPublisher : MonoBehaviour
    {
        private ExperienceOrbPool orbPool;
        private Action<WorldEntityRecord> sendSpawn;

        public void Configure(
            ExperienceOrbPool newOrbPool,
            Action<WorldEntityRecord> newSendSpawn)
        {
            if (newOrbPool == null)
            {
                throw new ArgumentNullException(nameof(newOrbPool));
            }

            if (newSendSpawn == null)
            {
                throw new ArgumentNullException(nameof(newSendSpawn));
            }

            Unsubscribe();
            orbPool = newOrbPool;
            sendSpawn = newSendSpawn;
            orbPool.OrbSpawned += HandleOrbSpawned;
        }

        private void HandleOrbSpawned(ExperienceOrb orb)
        {
            if (!GameSession.IsHost || sendSpawn == null || orb == null)
            {
                return;
            }

            if (!orb.TryGetComponent(
                    out NetworkEntityId identifier) ||
                !identifier.IsAssigned ||
                identifier.EntityType != NetworkEntityType.ExperienceOrb)
            {
                throw new InvalidOperationException(
                    "Spawned experience orb must have an assigned " +
                    "ExperienceOrb NetworkEntityId.");
            }

            Vector3 position = orb.transform.position;
            sendSpawn(new WorldEntityRecord(
                identifier.EntityId,
                NetworkEntityType.ExperienceOrb,
                WorldEntityLifecycle.Spawn,
                WorldEntityFlags.Active,
                position.x,
                position.y,
                orb.transform.eulerAngles.z,
                0,
                0,
                experienceAmount: checked((ushort)orb.ExperienceAmount)));
        }

        private void Unsubscribe()
        {
            if (orbPool != null)
            {
                orbPool.OrbSpawned -= HandleOrbSpawned;
            }

            orbPool = null;
            sendSpawn = null;
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }
    }
}
