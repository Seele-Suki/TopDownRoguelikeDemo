using System;
using System.Collections.Generic;
using TopDownRoguelike.Networking.Protocol;
using UnityEngine;

namespace TopDownRoguelike.Gameplay.Networking
{
    public sealed class RemotePlayerShotgunEventReceiver
        : MonoBehaviour
    {
        private readonly Queue<PlayerShotgunEvent>
            pendingShotgunEvents =
                new Queue<PlayerShotgunEvent>();

        private uint remotePlayerId;
        private uint lastVolleySequence;
        private bool hasReceivedSequence;

        public int PendingCount =>
            pendingShotgunEvents.Count;

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

            lastVolleySequence =
                0u;

            hasReceivedSequence =
                false;

            pendingShotgunEvents.Clear();
        }

        public void Enqueue(
            uint senderPlayerId,
            PlayerShotgunEvent shotgunEvent)
        {
            if (remotePlayerId == 0u)
            {
                throw new InvalidOperationException(
                    "Remote shotgun receiver is not configured.");
            }

            if (senderPlayerId == 0u)
            {
                throw new ArgumentException(
                    "Shotgun sender ID must be non-zero.",
                    nameof(senderPlayerId));
            }

            if (shotgunEvent == null)
            {
                throw new ArgumentNullException(
                    nameof(shotgunEvent));
            }

            if (shotgunEvent.PlayerId !=
                remotePlayerId)
            {
                throw new ArgumentException(
                    "Shotgun event player ID does not match " +
                    "the configured remote player.",
                    nameof(shotgunEvent));
            }

            uint sequence =
                shotgunEvent.VolleySequence;

            if (hasReceivedSequence &&
                !IsNewerSequence(
                    sequence,
                    lastVolleySequence))
            {
                throw new ArgumentException(
                    "Shotgun event sequence is duplicate " +
                    "or expired.",
                    nameof(shotgunEvent));
            }

            lastVolleySequence =
                sequence;

            hasReceivedSequence =
                true;

            pendingShotgunEvents.Enqueue(
                shotgunEvent);
        }

        public bool TryDequeue(
            out PlayerShotgunEvent shotgunEvent)
        {
            if (pendingShotgunEvents.Count == 0)
            {
                shotgunEvent =
                    null;

                return false;
            }

            shotgunEvent =
                pendingShotgunEvents.Dequeue();

            return true;
        }

        public void Clear()
        {
            pendingShotgunEvents.Clear();
        }

        private static bool IsNewerSequence(
            uint candidate,
            uint previous)
        {
            uint difference =
                unchecked(
                    candidate - previous);

            return difference != 0u &&
                difference < 0x80000000u;
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