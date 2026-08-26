using System;
using TopDownRoguelike.Networking.Protocol;
using TopDownRoguelike.Infrastructure;

namespace TopDownRoguelike.Networking.Client
{
    public interface IRoomNetworkClient
    {
        event Action<NetworkClientState>
            StateChanged;

        event Action<RoomStateSnapshot>
            RoomStateChanged;

        event Action<string>
            ErrorReceived;

        event Action GameStarted;

        NetworkClientState State
        {
            get;
        }

        string LastError
        {
            get;
        }

        uint PlayerId
        {
            get;
        }

        string CurrentRoomId
        {
            get;
        }

        RoomStateSnapshot CurrentRoomState
        {
            get;
        }

        void Connect(
            string address,
            int port);

        void CreateRoom(
            string nickname);

        void JoinRoom(
            string nickname);

        void SetPlayerSelection(
            CharacterId character,
            DifficultyId difficulty);

        void SetReady(
            bool ready);

        void StartGame();

        void LeaveRoom();

        void Disconnect();
    }
}