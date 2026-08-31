using NUnit.Framework;
using System;
using System.Reflection;
using TopDownRoguelike.Networking.Protocol;

namespace TopDownRoguelike.Tests.EditMode
{
    public sealed class PlayerSyncCodecTests
    {
        [Test]
        public void PlayerInputEncodeThenDecode_MatchesCppWireFormat()
        {
            var original = new PlayerInputPayload(
                0.5f,
                -0.25f,
                1.0f,
                -1.0f);

            var expected = new byte[]
{
                // Move X: 0.5
                0x3F, 0x00, 0x00, 0x00,

                // Move Y: -0.25
                0xBE, 0x80, 0x00, 0x00,

                // Aim X: 1
                0x3F, 0x80, 0x00, 0x00,

                // Aim Y: -1
                0xBF, 0x80, 0x00, 0x00,

                // Flags: FireHeld is not set
                0x00, 0x00, 0x00, 0x00,

                // DashRequestSequence: no request yet
                0x00, 0x00, 0x00, 0x00,

                // ShotgunRequestSequence: no request yet
                0x00, 0x00, 0x00, 0x00
};

            byte[] encoded =
                PlayerInputCodec.Encode(original);

            Assert.That(encoded, Is.EqualTo(expected));

            PlayerInputPayload decoded =
                PlayerInputCodec.Decode(encoded);

            Assert.That(decoded.MoveX, Is.EqualTo(0.5f));
            Assert.That(decoded.MoveY, Is.EqualTo(-0.25f));
            Assert.That(decoded.AimX, Is.EqualTo(1.0f));
            Assert.That(decoded.AimY, Is.EqualTo(-1.0f));
            Assert.That(
                decoded.DashRequestSequence,
                Is.EqualTo(0u));
        }

        [Test]
        public void PlayerStateRecordExposesFireHeldState()
        {
            Type recordType =
                typeof(PlayerStateRecord);

            PropertyInfo fireHeldProperty =
                recordType.GetProperty(
                    "FireHeld");

            Assert.That(
                fireHeldProperty,
                Is.Not.Null,
                "PlayerStateRecord must expose FireHeld.");

            Assert.That(
                fireHeldProperty.PropertyType,
                Is.EqualTo(typeof(bool)));

            ConstructorInfo constructor =
                recordType.GetConstructor(
                    new[]
                    {
                typeof(uint),
                typeof(float),
                typeof(float),
                typeof(float),
                typeof(float),
                typeof(bool)
                    });

            Assert.That(
                constructor,
                Is.Not.Null,
                "PlayerStateRecord must accept FireHeld.");
        }

        [Test]
        public void PlayerInputPayloadExposesFireHeldFlag()
        {
            Type payloadType =
                typeof(PlayerInputPayload);

            PropertyInfo fireHeldProperty =
                payloadType.GetProperty(
                    "FireHeld");

            Assert.That(
                fireHeldProperty,
                Is.Not.Null,
                "PlayerInputPayload must expose FireHeld.");

            Assert.That(
                fireHeldProperty.PropertyType,
                Is.EqualTo(typeof(bool)));

            ConstructorInfo constructor =
                payloadType.GetConstructor(
                    new[]
                    {
                                    typeof(float),
                                    typeof(float),
                                    typeof(float),
                                    typeof(float),
                                    typeof(bool),
                                    typeof(uint)
                    });

            Assert.That(
                constructor,
                Is.Not.Null,
                "PlayerInputPayload must accept " +
                "FireHeld and DashRequestSequence.");
        }

        [TestCase(1.1f, 0.0f)]
        [TestCase(0.8f, 0.8f)]
        public void PlayerInputEncodeInvalidMovement_RejectsInput(
            float moveX,
            float moveY)
        {
            var input = new PlayerInputPayload(
                moveX,
                moveY,
                1.0f,
                0.0f);

            Assert.Throws<ArgumentException>(
                () => PlayerInputCodec.Encode(input));
        }

