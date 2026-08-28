using System;
using System.Collections.Generic;
using TopDownRoguelike.Networking.Protocol;
using UnityEngine;

namespace TopDownRoguelike.Gameplay.Networking
{
    public sealed class RemotePlayerShotEventReceiver
        : MonoBehaviour
    {
        private readonly Queue<PlayerShotEvent>
            pendingShots =
                new Queue<PlayerShotEvent>();

        private uint remotePlayerId;

        public int PendingCount =>
            pendingShots.Count;

        public void Configure(
            uint newRemotePlayerId)
        {
            if (newRemotePlayerId == 0u)
            {
                throw new ArgumentException(
                    "Remote player ID must be non-zero.",
                    nameof(newRemotePlayerId));
            }

            remotePlayerId =
                newRemotePlayerId;

            pendingShots.Clear();
        }

        public void Enqueue(
            uint senderPlayerId,
            PlayerShotEvent shotEvent)
        {
            if (remotePlayerId == 0u)
            {
                throw new InvalidOperationException(
                    "Remote shot receiver is not configured.");
            }

            if (senderPlayerId == 0u ||
                senderPlayerId != remotePlayerId)
            {
                throw new ArgumentException(
                    "Shot sender does not match " +
                    "the configured remote player.",
                    nameof(senderPlayerId));
            }

            if (shotEvent == null)
            {
                throw new ArgumentNullException(
                    nameof(shotEvent));
            }

            if (shotEvent.PlayerId !=
                remotePlayerId)
            {
                throw new ArgumentException(
                    "Shot event player ID does not match " +
                    "the configured remote player.",
                    nameof(shotEvent));
            }

            pendingShots.Enqueue(
                shotEvent);
        }

        public bool TryDequeue(
            out PlayerShotEvent shotEvent)
        {
            if (pendingShots.Count == 0)
            {
                shotEvent =
                    null;

                return false;
            }

            shotEvent =
                pendingShots.Dequeue();

            return true;
        }

        public void Clear()
        {
            pendingShots.Clear();
        }

        private void OnDisable()
        {
            Clear();
        }

        private void OnDestroy()
        {
            Clear();
        }
    }
}