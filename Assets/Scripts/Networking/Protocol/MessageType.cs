namespace TopDownRoguelike.Networking.Protocol
{
    public enum MessageType : ushort
    {
        Invalid = 0,

        ClientHello = 1,
        ServerHello = 2,
        SetNickname = 3,

        CreateRoomRequest = 10,
        CreateRoomResponse = 11,
        JoinRoomRequest = 12,
        JoinRoomResponse = 13,
        RoomStateSnapshot = 14,
        SetPlayerSelection = 15,
        SetReady = 16,
        StartGameRequest = 17,
        GameStarted = 18,
        LeaveRoom = 19,
        ErrorMessage = 20,

        UdpBindRequest = 30,
        UdpBindAccepted = 31,
        UdpPing = 32,
        UdpPong = 33,

        PlayerInput = 34,
        PlayerStateSnapshot = 35
    }
}