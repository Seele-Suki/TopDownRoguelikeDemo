using System;
using NUnit.Framework;
using TopDownRoguelike.Infrastructure;
using TopDownRoguelike.Networking.Protocol;

namespace TopDownRoguelike.Tests.EditMode
{
    public sealed class RoomStateSnapshotCodecTests
    {
        [Test]
        public void EncodeThenDecode_MatchesCppWireFormat()
        {
            var snapshot = new RoomStateSnapshot(
                "ROOM-7",
                RoomStateStatus.Waiting,
                DifficultyId.Hard,
                new[]
                {
                    new RoomPlayerSnapshot(
                        0x01020304u,
                        true,
                        true,
                        CharacterId.Ranged,
                        "Host"),

                    new RoomPlayerSnapshot(
                        0xA0B0C0D0u,
                        false,
                        false,
                        CharacterId.Melee,
                        "Guest")
                });

            var expected = new byte[]
            {
                0x00, 0x06,
                0x52, 0x4F, 0x4F, 0x4D, 0x2D, 0x37,
                0x00,
                0x02,
                0x02,

                0x01, 0x02, 0x03, 0x04,
                0x03,
                0x01,
                0x00, 0x04,
                0x48, 0x6F, 0x73, 0x74,

                0xA0, 0xB0, 0xC0, 0xD0,
                0x00,
                0x02,
                0x00, 0x05,
                0x47, 0x75, 0x65, 0x73, 0x74
            };

            byte[] encoded =
                RoomStateSnapshotCodec.Encode(snapshot);

            Assert.That(encoded, Is.EqualTo(expected));

            RoomStateSnapshot decoded =
                RoomStateSnapshotCodec.Decode(encoded);

            Assert.That(decoded.RoomId, Is.EqualTo("ROOM-7"));
            Assert.That(
                decoded.Status,
                Is.EqualTo(RoomStateStatus.Waiting));
            Assert.That(
                decoded.SelectedDifficulty,
                Is.EqualTo(DifficultyId.Hard));
            Assert.That(decoded.Players.Count, Is.EqualTo(2));

            Assert.That(
                decoded.Players[0].PlayerId,
                Is.EqualTo(0x01020304u));
            Assert.That(decoded.Players[0].IsHost, Is.True);
            Assert.That(decoded.Players[0].IsReady, Is.True);
            Assert.That(
                decoded.Players[0].Character,
                Is.EqualTo(CharacterId.Ranged));
            Assert.That(
                decoded.Players[0].Nickname,
                Is.EqualTo("Host"));

            Assert.That(
                decoded.Players[1].PlayerId,
                Is.EqualTo(0xA0B0C0D0u));
            Assert.That(decoded.Players[1].IsHost, Is.False);
            Assert.That(decoded.Players[1].IsReady, Is.False);
            Assert.That(
                decoded.Players[1].Character,
                Is.EqualTo(CharacterId.Melee));
            Assert.That(
                decoded.Players[1].Nickname,
                Is.EqualTo("Guest"));
        }

        [Test]
        public void DecodeUnknownPlayerFlags_RejectsPayload()
        {
            var payload = new byte[]
            {
                0x00, 0x01,
                0x52,

                0x00,
                0x00,
                0x01,

                0x00, 0x00, 0x00, 0x01,
                0x04,
                0x01,
                0x00, 0x01,
                0x48
            };

            Assert.Throws<ArgumentException>(
                () =>
                    RoomStateSnapshotCodec.Decode(
                        payload));
        }

        [TestCase(3, 0x02)]
        [TestCase(4, 0x04)]
        [TestCase(5, 0x00)]
        public void DecodeInvalidRoomField_RejectsPayload(
            int fieldOffset,
            byte invalidValue)
        {
            byte[] payload =
                CreateSingleHostPayload();

            payload[fieldOffset] =
                invalidValue;

            Assert.Throws<ArgumentException>(
                () =>
                    RoomStateSnapshotCodec.Decode(
                        payload));
        }

        private static RoomStateSnapshot
            CreateSnapshot(
                params RoomPlayerSnapshot[] players)
        {
            return new RoomStateSnapshot(
                "ROOM-1",
                RoomStateStatus.Waiting,
                DifficultyId.Normal,
                players);
        }

