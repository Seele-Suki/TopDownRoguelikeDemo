using System;
using System.Threading;

namespace TopDownRoguelike.Networking.Transport
{
    public sealed class
        MainThreadNetworkEventDispatcher
    {
        private readonly
            MainThreadMessageQueue<
                NetworkTransportEvent>
            messageQueue;

        private readonly int ownerThreadId;

        public MainThreadNetworkEventDispatcher(
            MainThreadMessageQueue<
                NetworkTransportEvent>
                messageQueue)
        {
            this.messageQueue =
                messageQueue
                ?? throw new ArgumentNullException(
                    nameof(messageQueue));

            ownerThreadId =
                Thread.CurrentThread.ManagedThreadId;
        }

        public event Action<NetworkTransportEvent>
            EventDispatched;

        public int DispatchPending()
        {
            EnsureOwnerThread();

            int dispatchedCount =
                0;

            while (messageQueue.TryDequeue(
                out NetworkTransportEvent
                    transportEvent))
            {
                EventDispatched?.Invoke(
                    transportEvent);

                dispatchedCount++;
            }

            return dispatchedCount;
        }

        private void EnsureOwnerThread()
        {
            if (Thread.CurrentThread.ManagedThreadId ==
                ownerThreadId)
            {
                return;
            }

            throw new InvalidOperationException(
                "Network events must be dispatched " +
                "from the dispatcher owner thread.");
        }
    }
}