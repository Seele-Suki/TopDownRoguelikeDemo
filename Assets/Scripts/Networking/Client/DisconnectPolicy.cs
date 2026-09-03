using TopDownRoguelike.Networking.Room;

namespace TopDownRoguelike.Networking.Client
{
    public enum DisconnectAction : byte
    {
        None = 0,
        ReturnToMultiplayerMenu = 1,
        ShowHostDisconnectedDialog = 2,
        ShowClientDisconnectedDialog = 3
    }

    public readonly struct DisconnectContext
    {
        public DisconnectContext(
            RoomRole role,
            bool isInGameplay,
            DisconnectReason reason,
            bool isLocalInitiated = false)
        {
            Role = role;
            IsInGameplay = isInGameplay;
            Reason = reason;
            IsLocalInitiated = isLocalInitiated;
        }

        public RoomRole Role { get; }

        public bool IsInGameplay { get; }

        public DisconnectReason Reason { get; }

        public bool IsLocalInitiated { get; }
    }

    public static class DisconnectPolicy
    {
        public static DisconnectAction Resolve(
            DisconnectContext context)
        {
            if (context.Role == RoomRole.None ||
                context.Reason == DisconnectReason.None)
            {
                return DisconnectAction.None;
            }

            if (context.Reason == DisconnectReason.ApplicationQuit)
            {
                return DisconnectAction.None;
            }

            if (context.Reason == DisconnectReason.RemotePeerLeft &&
                context.Role == RoomRole.Client &&
                !context.IsLocalInitiated)
            {
                return DisconnectAction.ShowHostDisconnectedDialog;
            }

            if (context.Reason == DisconnectReason.RemotePeerLeft &&
                context.Role == RoomRole.Host &&
                context.IsInGameplay &&
                !context.IsLocalInitiated)
            {
                return DisconnectAction.ShowClientDisconnectedDialog;
            }

            return DisconnectAction.ReturnToMultiplayerMenu;
        }
    }
}
