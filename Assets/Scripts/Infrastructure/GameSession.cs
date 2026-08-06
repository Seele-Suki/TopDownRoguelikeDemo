namespace TopDownRoguelike.Infrastructure
{
    public static class GameSession
    {
        public static GameMode CurrentMode { get; private set; } =
            GameMode.SinglePlayer;

        public static CharacterId SelectedCharacter { get; private set; } =
            CharacterId.None;

        public static DifficultyId SelectedDifficulty { get; private set; } =
            DifficultyId.None;

        public static bool HasCompleteSelection =>
            SelectedCharacter != CharacterId.None &&
            SelectedDifficulty != DifficultyId.None;

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
            ClearSelection();
        }

        public static void ConfigureMultiplayerHost()
        {
            CurrentMode = GameMode.MultiplayerHost;
            ClearSelection();
        }

        public static void ConfigureMultiplayerClient()
        {
            CurrentMode = GameMode.MultiplayerClient;
            ClearSelection();
        }

        public static void SelectCharacter(CharacterId character)
        {
            SelectedCharacter = character;
        }

        public static void SelectDifficulty(DifficultyId difficulty)
        {
            SelectedDifficulty = difficulty;
        }

        public static void ClearSelection()
        {
            SelectedCharacter = CharacterId.None;
            SelectedDifficulty = DifficultyId.None;
        }

        public static void Reset()
        {
            CurrentMode = GameMode.SinglePlayer;
            ClearSelection();
        }
    }
}