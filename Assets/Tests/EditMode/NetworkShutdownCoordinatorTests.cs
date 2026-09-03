using System;
using NUnit.Framework;
using TopDownRoguelike.Networking.Client;

namespace TopDownRoguelike.Tests.EditMode
{
    public sealed class NetworkShutdownCoordinatorTests
    {
        [Test]
        public void Shutdown_ExecutesCleanupOnlyOnce()
        {
            var coordinator = new NetworkShutdownCoordinator();
            int calls = 0;
            Assert.That(coordinator.Shutdown(() => calls++), Is.True);
            Assert.That(coordinator.Shutdown(() => calls++), Is.False);
            Assert.That(calls, Is.EqualTo(1));
            Assert.That(coordinator.IsShutdown, Is.True);
        }

        [Test]
        public void Shutdown_RejectsNullCleanup()
        {
            var coordinator = new NetworkShutdownCoordinator();
            Assert.Throws<ArgumentNullException>(() => coordinator.Shutdown(null));
        }
    }
}
