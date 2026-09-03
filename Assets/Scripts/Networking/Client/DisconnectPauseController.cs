using TopDownRoguelike.Infrastructure;
using TopDownRoguelike.Networking.Room;
using UnityEngine;

namespace TopDownRoguelike.Networking.Client
{
    public static class DisconnectPauseController
    {
        private static bool isPaused;
        private static float capturedTimeScale = 1f;

        public static bool IsPaused => isPaused;

        public static bool TryPause(DisconnectContext context)
        {
            if (isPaused ||
                context.Role != RoomRole.Host ||
                !context.IsInGameplay ||
                context.Reason != DisconnectReason.RemotePeerLeft ||
                !GameSession.IsHost)
            {
                return false;
            }

            capturedTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            isPaused = true;
            return true;
        }

        public static bool Restore()
        {
            if (!isPaused)
                return false;

            Time.timeScale = capturedTimeScale;
            isPaused = false;
            capturedTimeScale = 1f;
            return true;
        }

        public static void Clear()
        {
            Restore();
        }
    }
}
