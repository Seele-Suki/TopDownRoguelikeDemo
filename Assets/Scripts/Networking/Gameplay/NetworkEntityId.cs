using TopDownRoguelike.Networking.Protocol;
using UnityEngine;

namespace TopDownRoguelike.Networking.Gameplay
{
    public sealed class NetworkEntityId : MonoBehaviour
    {
        private uint entityId;

        private NetworkEntityType entityType =
            NetworkEntityType.Invalid;

        public uint EntityId =>
            entityId;

        public NetworkEntityType EntityType =>
            entityType;

        public bool IsAssigned =>
            entityId != 0u;

        public bool TryAssign(
            uint newEntityId)
        {
            if (newEntityId == 0u ||
                IsAssigned)
            {
                return false;
            }

            entityId =
                newEntityId;

            return true;
        }

        public bool TryAssign(
            uint newEntityId,
            NetworkEntityType newEntityType)
        {
            if (newEntityType ==
                NetworkEntityType.Invalid)
            {
                return false;
            }

            if (!TryAssign(newEntityId))
            {
                return false;
            }

            entityType =
                newEntityType;

            return true;
        }

        public void Clear()
        {
            entityId =
                0u;

            entityType =
                NetworkEntityType.Invalid;
        }
    }
}