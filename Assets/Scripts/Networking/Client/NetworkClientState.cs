namespace TopDownRoguelike.Networking.Client
{
    public enum NetworkClientState
    {
        Disconnected,
        ConnectingTcp,
        WaitingForServerHello,
        ConnectingUdp,
        BindingUdp,
        Connected,
        CreatingRoom,
        JoiningRoom,
        InRoom,
        Error
    }
}