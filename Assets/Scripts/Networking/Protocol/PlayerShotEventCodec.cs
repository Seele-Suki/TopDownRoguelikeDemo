using System;

namespace TopDownRoguelike.Networking.Protocol
{
    public static class PlayerShotEventCodec
    {
        public const int PayloadSize = 24;

        public static byte[] Encode(
            PlayerShotEvent shotEvent)
        {
            Validate(shotEvent);

            var payload =
                new byte[PayloadSize];

            PacketCodec.WriteNetworkUInt32(
                payload,
                0,
                shotEvent.PlayerId);

            PacketCodec.WriteNetworkUInt32(
                payload,
                4,
                shotEvent.ShotSequence);

            WriteNetworkFloat(
                payload,
                8,
                shotEvent.OriginX);

            WriteNetworkFloat(
                payload,
                12,
                shotEvent.OriginY);

            WriteNetworkFloat(
                payload,
                16,
                shotEvent.DirectionX);

            WriteNetworkFloat(
                payload,
                20,
                shotEvent.DirectionY);

            return payload;
        }

        public static PlayerShotEvent Decode(
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
                    "Player shot event payload has an invalid size.",
                    nameof(payload));
            }

            var shotEvent =
                new PlayerShotEvent(
                    PacketCodec.ReadNetworkUInt32(
                        payload,
                        0),
                    PacketCodec.ReadNetworkUInt32(
                        payload,
                        4),
                    ReadNetworkFloat(payload, 8),
                    ReadNetworkFloat(payload, 12),
                    ReadNetworkFloat(payload, 16),
                    ReadNetworkFloat(payload, 20));

            return shotEvent;
        }

        private static void Validate(
            PlayerShotEvent shotEvent)
        {
            if (shotEvent.PlayerId == 0u)
            {
                throw new ArgumentException(
                    "Player shot event ID must be non-zero.");
            }

            if (!IsFinite(shotEvent.OriginX) ||
                !IsFinite(shotEvent.OriginY) ||
                !IsFinite(shotEvent.DirectionX) ||
                !IsFinite(shotEvent.DirectionY))
            {
                throw new ArgumentException(
                    "Player shot event contains a non-finite value.");
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
            byte[] bytes =
                BitConverter.GetBytes(value);

            uint bits =
                BitConverter.ToUInt32(
                    bytes,
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

            return BitConverter.ToSingle(
                BitConverter.GetBytes(bits),
                0);
        }
    }
}