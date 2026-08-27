using System.Collections.Generic;
using UnityEngine;

namespace TopDownRoguelike.Networking.Gameplay
{
    public sealed class NetworkPlayerRegistry
    {
        private readonly Dictionary<uint, GameObject>
            players =
                new Dictionary<uint, GameObject>();

        public int Count =>
            players.Count;

        public bool TryRegister(
            uint playerId,
            GameObject player)
        {
            if (playerId == 0u ||
                player == null ||
                players.ContainsKey(playerId))
            {
                return false;
            }

            players.Add(
                playerId,
                player);

            return true;
        }

        public bool TryGetPlayer(
            uint playerId,
            out GameObject player)
        {
            return players.TryGetValue(
                playerId,
                out player);
        }

        public bool Remove(
            uint playerId)
        {
            return players.Remove(playerId);
        }

        public void Clear()
        {
            players.Clear();
        }
    }
}