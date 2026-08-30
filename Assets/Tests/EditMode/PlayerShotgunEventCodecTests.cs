using System;
using NUnit.Framework;
using TopDownRoguelike.Networking.Protocol;

namespace TopDownRoguelike.Tests.EditMode
{
    public sealed class PlayerShotgunEventCodecTests
    {
        [Test]
        public void MessageTypeUsesValue37AndIsAcceptedForUdp()
        {
            Assert.That(
                (ushort)MessageType.PlayerShotgunEvent,
                Is.EqualTo(37));

            var header =
                new UdpMessageHeader(
                    MessageType.PlayerShotgunEvent,
                    new byte[UdpPacketCodec.SessionTokenSize],
                    7u,
                    9u);

            Assert.DoesNotThrow(
                () =>
                    UdpPacketCodec.Encode(
                        header,
                        Array.Empty<byte>()));
        }

        [Test]
        public void EncodeUsesStableCppWireLayout()
        {
            var source =
                new PlayerShotgunEvent(
                    0x01020304u,
                    0x05060708u,
                    1.5f,
                    -2.25f,
                    0.6f,
                    0.8f,
                    5u,
                    40f,
                    4f);

            byte[] expected =
            {
                // PlayerId
                0x01, 0x02, 0x03, 0x04,

                // VolleySequence
                0x05, 0x06, 0x07, 0x08,

                // OriginX: 1.5
                0x3F, 0xC0, 0x00, 0x00,

                // OriginY: -2.25
                0xC0, 0x10, 0x00, 0x00,

                // CenterDirectionX: 0.6
                0x3F, 0x19, 0x99, 0x9A,

                // CenterDirectionY: 0.8
                0x3F, 0x4C, 0xCC, 0xCD,

                // ProjectileCount: 5
                0x00, 0x00, 0x00, 0x05,

                // SpreadAngle: 40
                0x42, 0x20, 0x00, 0x00,

                // EffectiveCooldown: 4
                0x40, 0x80, 0x00, 0x00
            };

            byte[] encoded =
                PlayerShotgunEventCodec.Encode(
                    source);

            Assert.That(
                PlayerShotgunEventCodec.PayloadSize,
                Is.EqualTo(36));

            Assert.That(
                encoded,
                Is.EqualTo(expected));

            PlayerShotgunEvent decoded =
                PlayerShotgunEventCodec.Decode(
                    encoded);

            Assert.That(decoded.PlayerId, Is.EqualTo(0x01020304u));
            Assert.That(decoded.VolleySequence, Is.EqualTo(0x05060708u));
            Assert.That(decoded.OriginX, Is.EqualTo(1.5f));
            Assert.That(decoded.OriginY, Is.EqualTo(-2.25f));
            Assert.That(decoded.CenterDirectionX, Is.EqualTo(0.6f));
            Assert.That(decoded.CenterDirectionY, Is.EqualTo(0.8f));
            Assert.That(decoded.ProjectileCount, Is.EqualTo(5u));
            Assert.That(decoded.SpreadAngle, Is.EqualTo(40f));
            Assert.That(decoded.EffectiveCooldown, Is.EqualTo(4f));
        }

        [Test]
        public void InvalidGameplayParametersAreRejected()
        {
            Assert.Throws<ArgumentException>(
                () => CreateValid(playerId: 0u));

            Assert.Throws<ArgumentException>(
                () => CreateValid(
                    centerDirectionX: 0f,
                    centerDirectionY: 0f));

            Assert.Throws<ArgumentException>(
                () => CreateValid(projectileCount: 0u));

            Assert.Throws<ArgumentException>(
                () => CreateValid(projectileCount: 33u));

            Assert.Throws<ArgumentException>(
                () => CreateValid(spreadAngle: -1f));

            Assert.Throws<ArgumentException>(
                () => CreateValid(spreadAngle: 181f));

            Assert.Throws<ArgumentException>(
                () => CreateValid(effectiveCooldown: -1f));

            Assert.Throws<ArgumentException>(
                () => CreateValid(originX: float.NaN));
        }

        [Test]
        public void DecodeRejectsMalformedPayloadSize()
        {
            Assert.Throws<ArgumentException>(
                () =>
                    PlayerShotgunEventCodec.Decode(
                        new byte[
                            PlayerShotgunEventCodec.PayloadSize - 1]));

            Assert.Throws<ArgumentException>(
                () =>
                    PlayerShotgunEventCodec.Decode(
                        new byte[
                            PlayerShotgunEventCodec.PayloadSize + 1]));

            Assert.Throws<ArgumentNullException>(
                () =>
                    PlayerShotgunEventCodec.Decode(
                        null));

            Assert.Throws<ArgumentNullException>(
                () =>
                    PlayerShotgunEventCodec.Encode(
                        null));
        }

        private static PlayerShotgunEvent CreateValid(
            uint playerId = 7u,
            float originX = 1f,
            float centerDirectionX = 1f,
            float centerDirectionY = 0f,
            uint projectileCount = 5u,
            float spreadAngle = 40f,
            float effectiveCooldown = 4f)
        {
            return new PlayerShotgunEvent(
                playerId,
                1u,
                originX,
                2f,
                centerDirectionX,
                centerDirectionY,
                projectileCount,
                spreadAngle,
                effectiveCooldown);
        }
    }
}