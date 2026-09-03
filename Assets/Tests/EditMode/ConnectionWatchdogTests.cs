using System;
using NUnit.Framework;
using TopDownRoguelike.Networking.Client;

namespace TopDownRoguelike.Tests.EditMode
{
    public sealed class ConnectionWatchdogTests
    {
        [Test]
        public void Tick_SendsHeartbeatAtConfiguredInterval()
        {
            double now = 10.0;
            int sends = 0;
            var watchdog = new ConnectionWatchdog(() => now, () => sends++);

            watchdog.Start();
            watchdog.Tick();
            Assert.That(sends, Is.EqualTo(0));

            now += HeartbeatTiming.IntervalSeconds - 0.01;
            watchdog.Tick();
            Assert.That(sends, Is.EqualTo(0));

            now += 0.01;
            watchdog.Tick();
            Assert.That(sends, Is.EqualTo(1));

            now += HeartbeatTiming.IntervalSeconds;
            watchdog.Tick();
            Assert.That(sends, Is.EqualTo(2));
        }

        [Test]
        public void Stop_PreventsFurtherHeartbeats()
        {
            double now = 0.0;
            int sends = 0;
            var watchdog = new ConnectionWatchdog(() => now, () => sends++);
            watchdog.Start();
            watchdog.Stop();
            now += HeartbeatTiming.IntervalSeconds * 2.0;
            watchdog.Tick();
            Assert.That(sends, Is.EqualTo(0));
        }

        [Test]
        public void MarkActivity_PreventsTimeoutUntilActivityExpires()
        {
            double now = 0.0;
            int sends = 0;
            var watchdog = new ConnectionWatchdog(() => now, () => sends++);
            watchdog.Start();
            now += HeartbeatTiming.IntervalSeconds;
            watchdog.Tick();
            now += HeartbeatTiming.IntervalSeconds;
            watchdog.MarkActivity();
            now += HeartbeatTiming.TimeoutSeconds - 0.01;
            Assert.DoesNotThrow(() => watchdog.Tick());
            now += 0.01;
            Assert.Throws<TimeoutException>(() => watchdog.Tick());
        }

        [Test]
        public void Timeout_IsRaisedOnlyOnce()
        {
            double now = 0.0;
            var watchdog = new ConnectionWatchdog(() => now, () => { });
            watchdog.Start();
            now += HeartbeatTiming.TimeoutSeconds;
            Assert.Throws<TimeoutException>(() => watchdog.Tick());
            Assert.DoesNotThrow(() => watchdog.Tick());
        }
    }
}
