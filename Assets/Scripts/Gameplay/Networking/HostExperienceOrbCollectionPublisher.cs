using System;
using TopDownRoguelike.Gameplay.Experience;
using TopDownRoguelike.Infrastructure;
using TopDownRoguelike.Networking.Gameplay;
using TopDownRoguelike.Networking.Protocol;
using UnityEngine;

namespace TopDownRoguelike.Gameplay.Networking
{
    public sealed class HostExperienceOrbCollectionPublisher : MonoBehaviour
    {
        private ExperienceOrbPool orbPool;
        private Action<WorldEntityRemovedPayload> sendRemoval;

        public void Configure(
            ExperienceOrbPool newOrbPool,
            Action<WorldEntityRemovedPayload> newSendRemoval)
        {
            if (newOrbPool == null)
            {
                throw new ArgumentNullException(nameof(newOrbPool));
            }

            if (newSendRemoval == null)
            {
                throw new ArgumentNullException(nameof(newSendRemoval));
            }

            Unsubscribe();
            orbPool = newOrbPool;
            sendRemoval = newSendRemoval;
            orbPool.OrbCollected += HandleOrbCollected;
        }

        private void HandleOrbCollected(ExperienceOrb orb)
        {
            if (!GameSession.IsHost || sendRemoval == null || orb == null ||
                !orb.TryGetComponent(out NetworkEntityId identifier) ||
                !identifier.IsAssigned ||
                identifier.EntityType != NetworkEntityType.ExperienceOrb)
            {
                return;
            }

            sendRemoval(new WorldEntityRemovedPayload(
                identifier.EntityId,
                NetworkEntityType.ExperienceOrb,
                WorldEntityRemovalReason.Despawned));
        }

        private void Unsubscribe()
        {
            if (orbPool != null)
            {
                orbPool.OrbCollected -= HandleOrbCollected;
            }

            orbPool = null;
            sendRemoval = null;
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }
    }
}