        [Test]
        public void PlayerInputEncodeNonFiniteValue_RejectsInput()
        {
            var infiniteAim = new PlayerInputPayload(
                0.0f,
                0.0f,
                float.PositiveInfinity,
                0.0f);

            var nanPosition = new PlayerInputPayload(
                float.NaN,
                0.0f,
                1.0f,
                0.0f);

            Assert.Throws<ArgumentException>(
                () =>
                    PlayerInputCodec.Encode(
                        infiniteAim));

            Assert.Throws<ArgumentException>(
                () =>
                    PlayerInputCodec.Encode(
                        nanPosition));
        }

        [Test]
        public void PlayerInputDecodeInvalidSize_RejectsPayload()
        {
            Assert.Throws<ArgumentException>(
                () =>
                    PlayerInputCodec.Decode(
                        new byte[PlayerInputCodec.PayloadSize - 1]));

            Assert.Throws<ArgumentException>(
                () =>
                    PlayerInputCodec.Decode(
                        new byte[PlayerInputCodec.PayloadSize + 1]));
        }

        [Test]
        public void PlayerInputDecodeUnknownFlags_RejectsPayload()
        {
            var payload = new byte[PlayerInputCodec.PayloadSize];

            payload[8] = 0x3F;
            payload[9] = 0x80;
            payload[19] = 0x02;

            Assert.Throws<ArgumentException>(
                () =>
                    PlayerInputCodec.Decode(
                        payload));
        }

        [Test]
        public void PlayerInputDecodeNonFiniteValue_RejectsPayload()
        {
            var payload = new byte[PlayerInputCodec.PayloadSize];

            // Aim X = positive infinity.
            payload[8] = 0x7F;
            payload[9] = 0x80;

            Assert.Throws<ArgumentException>(
                () =>
                    PlayerInputCodec.Decode(
                        payload));
        }

        [Test]
        public void PlayerInputNullArguments_AreRejected()
        {
            Assert.Throws<ArgumentNullException>(
                () =>
                    PlayerInputCodec.Encode(null));

            Assert.Throws<ArgumentNullException>(
                () =>
                    PlayerInputCodec.Decode(null));
        }

        [Test]
        public void PlayerStateEncodeThenDecode_MatchesCppWireFormat()
        {
            var original =
                new PlayerStateSnapshotPayload(
                    new[]
                    {
                        // 故意使用乱序 ID。
                        new PlayerStateRecord(
                            0x01020304u,
                            -3.5f,
                            4.25f,
                            -1.0f,
                            0.0f),

                        new PlayerStateRecord(
                            1u,
                            1.5f,
                            -2.25f,
                            0.0f,
                            1.0f)
                    });

            var expected = new byte[]
            {
                // Player count: 2
                0x00, 0x00, 0x00, 0x02,

                // Player ID: 1
                0x00, 0x00, 0x00, 0x01,
                // Position X: 1.5
                0x3F, 0xC0, 0x00, 0x00,
                // Position Y: -2.25
                0xC0, 0x10, 0x00, 0x00,
                // Aim X: 0
                0x00, 0x00, 0x00, 0x00,
                // Aim Y: 1
                0x3F, 0x80, 0x00, 0x00,
                // State flags: none
                0x00, 0x00, 0x00, 0x00,
                // Current health: 1
                0x00, 0x01,

                // Max health: 1
                0x00, 0x01,

                // Player ID: 0x01020304
                0x01, 0x02, 0x03, 0x04,
                // Position X: -3.5
                0xC0, 0x60, 0x00, 0x00,
                // Position Y: 4.25
                0x40, 0x88, 0x00, 0x00,
                // Aim X: -1
                0xBF, 0x80, 0x00, 0x00,
                // Aim Y: 0
                0x00, 0x00, 0x00, 0x00,
                // State flags: none
                0x00, 0x00, 0x00, 0x00,
                // Current health: 1
                0x00, 0x01,

                // Max health: 1
                0x00, 0x01,
            };

            byte[] encoded =
                PlayerStateSnapshotCodec.Encode(
                    original);

            Assert.That(encoded, Is.EqualTo(expected));

            PlayerStateSnapshotPayload decoded =
                PlayerStateSnapshotCodec.Decode(
                    encoded);

            Assert.That(
                decoded.Players.Count,
                Is.EqualTo(2));

            Assert.That(
                decoded.Players[0].PlayerId,
                Is.EqualTo(1u));

            Assert.That(
                decoded.Players[0].PositionX,
                Is.EqualTo(1.5f));

            Assert.That(
                decoded.Players[0].PositionY,
                Is.EqualTo(-2.25f));

            Assert.That(
                decoded.Players[0].AimX,
                Is.EqualTo(0.0f));

            Assert.That(
                decoded.Players[0].AimY,
                Is.EqualTo(1.0f));

            Assert.That(
                decoded.Players[1].PlayerId,
                Is.EqualTo(0x01020304u));

            Assert.That(
                decoded.Players[1].PositionX,
                Is.EqualTo(-3.5f));

            Assert.That(
                decoded.Players[1].PositionY,
                Is.EqualTo(4.25f));
        }

