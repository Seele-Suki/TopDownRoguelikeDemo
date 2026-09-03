using System;

namespace TopDownRoguelike.Networking.Client
{
    public sealed class NetworkShutdownCoordinator
    {
        private readonly object syncRoot = new object();
        private bool isShutdown;

        public bool IsShutdown
        {
            get { lock (syncRoot) return isShutdown; }
        }

        public bool TryBeginShutdown()
        {
            lock (syncRoot)
            {
                if (isShutdown)
                    return false;
                isShutdown = true;
                return true;
            }
        }

        public bool Shutdown(Action cleanup)
        {
            if (cleanup == null)
                throw new ArgumentNullException(nameof(cleanup));
            if (!TryBeginShutdown())
                return false;
            cleanup();
            return true;
        }
    }
}
