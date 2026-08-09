using NUnit.Framework;
using TopDownRoguelike.Infrastructure;
using TopDownRoguelike.Networking.Room;

namespace TopDownRoguelike.Tests.EditMode
{
    public sealed class RoomStateTests
    {
        private RoomState room;

        [SetUp]
        public void SetUp()
        {
            room = new RoomState();
        }

        [Test]
        public void NewRoom_HasFourEmptySlots()
        {
            Assert.That(
                room.Players.Count,
                Is.EqualTo(RoomState.MaxPlayerCount));
            Assert.That(room.PlayerCount, Is.EqualTo(0));
            Assert.That(
                room.SelectedDifficulty,
                Is.EqualTo(DifficultyId.None));
            Assert.That(room.CanStartGame, Is.False);
        }

        [Test]
        public void TwoReadyPlayers_CanStartGame()
        {
            Assert.That(
                room.TryAddPlayer(
                    1,
                    "Host",
                    RoomRole.Host),
                Is.True);

            Assert.That(
                room.TryAddPlayer(
                    2,
                    "Client",
                    RoomRole.Client),
                Is.True);

            Assert.That(
                room.TrySelectDifficulty(
                    1,
                    DifficultyId.Normal),
                Is.True);

            Assert.That(
                room.TrySelectCharacter(
                    1,
                    CharacterId.Ranged),
                Is.True);

            Assert.That(
                room.TrySelectCharacter(
                    2,
                    CharacterId.Ranged),
                Is.True);

            Assert.That(room.TrySetReady(1, true), Is.True);
            Assert.That(room.TrySetReady(2, true), Is.True);
            Assert.That(room.CanStartGame, Is.True);
        }

        [Test]
        public void Client_CannotSelectDifficulty()
        {
            room.TryAddPlayer(
                1,
                "Host",
                RoomRole.Host);

            room.TryAddPlayer(
                2,
                "Client",
                RoomRole.Client);

            bool result = room.TrySelectDifficulty(
                2,
                DifficultyId.Normal);

            Assert.That(result, Is.False);
            Assert.That(
                room.SelectedDifficulty,
                Is.EqualTo(DifficultyId.None));
        }

        [Test]
        public void PlayerWithoutCharacter_CannotBecomeReady()
        {
            room.TryAddPlayer(
                1,
                "Host",
                RoomRole.Host);

            bool result = room.TrySetReady(1, true);

            Assert.That(result, Is.False);
            Assert.That(
                room.GetPlayer(1).IsReady,
                Is.False);
        }

        [Test]
        public void RemovingHost_ResetsWholeRoom()
        {
            room.TryAddPlayer(
                1,
                "Host",
                RoomRole.Host);

            room.TryAddPlayer(
                2,
                "Client",
                RoomRole.Client);

            room.TrySelectDifficulty(
                1,
                DifficultyId.Normal);

            bool result = room.RemovePlayer(1);

            Assert.That(result, Is.True);
            Assert.That(room.PlayerCount, Is.EqualTo(0));
            Assert.That(
                room.SelectedDifficulty,
                Is.EqualTo(DifficultyId.None));
            Assert.That(room.CanStartGame, Is.False);
        }
    }
}