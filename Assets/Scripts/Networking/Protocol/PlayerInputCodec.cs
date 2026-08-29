using System;

namespace TopDownRoguelike.Networking.Protocol
{
    public sealed class PlayerInputPayload
    {
        public PlayerInputPayload(
    float moveX,
    float moveY,
    float aimX,
    float aimY,
    bool fireHeld = false,
    uint dashRequestSequence = 0u)
        {
            MoveX = moveX;
            MoveY = moveY;
            AimX = aimX;
            AimY = aimY;
            FireHeld = fireHeld;
            DashRequestSequence =
                dashRequestSequence;
        }

        public float MoveX { get; }
        public float MoveY { get; }
        public float AimX { get; }
        public float AimY { get; }
        public bool FireHeld { get; }
        public uint DashRequestSequence { get; }
    }

    public static class PlayerInputCodec
    {
        public const int PayloadSize = 24;

        private const int MoveXOffset = 0;
        private const int MoveYOffset = 4;
        private const int AimXOffset = 8;
        private const int AimYOffset = 12;
        private const int FlagsOffset = 16;
        private const int DashRequestSequenceOffset = 20;
        private const uint FireHeldFlag = 1u;
        private const uint KnownFlags = FireHeldFlag;

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

            uint flags =
                input.FireHeld
                    ? FireHeldFlag
                    : 0u;

            PacketCodec.WriteNetworkUInt32(
                payload,
                FlagsOffset,
                flags);

            PacketCodec.WriteNetworkUInt32(
                payload,
                DashRequestSequenceOffset,
                input.DashRequestSequence);

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

            uint flags =
                PacketCodec.ReadNetworkUInt32(
                    payload,
                    FlagsOffset);

            if ((flags & ~KnownFlags) != 0u)
            {
                throw new ArgumentException(
                    "Player input contains unknown flags.",
                    nameof(payload));
            }

            bool fireHeld =
                (flags & FireHeldFlag) != 0u;

            var input = new PlayerInputPayload(
                ReadNetworkFloat(payload, MoveXOffset),
                ReadNetworkFloat(payload, MoveYOffset),
                ReadNetworkFloat(payload, AimXOffset),
                ReadNetworkFloat(payload, AimYOffset),
                fireHeld,
                PacketCodec.ReadNetworkUInt32(
                    payload,
                    DashRequestSequenceOffset));

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