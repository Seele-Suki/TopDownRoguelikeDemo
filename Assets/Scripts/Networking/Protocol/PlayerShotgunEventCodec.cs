using System;

namespace TopDownRoguelike.Networking.Protocol
{
    public static class PlayerShotgunEventCodec
    {
        public const int PayloadSize = 36;

        public static byte[] Encode(
            PlayerShotgunEvent shotgunEvent)
        {
            if (shotgunEvent == null)
            {
                throw new ArgumentNullException(
                    nameof(shotgunEvent));
            }

            var payload =
                new byte[PayloadSize];

            PacketCodec.WriteNetworkUInt32(
                payload,
                0,
                shotgunEvent.PlayerId);

            PacketCodec.WriteNetworkUInt32(
                payload,
                4,
                shotgunEvent.VolleySequence);

            WriteNetworkFloat(
                payload,
                8,
                shotgunEvent.OriginX);

            WriteNetworkFloat(
                payload,
                12,
                shotgunEvent.OriginY);

            WriteNetworkFloat(
                payload,
                16,
                shotgunEvent.CenterDirectionX);

            WriteNetworkFloat(
                payload,
                20,
                shotgunEvent.CenterDirectionY);

            PacketCodec.WriteNetworkUInt32(
                payload,
                24,
                shotgunEvent.ProjectileCount);

            WriteNetworkFloat(
                payload,
                28,
                shotgunEvent.SpreadAngle);

            WriteNetworkFloat(
                payload,
                32,
                shotgunEvent.EffectiveCooldown);

            return payload;
        }

        public static PlayerShotgunEvent Decode(
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
                    "Player shotgun event payload " +
                    "has an invalid size.",
                    nameof(payload));
            }

            return new PlayerShotgunEvent(
                PacketCodec.ReadNetworkUInt32(
                    payload,
                    0),
                PacketCodec.ReadNetworkUInt32(
                    payload,
                    4),
                ReadNetworkFloat(payload, 8),
                ReadNetworkFloat(payload, 12),
                ReadNetworkFloat(payload, 16),
                ReadNetworkFloat(payload, 20),
                PacketCodec.ReadNetworkUInt32(
                    payload,
                    24),
                ReadNetworkFloat(payload, 28),
                ReadNetworkFloat(payload, 32));
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