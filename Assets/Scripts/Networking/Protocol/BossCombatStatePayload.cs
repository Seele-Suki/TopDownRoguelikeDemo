using System;

namespace TopDownRoguelike.Networking.Protocol
{
    public enum BossCombatState : byte { Started = 1, Paused = 2, Resumed = 3 }

    public sealed class BossCombatStatePayload
    {
        public BossCombatStatePayload(BossCombatState state)
        {
            if (state < BossCombatState.Started || state > BossCombatState.Resumed)
                throw new ArgumentOutOfRangeException(nameof(state));
            State = state;
        }
        public BossCombatState State { get; }
    }

    public static class BossCombatStateCodec
    {
        public const int PayloadSize = 1;
        public static byte[] Encode(BossCombatStatePayload payload)
        {
            if (payload == null) throw new ArgumentNullException(nameof(payload));
            return new[] { (byte)payload.State };
        }
        public static BossCombatStatePayload Decode(byte[] payload)
        {
            if (payload == null || payload.Length != PayloadSize)
                throw new ArgumentException("Boss combat state payload must be 1 byte.", nameof(payload));
            return new BossCombatStatePayload((BossCombatState)payload[0]);
        }
    }
}
