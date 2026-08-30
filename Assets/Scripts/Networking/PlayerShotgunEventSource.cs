using System;
using TopDownRoguelike.Networking.Protocol;
using UnityEngine;

namespace TopDownRoguelike.Gameplay.Networking
{
    public sealed class PlayerShotgunEventSource
        : MonoBehaviour
    {
        private uint playerId;
        private uint nextVolleySequence = 1u;

        public event Action<PlayerShotgunEvent>
            ShotgunGenerated;

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

            nextVolleySequence =
                1u;
        }

        public void NotifyShotgun(
            Vector2 centerDirection,
            uint projectileCount,
            float spreadAngle,
            float effectiveCooldown)
        {
            if (playerId == 0u)
            {
                throw new InvalidOperationException(
                    "Shotgun event source is not configured.");
            }

            if (centerDirection.sqrMagnitude <
                0.0001f)
            {
                throw new ArgumentException(
                    "Shotgun center direction cannot be zero.",
                    nameof(centerDirection));
            }

            if (projectileCount == 0u ||
                projectileCount >
                PlayerShotgunEvent.MaxProjectileCount)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(projectileCount));
            }

            if (float.IsNaN(spreadAngle) ||
                float.IsInfinity(spreadAngle) ||
                spreadAngle < 0.0f ||
                spreadAngle > 180.0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(spreadAngle));
            }

            if (float.IsNaN(effectiveCooldown) ||
                float.IsInfinity(effectiveCooldown) ||
                effectiveCooldown < 0.0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(effectiveCooldown));
            }

            Vector2 normalizedDirection =
                centerDirection.normalized;

            PlayerShotgunEvent shotgunEvent =
                new PlayerShotgunEvent(
                    playerId,
                    nextVolleySequence,
                    transform.position.x,
                    transform.position.y,
                    normalizedDirection.x,
                    normalizedDirection.y,
                    projectileCount,
                    spreadAngle,
                    effectiveCooldown);

            nextVolleySequence =
                unchecked(
                    nextVolleySequence + 1u);

            ShotgunGenerated?.Invoke(
                shotgunEvent);
        }
    }
}