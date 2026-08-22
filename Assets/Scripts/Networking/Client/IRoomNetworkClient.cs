using System;

namespace TopDownRoguelike.Networking.Client
{
    public interface IRoomNetworkClient
    {
        event Action<NetworkClientState>
            StateChanged;

        NetworkClientState State
        {
            get;
        }

        string LastError
        {
            get;
        }

        void Connect(
            string address,
            int port);

        void CreateRoom(
            string nickname);

        void JoinRoom(
            string nickname,
            string roomId);

        void Disconnect();
    }
}