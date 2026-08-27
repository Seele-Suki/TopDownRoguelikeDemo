using System;

namespace TopDownRoguelike.Networking.Protocol
{
    public sealed class PlayerInputPayload
    {
        public PlayerInputPayload(
            float moveX,
            float moveY,
            float aimX,
            float aimY)
        {
            MoveX = moveX;
            MoveY = moveY;
            AimX = aimX;
            AimY = aimY;
        }

        public float MoveX { get; }
        public float MoveY { get; }
        public float AimX { get; }
        public float AimY { get; }
    }

    public static class PlayerInputCodec
    {
        public const int PayloadSize = 20;

        private const int MoveXOffset = 0;
        private const int MoveYOffset = 4;
        private const int AimXOffset = 8;
        private const int AimYOffset = 12;
        private const int ReservedOffset = 16;

        public static byte[] Encode(
            PlayerInputPayload input)
        {
            if (input == null)
            {
                throw new ArgumentNullException(
                    nameof(input));
            }

            Validate(input);

            var payload = new byte[PayloadSize];

            WriteNetworkFloat(
                payload,
                MoveXOffset,
                input.MoveX);

            WriteNetworkFloat(
                payload,
                MoveYOffset,
                input.MoveY);

            WriteNetworkFloat(
                payload,
                AimXOffset,
                input.AimX);

            WriteNetworkFloat(
                payload,
                AimYOffset,
                input.AimY);

            PacketCodec.WriteNetworkUInt32(
                payload,
                ReservedOffset,
                0u);

            return payload;
        }

        public static PlayerInputPayload Decode(
            byte[] payload)
        {
            if (payload == null)
            {
                throw new ArgumentNullException(
                    nameof(payload));
            }

            if (payload.Length != PayloadSize)
            {
                throw new ArgumentException(
                    "Player input payload has an invalid size.",
                    nameof(payload));
            }

            uint reserved =
                PacketCodec.ReadNetworkUInt32(
                    payload,
                    ReservedOffset);

            if (reserved != 0u)
            {
                throw new ArgumentException(
                    "Player input reserved field must be zero.",
                    nameof(payload));
            }

            var input = new PlayerInputPayload(
                ReadNetworkFloat(
                    payload,
                    MoveXOffset),

                ReadNetworkFloat(
                    payload,
                    MoveYOffset),

                ReadNetworkFloat(
                    payload,
                    AimXOffset),

                ReadNetworkFloat(
                    payload,
                    AimYOffset));

            Validate(input);

            return input;
        }

        private static void Validate(
            PlayerInputPayload input)
        {
            if (!IsFinite(input.MoveX) ||
                !IsFinite(input.MoveY) ||
                !IsFinite(input.AimX) ||
                !IsFinite(input.AimY))
            {
                throw new ArgumentException(
                    "Player input contains a non-finite value.");
            }

            if (input.MoveX < -1.0f ||
                input.MoveX > 1.0f ||
                input.MoveY < -1.0f ||
                input.MoveY > 1.0f)
            {
                throw new ArgumentException(
                    "Player movement component is outside [-1, 1].");
            }

            float movementMagnitudeSquared =
                input.MoveX * input.MoveX +
                input.MoveY * input.MoveY;

            if (movementMagnitudeSquared > 1.0001f)
            {
                throw new ArgumentException(
                    "Player movement magnitude exceeds one.");
            }
        }

        private static bool IsFinite(
            float value)
        {
            return !float.IsNaN(value) &&
                !float.IsInfinity(value);
        }

        private static void WriteNetworkFloat(
            byte[] destination,
            int offset,
            float value)
        {
            byte[] localBytes =
                BitConverter.GetBytes(value);

            uint bits =
                BitConverter.ToUInt32(
                    localBytes,
                    0);

            PacketCodec.WriteNetworkUInt32(
                destination,
                offset,
                bits);
        }

        private static float ReadNetworkFloat(
            byte[] source,
            int offset)
        {
            uint bits =
                PacketCodec.ReadNetworkUInt32(
                    source,
                    offset);

            byte[] localBytes =
                BitConverter.GetBytes(bits);

            return BitConverter.ToSingle(
                localBytes,
                0);
        }
    }
}