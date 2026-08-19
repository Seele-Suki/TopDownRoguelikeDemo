using System;
using System.Threading;
using NUnit.Framework;
using TopDownRoguelike.Networking.Transport;

namespace TopDownRoguelike.Tests.EditMode
{
    public sealed class
        MainThreadNetworkEventDispatcherTests
    {
        [Test]
        public void BackgroundEnqueue_DispatchesOnOwnerThread()
        {
            int ownerThreadId =
                Thread.CurrentThread.ManagedThreadId;

            var queue =
                new MainThreadMessageQueue<
                    NetworkTransportEvent>();

            var dispatcher =
                new MainThreadNetworkEventDispatcher(
                    queue);

            NetworkTransportEvent expected =
                NetworkTransportEvent.Connected(
                    NetworkTransportKind.Tcp);

            NetworkTransportEvent actual =
                null;

            int callbackThreadId =
                -1;

            dispatcher.EventDispatched +=
                transportEvent =>
                {
                    actual =
                        transportEvent;

                    callbackThreadId =
                        Thread.CurrentThread.ManagedThreadId;
                };

            var worker =
                new Thread(
                    () =>
                        queue.Enqueue(expected));

            worker.Start();

            Assert.That(
                worker.Join(2000),
                Is.True);

            Assert.That(
                actual,
                Is.Null);

            int dispatchedCount =
                dispatcher.DispatchPending();

            Assert.That(
                dispatchedCount,
                Is.EqualTo(1));

            Assert.That(
                actual,
                Is.SameAs(expected));

            Assert.That(
                callbackThreadId,
                Is.EqualTo(ownerThreadId));
        }

        [Test]
        public void DispatchPending_DrainsAllQueuedEvents()
        {
            var queue =
                new MainThreadMessageQueue<
                    NetworkTransportEvent>();

            var dispatcher =
                new MainThreadNetworkEventDispatcher(
                    queue);

            queue.Enqueue(
                NetworkTransportEvent.Connected(
                    NetworkTransportKind.Tcp));

            queue.Enqueue(
                NetworkTransportEvent.Connected(
                    NetworkTransportKind.Udp));

            int callbackCount =
                0;

            dispatcher.EventDispatched +=
                _ => callbackCount++;

            int firstDispatchCount =
                dispatcher.DispatchPending();

            int secondDispatchCount =
                dispatcher.DispatchPending();

            Assert.That(
                firstDispatchCount,
                Is.EqualTo(2));

            Assert.That(
                callbackCount,
                Is.EqualTo(2));

            Assert.That(
                secondDispatchCount,
                Is.EqualTo(0));
        }

        [Test]
        public void DispatchPending_FromOtherThread_Throws()
        {
            var queue =
                new MainThreadMessageQueue<
                    NetworkTransportEvent>();

            var dispatcher =
                new MainThreadNetworkEventDispatcher(
                    queue);

            Exception capturedException =
                null;

            var worker =
                new Thread(
                    () =>
                    {
                        try
                        {
                            dispatcher.DispatchPending();
                        }
                        catch (Exception exception)
                        {
                            capturedException =
                                exception;
                        }
                    });

            worker.Start();

            Assert.That(
                worker.Join(2000),
                Is.True);

            Assert.That(
                capturedException,
                Is.TypeOf<InvalidOperationException>());
        }
    }
}