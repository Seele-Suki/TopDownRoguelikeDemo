using NUnit.Framework;
using TopDownRoguelike.Infrastructure;

namespace TopDownRoguelike.Tests.EditMode
{
    public sealed class GameSessionAuthorityTests
    {
        [TearDown]
        public void TearDown()
        {
            GameSession.Reset();
        }

        [Test]
        public void SinglePlayer_IsGameplayAuthority()
        {
            GameSession.ConfigureSinglePlayer();
            Assert.That(GameSession.IsGameplayAuthority, Is.True);
        }

        [Test]
        public void MultiplayerClient_IsNotGameplayAuthority()
        {
            GameSession.ConfigureMultiplayerClient();
            Assert.That(GameSession.IsGameplayAuthority, Is.False);
        }
    }
}