        [Test]
        public void PlayerStateRecordExposesHealthFields()
        {
            Type recordType =
                typeof(PlayerStateRecord);

            PropertyInfo currentHealthProperty =
                recordType.GetProperty("CurrentHealth");

            PropertyInfo maxHealthProperty =
                recordType.GetProperty("MaxHealth");

            Assert.That(
                currentHealthProperty,
                Is.Not.Null,
                "PlayerStateRecord must expose CurrentHealth.");

            Assert.That(
                maxHealthProperty,
                Is.Not.Null,
                "PlayerStateRecord must expose MaxHealth.");

            Assert.That(
                currentHealthProperty.PropertyType,
                Is.EqualTo(typeof(ushort)));

            Assert.That(
                maxHealthProperty.PropertyType,
                Is.EqualTo(typeof(ushort)));

            ConstructorInfo constructor =
                recordType.GetConstructor(
                    new[]
                    {
                typeof(uint),
                typeof(float),
                typeof(float),
                typeof(float),
                typeof(float),
                typeof(bool),
                typeof(bool),
                typeof(ushort),
                typeof(ushort)
                    });

            Assert.That(
                constructor,
                Is.Not.Null,
                "PlayerStateRecord must accept health fields.");
        }

        [Test]
        public void PlayerStateSnapshotRecordSize_IncludesHealthFields()
        {
            Assert.That(
                PlayerStateSnapshotCodec.RecordSize,
                Is.EqualTo(28));
        }

        [Test]
        public void PlayerStateHealthFields_UseNetworkByteOrder()
        {
            var snapshot =
                new PlayerStateSnapshotPayload(
                    new[]
                    {
                new PlayerStateRecord(
                    1u,
                    0.0f,
                    0.0f,
                    1.0f,
                    0.0f,
                    true,
                    false,
                    0x1234,
                    0x5678)
                    });

            byte[] encoded =
                PlayerStateSnapshotCodec.Encode(snapshot);

            Assert.That(encoded.Length, Is.EqualTo(32));

            Assert.That(encoded[28], Is.EqualTo(0x12));
            Assert.That(encoded[29], Is.EqualTo(0x34));
            Assert.That(encoded[30], Is.EqualTo(0x56));
            Assert.That(encoded[31], Is.EqualTo(0x78));

            PlayerStateSnapshotPayload decoded =
                PlayerStateSnapshotCodec.Decode(encoded);

            Assert.That(
                decoded.Players[0].CurrentHealth,
                Is.EqualTo((ushort)0x1234));

            Assert.That(
                decoded.Players[0].MaxHealth,
                Is.EqualTo((ushort)0x5678));
        }

        [Test]
        public void PlayerStateDecodeMalformedSize_RejectsPayload()
        {
            var snapshot =
                new PlayerStateSnapshotPayload(
                    new[]
                    {
                        new PlayerStateRecord(
                            1u,
                            0.0f,
                            0.0f,
                            1.0f,
                            0.0f)
                    });

            byte[] encoded =
                PlayerStateSnapshotCodec.Encode(
                    snapshot);

            var truncated =
                new byte[encoded.Length - 1];

            Array.Copy(
                encoded,
                truncated,
                truncated.Length);

            var trailing =
                new byte[encoded.Length + 1];

            Array.Copy(
                encoded,
                trailing,
                encoded.Length);

            Assert.Throws<ArgumentException>(
                () =>
                    PlayerStateSnapshotCodec.Decode(
                        truncated));

            Assert.Throws<ArgumentException>(
                () =>
                    PlayerStateSnapshotCodec.Decode(
                        trailing));
        }

