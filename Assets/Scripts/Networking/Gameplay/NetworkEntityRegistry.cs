using System.Collections.Generic;

namespace TopDownRoguelike.Networking.Gameplay
{
    public sealed class NetworkEntityRegistry
    {
        private readonly
            Dictionary<uint, NetworkEntityId>
            entities =
                new Dictionary<uint, NetworkEntityId>();

        public int Count =>
            entities.Count;

        public bool TryRegister(
            NetworkEntityId entity)
        {
            if (entity == null ||
                !entity.IsAssigned ||
                entities.ContainsKey(
                    entity.EntityId))
            {
                return false;
            }

            entities.Add(
                entity.EntityId,
                entity);

            return true;
        }

        public bool TryGet(
            uint entityId,
            out NetworkEntityId entity)
        {
            return entities.TryGetValue(
                entityId,
                out entity);
        }

        public IEnumerable<NetworkEntityId>
            EnumerateEntities()
        {
            foreach (KeyValuePair<uint, NetworkEntityId>
                entry in entities)
            {
                if (entry.Value != null)
                {
                    yield return entry.Value;
                }
            }
        }

        public bool Remove(
            uint entityId)
        {
            return entities.Remove(
                entityId);
        }

        public void Clear()
        {
            entities.Clear();
        }
    }
}