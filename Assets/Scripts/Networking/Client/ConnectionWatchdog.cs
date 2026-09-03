using System;

namespace TopDownRoguelike.Networking.Client
{
    public sealed class ConnectionWatchdog
    {
        private readonly Func<double> clock;
        private readonly Action sendHeartbeat;
        private double nextHeartbeatAt;
        private double lastActivityAt;
        private bool started;
        private bool timedOut;

        public ConnectionWatchdog(
            Func<double> clock,
            Action sendHeartbeat)
        {
            this.clock = clock ?? throw new ArgumentNullException(nameof(clock));
            this.sendHeartbeat = sendHeartbeat ?? throw new ArgumentNullException(nameof(sendHeartbeat));
        }

        public void Start()
        {
            nextHeartbeatAt = clock() + HeartbeatTiming.IntervalSeconds;
            lastActivityAt = clock();
            timedOut = false;
            started = true;
        }

        public void Stop() { started = false; }

        public void MarkActivity()
        {
            if (started)
                lastActivityAt = clock();
        }

        public void Tick()
        {
            if (!started)
                return;

            double now = clock();
            if (now - lastActivityAt >= HeartbeatTiming.TimeoutSeconds)
            {
                if (!timedOut)
                {
                    timedOut = true;
                    started = false;
                    throw new TimeoutException("TCP heartbeat timed out.");
                }
                return;
            }

            if (now < nextHeartbeatAt)
                return;

            sendHeartbeat();
            nextHeartbeatAt = now + HeartbeatTiming.IntervalSeconds;
        }
    }
}
