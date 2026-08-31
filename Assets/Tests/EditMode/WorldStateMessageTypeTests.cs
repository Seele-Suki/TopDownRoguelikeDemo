using System;
using NUnit.Framework;
using TopDownRoguelike.Networking.Protocol;

namespace TopDownRoguelike.Tests.EditMode
{
    public sealed class WorldStateMessageTypeTests
    {
        [Test]
        public void WorldStateSnapshot_HasStableValue()
        {
            Assert.That(
                Enum.IsDefined(
                    typeof(MessageType),
                    "WorldStateSnapshot"),
                Is.True);

            MessageType messageType =
                (MessageType)Enum.Parse(
                    typeof(MessageType),
                    "WorldStateSnapshot");

            Assert.That(
                (ushort)messageType,
                Is.EqualTo(40));
        }
    }
}