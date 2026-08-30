using System;

namespace TopDownRoguelike.Networking.Protocol
{
    public sealed class PlayerShotgunEvent
    {
        public const uint MaxProjectileCount = 32u;

        public PlayerShotgunEvent(
            uint playerId,
            uint volleySequence,
            float originX,
            float originY,
            float centerDirectionX,
            float centerDirectionY,
            uint projectileCount,
            float spreadAngle,
            float effectiveCooldown)
        {
            if (playerId == 0u)
            {
                throw new ArgumentException(
                    "Player shotgun event requires " +
                    "a valid player ID.",
                    nameof(playerId));
            }

            if (!IsFinite(originX) ||
                !IsFinite(originY) ||
                !IsFinite(centerDirectionX) ||
                !IsFinite(centerDirectionY) ||
                !IsFinite(spreadAngle) ||
                !IsFinite(effectiveCooldown))
            {
                throw new ArgumentException(
                    "Player shotgun event contains " +
                    "a non-finite value.");
            }

            float directionMagnitudeSquared =
                centerDirectionX * centerDirectionX +
                centerDirectionY * centerDirectionY;

            if (directionMagnitudeSquared < 0.0001f)
            {
                throw new ArgumentException(
                    "Player shotgun event requires " +
                    "a non-zero center direction.");
            }

            if (projectileCount == 0u ||
                projectileCount > MaxProjectileCount)
            {
                throw new ArgumentException(
                    "Player shotgun projectile count " +
                    "is outside the supported range.",
                    nameof(projectileCount));
            }

            if (spreadAngle < 0f ||
                spreadAngle > 180f)
            {
                throw new ArgumentException(
                    "Player shotgun spread angle " +
                    "must be between 0 and 180.",
                    nameof(spreadAngle));
            }

            if (effectiveCooldown < 0f)
            {
                throw new ArgumentException(
                    "Player shotgun cooldown " +
                    "cannot be negative.",
                    nameof(effectiveCooldown));
            }

            PlayerId = playerId;
            VolleySequence = volleySequence;
            OriginX = originX;
            OriginY = originY;
            CenterDirectionX = centerDirectionX;
            CenterDirectionY = centerDirectionY;
            ProjectileCount = projectileCount;
            SpreadAngle = spreadAngle;
            EffectiveCooldown = effectiveCooldown;
        }

        public uint PlayerId { get; }
        public uint VolleySequence { get; }
        public float OriginX { get; }
        public float OriginY { get; }
        public float CenterDirectionX { get; }
        public float CenterDirectionY { get; }
        public uint ProjectileCount { get; }
        public float SpreadAngle { get; }
        public float EffectiveCooldown { get; }

        private static bool IsFinite(
            float value)
        {
            return !float.IsNaN(value) &&
                !float.IsInfinity(value);
        }
    }
}