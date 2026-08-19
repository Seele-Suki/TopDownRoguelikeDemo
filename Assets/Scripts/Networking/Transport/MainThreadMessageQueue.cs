using System.Collections.Concurrent;

namespace TopDownRoguelike.Networking.Transport
{
    public sealed class MainThreadMessageQueue<T>
        where T : class
    {
        private readonly ConcurrentQueue<T> messages =
            new ConcurrentQueue<T>();

        public void Enqueue(T message)
        {
            messages.Enqueue(message);
        }

        public bool TryDequeue(out T message)
        {
            return messages.TryDequeue(out message);
        }
    }
}