        [Test]
        public void PlayerStateDecodeUnknownFlags_RejectsPayload()
        {
            var payload = new byte[32];

            // Count = 1
            payload[3] = 1;

            // Player ID = 1
            payload[7] = 1;

            // Aim X = 1.0
            payload[16] = 0x3F;
            payload[17] = 0x80;

            // Unknown flags: bit 2 is not defined
            payload[27] = 4;

            Assert.Throws<ArgumentException>(
                () =>
                    PlayerStateSnapshotCodec.Decode(
                        payload));
        }

        [Test]
        public void PlayerStateNullArguments_AreRejected()
        {
            Assert.Throws<ArgumentNullException>(
                () =>
                    PlayerStateSnapshotCodec.Encode(
                        null));

            Assert.Throws<ArgumentNullException>(
                () =>
                    PlayerStateSnapshotCodec.Decode(
                        null));
        }

        [Test]
        public void PlayerStateEncodeInvalidRecords_RejectsSnapshot()
        {
            var zeroId =
                new PlayerStateSnapshotPayload(
                    new[]
                    {
                        new PlayerStateRecord(
                            0u,
                            0.0f,
                            0.0f,
                            1.0f,
                            0.0f)
                    });

            var duplicateIds =
                new PlayerStateSnapshotPayload(
                    new[]
                    {
                        new PlayerStateRecord(
                            1u,
                            0.0f,
                            0.0f,
                            1.0f,
                            0.0f),

                        new PlayerStateRecord(
                            1u,
                            1.0f,
                            2.0f,
                            0.0f,
                            1.0f)
                    });

            var nullRecord =
                new PlayerStateSnapshotPayload(
                    new PlayerStateRecord[]
                    {
                        null
                    });

            var infinitePosition =
                new PlayerStateSnapshotPayload(
                    new[]
                    {
                        new PlayerStateRecord(
                            1u,
                            float.PositiveInfinity,
                            0.0f,
                            1.0f,
                            0.0f)
                    });

            var nanAim =
                new PlayerStateSnapshotPayload(
                    new[]
                    {
                        new PlayerStateRecord(
                            1u,
                            0.0f,
                            0.0f,
                            float.NaN,
                            0.0f)
                    });

            Assert.Throws<ArgumentException>(
                () =>
                    PlayerStateSnapshotCodec.Encode(
                        zeroId));

            Assert.Throws<ArgumentException>(
                () =>
                    PlayerStateSnapshotCodec.Encode(
                        duplicateIds));

            Assert.Throws<ArgumentException>(
                () =>
                    PlayerStateSnapshotCodec.Encode(
                        nullRecord));

            Assert.Throws<ArgumentException>(
                () =>
                    PlayerStateSnapshotCodec.Encode(
                        infinitePosition));

            Assert.Throws<ArgumentException>(
                () =>
                    PlayerStateSnapshotCodec.Encode(
                        nanAim));
        }

        [Test]
        public void PlayerStateDecodeInvalidRecords_RejectsPayload()
        {
            var valid =
                new PlayerStateSnapshotPayload(
                    new[]
                    {
                        new PlayerStateRecord(
                            1u,
                            0.0f,
                            0.0f,
                            1.0f,
                            0.0f),

                        new PlayerStateRecord(
                            2u,
                            1.0f,
                            2.0f,
                            0.0f,
                            1.0f)
                    });

            byte[] encoded =
                PlayerStateSnapshotCodec.Encode(
                    valid);

            var zeroId =
                (byte[])encoded.Clone();

            zeroId[4] = 0x00;
            zeroId[5] = 0x00;
            zeroId[6] = 0x00;
            zeroId[7] = 0x00;

            Assert.Throws<ArgumentException>(
                () =>
                    PlayerStateSnapshotCodec.Decode(
                        zeroId));

            var duplicateId =
                (byte[])encoded.Clone();

            duplicateId[32] = 0x00;
            duplicateId[33] = 0x00;
            duplicateId[34] = 0x00;
            duplicateId[35] = 0x01;

            Assert.Throws<ArgumentException>(
                () =>
                    PlayerStateSnapshotCodec.Decode(
                        duplicateId));

            var descendingIds =
                (byte[])encoded.Clone();

            descendingIds[4] = 0x00;
            descendingIds[5] = 0x00;
            descendingIds[6] = 0x00;
            descendingIds[7] = 0x02;

            descendingIds[32] = 0x00;
            descendingIds[33] = 0x00;
            descendingIds[34] = 0x00;
            descendingIds[35] = 0x01;

            Assert.Throws<ArgumentException>(
                () =>
                    PlayerStateSnapshotCodec.Decode(
                        descendingIds));

            var infinitePosition =
                (byte[])encoded.Clone();

            // 第一条记录 Position X = +Infinity
            infinitePosition[8] = 0x7F;
            infinitePosition[9] = 0x80;
            infinitePosition[10] = 0x00;
            infinitePosition[11] = 0x00;

            Assert.Throws<ArgumentException>(
                () =>
                    PlayerStateSnapshotCodec.Decode(
                        infinitePosition));

            var nanAim =
                (byte[])encoded.Clone();

            // 第一条记录 Aim X = NaN
            nanAim[16] = 0x7F;
            nanAim[17] = 0xC0;
            nanAim[18] = 0x00;
            nanAim[19] = 0x00;

            Assert.Throws<ArgumentException>(
                () =>
                    PlayerStateSnapshotCodec.Decode(
                        nanAim));
        }

