using System.Reflection;
using NUnit.Framework;
using TopDownRoguelike.Networking.Protocol;

namespace TopDownRoguelike.Tests.EditMode
{
    public sealed class PlayerShotgunInputProtocolTests
    {
        [Test]
        public void PlayerInputCarriesShotgunRequestSequenceInNetworkOrder()
        {
            ConstructorInfo constructor =
                typeof(PlayerInputPayload).GetConstructor(
                    new[]
                    {
                        typeof(float),
                        typeof(float),
                        typeof(float),
                        typeof(float),
                        typeof(bool),
                        typeof(uint),
                        typeof(uint)
                    });

            Assert.That(
                constructor,
                Is.Not.Null,
                "PlayerInputPayload must accept " +
                "ShotgunRequestSequence.");

            var input =
                (PlayerInputPayload)constructor.Invoke(
                    new object[]
                    {
                        0.5f,
                        -0.25f,
                        1.0f,
                        -1.0f,
                        true,
                        0x01020304u,
                        0x05060708u
                    });

            byte[] expected =
            {
                // Move X: 0.5
                0x3F, 0x00, 0x00, 0x00,

                // Move Y: -0.25
                0xBE, 0x80, 0x00, 0x00,

                // Aim X: 1
                0x3F, 0x80, 0x00, 0x00,

                // Aim Y: -1
                0xBF, 0x80, 0x00, 0x00,

                // Flags: bit 0 = FireHeld
                0x00, 0x00, 0x00, 0x01,

                // DashRequestSequence
                0x01, 0x02, 0x03, 0x04,

                // ShotgunRequestSequence
                0x05, 0x06, 0x07, 0x08
            };

            byte[] encoded =
                PlayerInputCodec.Encode(input);

            Assert.That(encoded, Is.EqualTo(expected));
            Assert.That(PlayerInputCodec.PayloadSize, Is.EqualTo(28));

            PlayerInputPayload decoded =
                PlayerInputCodec.Decode(encoded);

            PropertyInfo property =
                typeof(PlayerInputPayload).GetProperty(
                    "ShotgunRequestSequence");

            Assert.That(
                property,
                Is.Not.Null,
                "PlayerInputPayload must expose " +
                "ShotgunRequestSequence.");

            Assert.That(
                (uint)property.GetValue(decoded),
                Is.EqualTo(0x05060708u));

            Assert.That(
                decoded.DashRequestSequence,
                Is.EqualTo(0x01020304u));

            Assert.That(decoded.FireHeld, Is.True);
        }
    }
}