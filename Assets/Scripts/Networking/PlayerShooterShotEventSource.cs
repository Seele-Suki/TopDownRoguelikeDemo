using System;
using TopDownRoguelike.Networking.Protocol;
using UnityEngine;

namespace TopDownRoguelike.Gameplay.Networking
{
    public sealed class PlayerShooterShotEventSource
        : MonoBehaviour
    {
        private uint playerId;
        private uint nextShotSequence = 1u;

        public event Action<PlayerShotEvent>
            ShotGenerated;

        public void Configure(
            uint newPlayerId)
        {
            if (newPlayerId == 0u)
            {
                throw new ArgumentException(
                    "Player ID must be non-zero.",
                    nameof(newPlayerId));
            }

            playerId =
                newPlayerId;

            nextShotSequence =
                1u;
        }

        public void NotifyShot(
            Vector2 direction)
        {
            if (playerId == 0u)
            {
                throw new InvalidOperationException(
                    "Shot event source is not configured.");
            }

            if (direction.sqrMagnitude <
                0.0001f)
            {
                throw new ArgumentException(
                    "Shot direction cannot be zero.",
                    nameof(direction));
            }

            Vector2 normalizedDirection =
                direction.normalized;

            PlayerShotEvent shotEvent =
                new PlayerShotEvent(
                    playerId,
                    nextShotSequence,
                    transform.position.x,
                    transform.position.y,
                    normalizedDirection.x,
                    normalizedDirection.y);

            nextShotSequence =
                unchecked(
                    nextShotSequence + 1u);

            ShotGenerated?.Invoke(
                shotEvent);
        }
    }
}