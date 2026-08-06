using NUnit.Framework;
using TopDownRoguelike.Infrastructure;
using Assert = NUnit.Framework.Assert;

namespace TopDownRoguelike.Tests.EditMode
{
    public sealed class GameSessionTests
    {
        [SetUp]
        public void SetUp()
        {
            GameSession.Reset();
        }

        [TearDown]
        public void TearDown()
        {
            GameSession.Reset();
        }

        [Test]
        public void Reset_RestoresSinglePlayerDefaults()
        {
            GameSession.ConfigureMultiplayerHost();
            GameSession.SelectCharacter(CharacterId.Ranged);
            GameSession.SelectDifficulty(DifficultyId.Normal);

            GameSession.Reset();

            Assert.That(
                GameSession.CurrentMode,
                Is.EqualTo(GameMode.SinglePlayer));
            Assert.That(
                GameSession.SelectedCharacter,
                Is.EqualTo(CharacterId.None));
            Assert.That(
                GameSession.SelectedDifficulty,
                Is.EqualTo(DifficultyId.None));
            Assert.That(GameSession.HasCompleteSelection, Is.False);
        }

        [Test]
        public void SelectingOnlyCharacter_IsNotComplete()
        {
            GameSession.ConfigureSinglePlayer();

            GameSession.SelectCharacter(CharacterId.Ranged);

            Assert.That(
                GameSession.SelectedCharacter,
                Is.EqualTo(CharacterId.Ranged));
            Assert.That(GameSession.HasCompleteSelection, Is.False);
        }

        [Test]
        public void SelectingCharacterAndDifficulty_CompletesSelection()
        {
            GameSession.ConfigureSinglePlayer();

            GameSession.SelectCharacter(CharacterId.Ranged);
            GameSession.SelectDifficulty(DifficultyId.Normal);

            Assert.That(
                GameSession.SelectedCharacter,
                Is.EqualTo(CharacterId.Ranged));
            Assert.That(
                GameSession.SelectedDifficulty,
                Is.EqualTo(DifficultyId.Normal));
            Assert.That(GameSession.HasCompleteSelection, Is.True);
        }

        [Test]
        public void ConfigureSinglePlayer_ClearsPreviousSelection()
        {
            GameSession.ConfigureMultiplayerHost();
            GameSession.SelectCharacter(CharacterId.Ranged);
            GameSession.SelectDifficulty(DifficultyId.Normal);

            GameSession.ConfigureSinglePlayer();

            Assert.That(
                GameSession.CurrentMode,
                Is.EqualTo(GameMode.SinglePlayer));
            Assert.That(
                GameSession.SelectedCharacter,
                Is.EqualTo(CharacterId.None));
            Assert.That(
                GameSession.SelectedDifficulty,
                Is.EqualTo(DifficultyId.None));
            Assert.That(GameSession.HasCompleteSelection, Is.False);
        }
    }
}