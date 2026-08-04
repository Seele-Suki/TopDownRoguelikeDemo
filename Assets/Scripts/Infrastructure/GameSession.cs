namespace TopDownRoguelike.Infrastructure
{
    public static class GameSession
    {
        public static GameMode CurrentMode { get; private set; } =
            GameMode.SinglePlayer;

        public static bool IsMultiplayer =>
            CurrentMode == GameMode.MultiplayerHost ||
            CurrentMode == GameMode.MultiplayerClient;

        public static bool IsHost =>
            CurrentMode == GameMode.MultiplayerHost;

        public static bool IsClient =>
            CurrentMode == GameMode.MultiplayerClient;

        public static void ConfigureSinglePlayer()
        {
            CurrentMode = GameMode.SinglePlayer;
        }

        public static void ConfigureMultiplayerHost()
        {
            CurrentMode = GameMode.MultiplayerHost;
        }

        public static void ConfigureMultiplayerClient()
        {
            CurrentMode = GameMode.MultiplayerClient;
        }

        public static void Reset()
        {
            CurrentMode = GameMode.SinglePlayer;
        }
    }
}