        private static byte[]
            CreateSingleHostPayload()
        {
            return new byte[]
            {
                0x00, 0x01,
                0x52,

                0x00,
                0x00,
                0x01,

                0x00, 0x00, 0x00, 0x01,
                0x01,
                0x01,
                0x00, 0x01,
                0x48
            };
        }

        [Test]
        public void EncodeZeroPlayerId_RejectsSnapshot()
        {
            RoomStateSnapshot snapshot =
                CreateSnapshot(
                    new RoomPlayerSnapshot(
                        0u,
                        true,
                        false,
                        CharacterId.Ranged,
                        "Host"));

            Assert.Throws<ArgumentException>(
                () =>
                    RoomStateSnapshotCodec.Encode(
                        snapshot));
        }

        [Test]
        public void EncodeInvalidCharacter_RejectsSnapshot()
        {
            RoomStateSnapshot snapshot =
                CreateSnapshot(
                    new RoomPlayerSnapshot(
                        1u,
                        true,
                        false,
                        (CharacterId)3,
                        "Host"));

            Assert.Throws<ArgumentException>(
                () =>
                    RoomStateSnapshotCodec.Encode(
                        snapshot));
        }

        [Test]
        public void EncodeDuplicatePlayerIds_RejectsSnapshot()
        {
            RoomStateSnapshot snapshot =
                CreateSnapshot(
                    new RoomPlayerSnapshot(
                        1u,
                        true,
                        false,
                        CharacterId.Ranged,
                        "Host"),

                    new RoomPlayerSnapshot(
                        1u,
                        false,
                        false,
                        CharacterId.Melee,
                        "Guest"));

            Assert.Throws<ArgumentException>(
                () =>
                    RoomStateSnapshotCodec.Encode(
                        snapshot));
        }

        [TestCase(false, false)]
        [TestCase(true, true)]
        public void EncodeInvalidHostCount_RejectsSnapshot(
            bool firstIsHost,
            bool secondIsHost)
        {
            RoomStateSnapshot snapshot =
                CreateSnapshot(
                    new RoomPlayerSnapshot(
                        1u,
                        firstIsHost,
                        false,
                        CharacterId.Ranged,
                        "First"),

                    new RoomPlayerSnapshot(
                        2u,
                        secondIsHost,
                        false,
                        CharacterId.Melee,
                        "Second"));

            Assert.Throws<ArgumentException>(
                () =>
                    RoomStateSnapshotCodec.Encode(
                        snapshot));
        }

        [Test]
        public void DecodeTruncatedPayload_RejectsPayload()
        {
            byte[] completePayload =
                CreateSingleHostPayload();

            var truncatedPayload =
                new byte[completePayload.Length - 1];

            Array.Copy(
                completePayload,
                truncatedPayload,
                truncatedPayload.Length);

            Assert.Throws<ArgumentException>(
                () =>
                    RoomStateSnapshotCodec.Decode(
                        truncatedPayload));
        }

        [Test]
        public void DecodeTrailingByte_RejectsPayload()
        {
            byte[] completePayload =
                CreateSingleHostPayload();

            var payloadWithTrailingByte =
                new byte[completePayload.Length + 1];

            Array.Copy(
                completePayload,
                payloadWithTrailingByte,
                completePayload.Length);

            payloadWithTrailingByte[
                payloadWithTrailingByte.Length - 1] =
                0xFF;

            Assert.Throws<ArgumentException>(
                () =>
                    RoomStateSnapshotCodec.Decode(
                        payloadWithTrailingByte));
        }

        [Test]
        public void EncodeThenDecode_Utf8TextIsPreserved()
        {
            RoomStateSnapshot snapshot =
                CreateSnapshot(
                    new RoomPlayerSnapshot(
                        1u,
                        true,
                        false,
                        CharacterId.Ranged,
                        "\u5E0C\u513F"));

            byte[] encoded =
                RoomStateSnapshotCodec.Encode(
                    snapshot);

            RoomStateSnapshot decoded =
                RoomStateSnapshotCodec.Decode(
                    encoded);

            Assert.That(
                decoded.Players[0].Nickname,
                Is.EqualTo("\u5E0C\u513F"));
        }

        [Test]
        public void DecodeInvalidUtf8_RejectsPayload()
        {
            byte[] payload =
                CreateSingleHostPayload();

            payload[2] = 0xFF;

            Assert.Throws<ArgumentException>(
                () =>
                    RoomStateSnapshotCodec.Decode(
                        payload));
        }
    }
}