        [Test]
        public void PlayerStateEncodeInvalidHealth_RejectsSnapshot()
        {
            var currentHealthAboveMax =
                new PlayerStateSnapshotPayload(
                    new[]
                    {
                new PlayerStateRecord(
                    1u,
                    0.0f,
                    0.0f,
                    1.0f,
                    0.0f,
                    false,
                    false,
                    101,
                    100)
                    });

            var zeroMaxHealth =
                new PlayerStateSnapshotPayload(
                    new[]
                    {
                new PlayerStateRecord(
                    1u,
                    0.0f,
                    0.0f,
                    1.0f,
                    0.0f,
                    false,
                    false,
                    0,
                    0)
                    });

            Assert.Throws<ArgumentException>(
                () =>
                    PlayerStateSnapshotCodec.Encode(
                        currentHealthAboveMax));

            Assert.Throws<ArgumentException>(
                () =>
                    PlayerStateSnapshotCodec.Encode(
                        zeroMaxHealth));
        }

        [Test]
        public void PlayerStateDecodeInvalidHealth_RejectsPayload()
        {
            var valid =
                new PlayerStateSnapshotPayload(
                    new[]
                    {
                new PlayerStateRecord(
                    1u,
                    0.0f,
                    0.0f,
                    1.0f,
                    0.0f,
                    false,
                    false,
                    100,
                    100)
                    });

            byte[] currentHealthAboveMax =
                PlayerStateSnapshotCodec.Encode(valid);

            currentHealthAboveMax[28] = 0x00;
            currentHealthAboveMax[29] = 0x65;

            currentHealthAboveMax[30] = 0x00;
            currentHealthAboveMax[31] = 0x64;

            byte[] zeroMaxHealth =
                PlayerStateSnapshotCodec.Encode(valid);

            zeroMaxHealth[30] = 0x00;
            zeroMaxHealth[31] = 0x00;

            Assert.Throws<ArgumentException>(
                () =>
                    PlayerStateSnapshotCodec.Decode(
                        currentHealthAboveMax));

            Assert.Throws<ArgumentException>(
                () =>
                    PlayerStateSnapshotCodec.Decode(
                        zeroMaxHealth));
        }

        [Test]
        public void PlayerStateDecodeZeroCurrentHealth_IsAcceptedAsDead()
        {
            var snapshot =
                new PlayerStateSnapshotPayload(
                    new[]
                    {
                new PlayerStateRecord(
                    1u,
                    0.0f,
                    0.0f,
                    1.0f,
                    0.0f,
                    false,
                    false,
                    0,
                    100)
                    });

            byte[] encoded =
                PlayerStateSnapshotCodec.Encode(snapshot);

            PlayerStateSnapshotPayload decoded =
                PlayerStateSnapshotCodec.Decode(encoded);

            Assert.That(
                decoded.Players[0].CurrentHealth,
                Is.EqualTo((ushort)0));

            Assert.That(
                decoded.Players[0].MaxHealth,
                Is.EqualTo((ushort)100));
        }
    }
}