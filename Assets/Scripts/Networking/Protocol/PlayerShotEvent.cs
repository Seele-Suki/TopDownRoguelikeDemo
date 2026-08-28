using System;

namespace TopDownRoguelike.Networking.Protocol
{
    public sealed class PlayerShotEvent
    {
        public PlayerShotEvent(
            uint playerId,
            uint shotSequence,
            float originX,
            float originY,
            float directionX,
            float directionY)
        {
            if (playerId == 0u)
            {
                throw new ArgumentException(
                    "Player shot event requires a valid player ID.",
                    nameof(playerId));
            }

            if (!IsFinite(originX) ||
                !IsFinite(originY) ||
                !IsFinite(directionX) ||
                !IsFinite(directionY))
            {
                throw new ArgumentException(
                    "Player shot event contains a non-finite value.");
            }

            float directionMagnitudeSquared =
                directionX * directionX +
                directionY * directionY;

            if (directionMagnitudeSquared < 0.0001f)
            {
                throw new ArgumentException(
                    "Player shot event requires a non-zero direction.");
            }

            PlayerId =
                playerId;

            ShotSequence =
                shotSequence;

            OriginX =
                originX;

            OriginY =
                originY;

            DirectionX =
                directionX;

            DirectionY =
                directionY;
        }

        public uint PlayerId
        {
            get;
        }

        public uint ShotSequence
        {
            get;
        }

        public float OriginX
        {
            get;
        }

        public float OriginY
        {
            get;
        }

        public float DirectionX
        {
            get;
        }

        public float DirectionY
        {
            get;
        }

        private static bool IsFinite(
            float value)
        {
            return !float.IsNaN(value) &&
                !float.IsInfinity(value);
        }
    }
}