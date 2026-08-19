using System;

namespace TopDownRoguelike.Networking.Protocol
{
    public readonly struct UdpBindingCredentials
    {
        public UdpBindingCredentials(
            uint playerId,
            byte[] sessionToken)
        {
            if (sessionToken == null)
            {
                throw new ArgumentNullException(
                    nameof(sessionToken));
            }

            if (sessionToken.Length !=
                UdpPacketCodec.SessionTokenSize)
            {
                throw new ArgumentException(
                    "UDP session token must contain 16 bytes.",
                    nameof(sessionToken));
            }

            PlayerId =
                playerId;

            SessionToken =
                (byte[])sessionToken.Clone();
        }

        public uint PlayerId { get; }

        public byte[] SessionToken { get; }
    }

    public static class
        UdpBindingCredentialsCodec
    {
        public const int PlayerIdOffset =
            0;

        public const int SessionTokenOffset =
            4;

        public const int CredentialsSize =
            20;

        public static byte[] Encode(
            UdpBindingCredentials credentials)
        {
            if (credentials.SessionToken == null ||
                credentials.SessionToken.Length !=
                UdpPacketCodec.SessionTokenSize)
            {
                throw new ArgumentException(
                    "UDP session token must contain 16 bytes.",
                    nameof(credentials));
            }

            var encoded =
                new byte[CredentialsSize];

            PacketCodec.WriteNetworkUInt32(
                encoded,
                PlayerIdOffset,
                credentials.PlayerId);

            Buffer.BlockCopy(
                credentials.SessionToken,
                0,
                encoded,
                SessionTokenOffset,
                UdpPacketCodec.SessionTokenSize);

            return encoded;
        }

        public static UdpBindingCredentials Decode(
            byte[] payload)
        {
            if (payload == null)
            {
                throw new ArgumentNullException(
                    nameof(payload));
            }

            if (payload.Length !=
                CredentialsSize)
            {
                throw new ArgumentException(
                    "UDP binding credentials must contain 20 bytes.",
                    nameof(payload));
            }

            uint playerId =
                PacketCodec.ReadNetworkUInt32(
                    payload,
                    PlayerIdOffset);

            var sessionToken =
                new byte[
                    UdpPacketCodec.SessionTokenSize];

            Buffer.BlockCopy(
                payload,
                SessionTokenOffset,
                sessionToken,
                0,
                sessionToken.Length);

            return new UdpBindingCredentials(
                playerId,
                sessionToken);
        }
    }
}