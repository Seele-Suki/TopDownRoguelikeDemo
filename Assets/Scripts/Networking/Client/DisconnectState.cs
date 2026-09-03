namespace TopDownRoguelike.Networking.Client
{
    public sealed class DisconnectState
    {
        private readonly object syncRoot = new object();

        private bool isHandled;
        private DisconnectReason reason;

        public bool IsHandled
        {
            get
            {
                lock (syncRoot)
                {
                    return isHandled;
                }
            }
        }

        public DisconnectReason Reason
        {
            get
            {
                lock (syncRoot)
                {
                    return reason;
                }
            }
        }

        public bool TryBegin(DisconnectReason nextReason)
        {
            if (nextReason == DisconnectReason.None)
            {
                return false;
            }

            lock (syncRoot)
            {
                if (isHandled)
                {
                    return false;
                }

                reason = nextReason;
                isHandled = true;
                return true;
            }
        }

        public void Reset()
        {
            lock (syncRoot)
            {
                reason = DisconnectReason.None;
                isHandled = false;
            }
        }
    }
}
