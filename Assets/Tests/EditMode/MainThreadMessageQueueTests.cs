using NUnit.Framework;
using TopDownRoguelike.Networking.Transport;

namespace TopDownRoguelike.Tests.EditMode
{
    public sealed class MainThreadMessageQueueTests
    {
        private sealed class TestMessage
        {
        }

        [Test]
        public void EnqueueThenTryDequeue_ReturnsSameMessage()
        {
            var queue =
                new MainThreadMessageQueue<TestMessage>();

            var expected = new TestMessage();

            queue.Enqueue(expected);

            bool dequeued =
                queue.TryDequeue(out TestMessage actual);

            Assert.That(dequeued, Is.True);
            Assert.That(actual, Is.SameAs(expected));
        }
    }
}