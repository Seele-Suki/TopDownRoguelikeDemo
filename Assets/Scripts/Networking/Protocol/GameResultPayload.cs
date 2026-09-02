using System;

namespace TopDownRoguelike.Networking.Protocol
{
    public enum GameResult : byte { Victory = 1, Defeat = 2 }

    public sealed class GameResultPayload
    {
        public GameResultPayload(GameResult result)
        {
            if (result != GameResult.Victory && result != GameResult.Defeat)
                throw new ArgumentOutOfRangeException(nameof(result));
            Result = result;
        }
        public GameResult Result { get; }
    }

    public static class GameResultCodec
    {
        public const int PayloadSize = 1;
        public static byte[] Encode(GameResultPayload payload)
        {
            if (payload == null) throw new ArgumentNullException(nameof(payload));
            return new[] { (byte)payload.Result };
        }
        public static GameResultPayload Decode(byte[] payload)
        {
            if (payload == null || payload.Length != PayloadSize)
                throw new ArgumentException("Game result payload must be 1 byte.", nameof(payload));
            return new GameResultPayload((GameResult)payload[0]);
        }
    }
}
