using System;
using NUnit.Framework;
using TopDownRoguelike.Networking.Protocol;
using UnityEngine;

namespace TopDownRoguelike.Tests.EditMode
{
    public sealed class PlayerShotEventCodecTests
    {
        [Test]
        public void EncodeDecode_PreservesAllFields()
        {
            var source =
                new PlayerShotEvent(
                    7u,
                    42u,
                    1.25f,
                    -2.5f,
                    0.6f,
                    0.8f);

            byte[] encoded =
                PlayerShotEventCodec.Encode(source);

            Assert.That(
                encoded.Length,
                Is.EqualTo(PlayerShotEventCodec.PayloadSize));

            PlayerShotEvent decoded =
                PlayerShotEventCodec.Decode(encoded);

            Assert.That(decoded.PlayerId, Is.EqualTo(7u));
            Assert.That(decoded.ShotSequence, Is.EqualTo(42u));
            Assert.That(decoded.OriginX, Is.EqualTo(1.25f));
            Assert.That(decoded.OriginY, Is.EqualTo(-2.5f));
            Assert.That(decoded.DirectionX, Is.EqualTo(0.6f));
            Assert.That(decoded.DirectionY, Is.EqualTo(0.8f));
        }

        [Test]
        public void Decode_RejectsTruncatedPayload()
        {
            Assert.Throws<ArgumentException>(
                () =>
                    PlayerShotEventCodec.Decode(
                        new byte[PlayerShotEventCodec.PayloadSize - 1]));
        }

        [Test]
        public void Decode_RejectsTrailingBytes()
        {
            Assert.Throws<ArgumentException>(
                () =>
                    PlayerShotEventCodec.Decode(
                        new byte[PlayerShotEventCodec.PayloadSize + 1]));
        }

        [Test]
        public void Decode_RejectsNonFiniteOrigin()
        {
            var source =
                new PlayerShotEvent(
                    7u,
                    42u,
                    0.0f,
                    0.0f,
                    1.0f,
                    0.0f);

            byte[] encoded =
                PlayerShotEventCodec.Encode(source);

            PacketCodec.WriteNetworkUInt32(
                encoded,
                8,
                0x7FC00000u);

            Assert.Throws<ArgumentException>(
                () =>
                    PlayerShotEventCodec.Decode(encoded));
        }

        [Test]
        public void Decode_RejectsNonFiniteDirection()
        {
            var source =
                new PlayerShotEvent(
                    7u,
                    42u,
                    0.0f,
                    0.0f,
                    1.0f,
                    0.0f);

            byte[] encoded =
                PlayerShotEventCodec.Encode(source);

            PacketCodec.WriteNetworkUInt32(
                encoded,
                16,
                0x7F800000u);

            Assert.Throws<ArgumentException>(
                () =>
                    PlayerShotEventCodec.Decode(encoded));
        }
    }
}
