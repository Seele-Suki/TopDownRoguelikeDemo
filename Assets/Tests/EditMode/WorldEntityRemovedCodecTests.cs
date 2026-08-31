using System;
using NUnit.Framework;
using TopDownRoguelike.Networking.Protocol;

namespace TopDownRoguelike.Tests.EditMode
{
    public sealed class WorldEntityRemovedCodecTests
    {
        [Test]
        public void EncodeThenDecode_UsesStableEightByteLayout()
        {
            var expected =
                new WorldEntityRemovedPayload(
                    0x1000002Au,
                    NetworkEntityType.Enemy,
                    WorldEntityRemovalReason.Died);

            byte[] encoded =
                WorldEntityRemovedCodec.Encode(expected);

            Assert.That(encoded, Is.EqualTo(new byte[]
            {
                0x10, 0x00, 0x00, 0x2A,
                0x02,
                0x01,
                0x00, 0x00
            }));

            WorldEntityRemovedPayload decoded =
                WorldEntityRemovedCodec.Decode(encoded);

            Assert.That(decoded.EntityId, Is.EqualTo(expected.EntityId));
            Assert.That(decoded.EntityType, Is.EqualTo(NetworkEntityType.Enemy));
            Assert.That(decoded.Reason, Is.EqualTo(WorldEntityRemovalReason.Died));
        }

        [Test]
        public void Decode_RejectsInvalidOrReservedFields()
        {
            byte[] valid =
                WorldEntityRemovedCodec.Encode(
                    new WorldEntityRemovedPayload(
                        1u,
                        NetworkEntityType.Enemy,
                        WorldEntityRemovalReason.Died));

            Assert.Throws<ArgumentException>(
                () => WorldEntityRemovedCodec.Decode(new byte[7]));

            valid[6] = 1;

            Assert.Throws<ArgumentException>(
                () => WorldEntityRemovedCodec.Decode(valid));
        }
    }
}
