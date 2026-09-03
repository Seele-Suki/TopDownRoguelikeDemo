using System;
using System.Reflection;
using NUnit.Framework;
using TopDownRoguelike.Networking.Client;
using TopDownRoguelike.Networking.Protocol;
using TopDownRoguelike.Networking.Transport;
using UnityEngine;
using UnityEngine.TestTools;

namespace TopDownRoguelike.Tests.EditMode
{
    public sealed class NetworkClientDisconnectTests
    {
        private static void RaiseTransportEvent(NetworkClient client, NetworkTransportEvent transportEvent)
        {
            MethodInfo method = typeof(NetworkClient).GetMethod(
                "HandleTransportEvent",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            method.Invoke(client, new object[] { transportEvent });
        }

        [Test]
        public void TransportErrors_ProduceOnlyOneDisconnectNotification()
        {
            using (var client = new NetworkClient())
            {
                int notifications = 0;
                DisconnectReason reason = DisconnectReason.None;
                client.DisconnectOccurred += value => { notifications++; reason = value; };
                LogAssert.Expect(LogType.Error, "NetworkClient failure: socket failed");
                RaiseTransportEvent(client, NetworkTransportEvent.Error(NetworkTransportKind.Tcp, "socket failed"));
                RaiseTransportEvent(client, NetworkTransportEvent.Disconnected(NetworkTransportKind.Udp));
                Assert.That(notifications, Is.EqualTo(1));
                Assert.That(reason, Is.EqualTo(DisconnectReason.TransportError));
                Assert.That(client.DisconnectReason, Is.EqualTo(DisconnectReason.TransportError));
            }
        }

        [Test]
        public void HeartbeatTimeout_PreservesHeartbeatReason()
        {
            using (var client = new NetworkClient())
            {
                DisconnectReason reason = DisconnectReason.None;
                client.DisconnectOccurred += value => reason = value;
                LogAssert.Expect(LogType.Error, "NetworkClient failure: TCP heartbeat timed out.");
                RaiseTransportEvent(client, NetworkTransportEvent.Error(NetworkTransportKind.Tcp, "TCP heartbeat timed out."));
                Assert.That(reason, Is.EqualTo(DisconnectReason.HeartbeatTimeout));
                Assert.That(client.DisconnectReason, Is.EqualTo(DisconnectReason.HeartbeatTimeout));
            }
        }
    }
}
