using System;
using TopDownRoguelike.Networking.Protocol;

namespace TopDownRoguelike.Networking.Client
{
    public interface IRoomNetworkClient
    {
        event Action<NetworkClientState>
            StateChanged;

        event Action<RoomStateSnapshot>
            RoomStateChanged;

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

        void Disconnect();
    }
}