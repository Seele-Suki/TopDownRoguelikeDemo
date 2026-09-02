using TopDownRoguelike.Gameplay.Characters;
using TopDownRoguelike.Networking.Gameplay;
using UnityEngine;

namespace TopDownRoguelike.Gameplay.Networking
{
    public static class NetworkCombatTargetSelector
    {
        public static bool TrySelectNearest(
            NetworkPlayerRegistry registry,
            Vector2 origin,
            out uint playerId,
            out Transform target)
        {
            playerId = 0u;
            target = null;
            if (registry == null)
                return false;

            float bestDistance = float.PositiveInfinity;
            foreach (var entry in registry.EnumeratePlayers())
            {
                GameObject player = entry.Value;
                if (player == null ||
                    !player.activeInHierarchy ||
                    (player.TryGetComponent(out PlayerHealth health) &&
                     health.IsDead))
                    continue;

                float distance = ((Vector2)player.transform.position - origin).sqrMagnitude;
                if (target == null || distance < bestDistance ||
                    (Mathf.Approximately(distance, bestDistance) && entry.Key < playerId))
                {
                    bestDistance = distance;
                    playerId = entry.Key;
                    target = player.transform;
                }
            }

            return target != null;
        }
    }
}
