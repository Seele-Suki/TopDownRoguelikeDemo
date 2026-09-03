namespace TopDownRoguelike.Networking.Client
{
    public enum DisconnectReason : byte
    {
        None = 0,
        LocalLeaveRoom = 1,
        RemotePeerLeft = 2,
        ServerClosed = 3,
        TransportError = 4,
        HeartbeatTimeout = 5,
        ApplicationQuit = 6,
        Unknown = 255
    }
}
