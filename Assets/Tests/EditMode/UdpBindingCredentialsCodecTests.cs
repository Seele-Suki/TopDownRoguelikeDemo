using System;
using NUnit.Framework;
using TopDownRoguelike.Networking.Protocol;

namespace TopDownRoguelike.Tests.EditMode
{
    public sealed class
        UdpBindingCredentialsCodecTests
    {
        [Test]
        public void Encode_MatchesCppLayout()
        {
            byte[] token =
                CreateToken();

            var credentials =
                new UdpBindingCredentials(
                    0x01020304u,
                    token);

            byte[] encoded =
                UdpBindingCredentialsCodec.Encode(
                    credentials);

            var expected =
                new byte[]
                {
                    0x01, 0x02, 0x03, 0x04,

                    0x00, 0x01, 0x02, 0x03,
                    0x04, 0x05, 0x06, 0x07,
                    0x08, 0x09, 0x0A, 0x0B,
                    0x0C, 0x0D, 0x0E, 0x0F
                };

            Assert.That(
                encoded,
                Is.EqualTo(expected));
        }

        [Test]
        public void EncodeThenDecode_PreservesCredentials()
        {
            byte[] token =
                CreateToken();

            var original =
                new UdpBindingCredentials(
                    42u,
                    token);

            byte[] encoded =
                UdpBindingCredentialsCodec.Encode(
                    original);

            UdpBindingCredentials decoded =
                UdpBindingCredentialsCodec.Decode(
                    encoded);

            Assert.That(
                decoded.PlayerId,
                Is.EqualTo(42u));

            Assert.That(
                decoded.SessionToken,
                Is.EqualTo(token));
        }

        [Test]
        public void DecodeWrongSize_RejectsPayload()
        {
            var payload =
                new byte[
                    UdpBindingCredentialsCodec
                        .CredentialsSize - 1];

            Assert.Throws<ArgumentException>(
                () =>
                    UdpBindingCredentialsCodec.Decode(
                        payload));
        }

        [Test]
        public void ConstructorWrongTokenSize_RejectsToken()
        {
            var token =
                new byte[
                    UdpPacketCodec.SessionTokenSize - 1];

            Assert.Throws<ArgumentException>(
                () =>
                    new UdpBindingCredentials(
                        1u,
                        token));
        }

        private static byte[] CreateToken()
        {
            var token =
                new byte[
                    UdpPacketCodec.SessionTokenSize];

            for (int index = 0;
                index < token.Length;
                index++)
            {
                token[index] =
                    (byte)index;
            }

            return token;
        }
    }
}