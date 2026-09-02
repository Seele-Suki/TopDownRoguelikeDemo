using System;
using TopDownRoguelike.Gameplay.Bosses;
using TopDownRoguelike.Infrastructure;
using TopDownRoguelike.Networking.Gameplay;
using TopDownRoguelike.Networking.Protocol;
using UnityEngine;

namespace TopDownRoguelike.Gameplay.Networking
{
    public sealed class HostBossDeathPublisher : MonoBehaviour
    {
        private BossEncounterController encounter;
        private BossHealth trackedHealth;
        private NetworkEntityId trackedIdentifier;
        private Action<WorldEntityRemovedPayload> sendRemoval;

        public void Configure(
            BossEncounterController newEncounter,
            Action<WorldEntityRemovedPayload> newSendRemoval)
        {
            if (newEncounter == null)
            {
                throw new ArgumentNullException(nameof(newEncounter));
            }

            if (newSendRemoval == null)
            {
                throw new ArgumentNullException(nameof(newSendRemoval));
            }

            Unsubscribe();
            encounter = newEncounter;
            sendRemoval = newSendRemoval;
            encounter.BossSpawned += HandleBossSpawned;

            if (encounter.CurrentBoss != null)
            {
                SubscribeBoss(encounter.CurrentBoss);
            }
        }

        private void HandleBossSpawned(GameObject boss)
        {
            SubscribeBoss(boss);
        }

        private void SubscribeBoss(GameObject boss)
        {
            if (boss == null ||
                !boss.TryGetComponent(out BossHealth health) ||
                !boss.TryGetComponent(out NetworkEntityId identifier) ||
                !identifier.IsAssigned ||
                identifier.EntityType != NetworkEntityType.Boss)
            {
                throw new InvalidOperationException(
                    "Tracked Boss must have BossHealth and an assigned Boss NetworkEntityId.");
            }

            UnsubscribeBoss();
            trackedHealth = health;
            trackedIdentifier = identifier;
            trackedHealth.OnDied += HandleBossDied;
        }

        private void HandleBossDied()
        {
            Debug.Log(
                $"HostBossDeathPublisher: Boss died callback " +
                $"entity={(trackedIdentifier != null ? trackedIdentifier.EntityId : 0u)}");

            if (GameSession.IsHost &&
                sendRemoval != null &&
                trackedIdentifier != null &&
                trackedIdentifier.IsAssigned)
            {
                Debug.Log(
                    $"HostBossDeathPublisher: sending Boss removal " +
                    $"entity={trackedIdentifier.EntityId}");
                sendRemoval(
                    new WorldEntityRemovedPayload(
                        trackedIdentifier.EntityId,
                        NetworkEntityType.Boss,
                        WorldEntityRemovalReason.Died));
            }
            else
            {
                Debug.LogError(
                    "HostBossDeathPublisher: cannot send Boss removal " +
                    "because host state, callback, or entity ID is invalid.");
            }

            UnsubscribeBoss();
        }

        private void UnsubscribeBoss()
        {
            if (trackedHealth != null)
            {
                trackedHealth.OnDied -= HandleBossDied;
            }

            trackedHealth = null;
            trackedIdentifier = null;
        }

        private void Unsubscribe()
        {
            if (encounter != null)
            {
                encounter.BossSpawned -= HandleBossSpawned;
            }

            UnsubscribeBoss();
            encounter = null;
            sendRemoval = null;
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }
    }
}
