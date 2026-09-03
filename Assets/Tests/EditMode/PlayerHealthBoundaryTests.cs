using NUnit.Framework;
using TopDownRoguelike.Networking.Protocol;

namespace TopDownRoguelike.Tests
{
    public sealed class PlayerHealthBoundaryTests
    {
        [Test]
        public void PlayerStateHealthFields_AcceptMinimumAndMaximumUint16()
        {
            var snapshot =
                new PlayerStateSnapshotPayload(
                    new[]
                    {
                        new PlayerStateRecord(
                            1u, 0.0f, 0.0f, 1.0f, 0.0f,
                            false, false, 1, 1),
                        new PlayerStateRecord(
                            2u, 0.0f, 0.0f, 1.0f, 0.0f,
                            false, false,
                            ushort.MaxValue,
                            ushort.MaxValue)
                    });

            PlayerStateSnapshotPayload decoded =
                PlayerStateSnapshotCodec.Decode(
                    PlayerStateSnapshotCodec.Encode(snapshot));

            Assert.That(
                decoded.Players[0].CurrentHealth,
                Is.EqualTo((ushort)1));
            Assert.That(
                decoded.Players[0].MaxHealth,
                Is.EqualTo((ushort)1));
            Assert.That(
                decoded.Players[1].CurrentHealth,
                Is.EqualTo(ushort.MaxValue));
            Assert.That(
                decoded.Players[1].MaxHealth,
                Is.EqualTo(ushort.MaxValue));
        }
    }
}
