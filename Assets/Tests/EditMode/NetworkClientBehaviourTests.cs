using System;
using System.Reflection;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using NUnit.Framework;
using TopDownRoguelike.Networking.Client;
using UnityEngine;

namespace TopDownRoguelike.Tests.EditMode
{
    public sealed class NetworkClientBehaviourTests
    {
        [Test]
        public void Update_DispatchesPendingNetworkEvents()
        {
            var tcpListener =
                new TcpListener(
                    IPAddress.IPv6Loopback,
                    0);

            tcpListener.Start();

            int port =
                ((IPEndPoint)
                    tcpListener.LocalEndpoint).Port;

            var gameObject =
                new GameObject("NetworkClientBehaviourTests");

            NetworkClientBehaviour behaviour = null;
            TcpClient acceptedClient = null;

            try
            {
                behaviour =
                    gameObject.AddComponent<
                        NetworkClientBehaviour>();

                MethodInfo awakeMethod =
                    typeof(NetworkClientBehaviour).GetMethod(
                        "Awake",
                        BindingFlags.Instance |
                        BindingFlags.NonPublic);

                Assert.That(awakeMethod, Is.Not.Null);

                awakeMethod.Invoke(
                    behaviour,
                    null);

                var acceptTask =
                    tcpListener.AcceptTcpClientAsync();

                behaviour.Client.Connect(
                    "::1",
                    port);

                Assert.That(
                    acceptTask.Wait(2000),
                    Is.True,
                    "TCP listener did not accept the client.");

                acceptedClient =
                    acceptTask.Result;

                MethodInfo updateMethod =
                    typeof(NetworkClientBehaviour).GetMethod(
                        "Update",
                        BindingFlags.Instance |
                        BindingFlags.NonPublic);

                Assert.That(
                    updateMethod,
                    Is.Not.Null,
                    "NetworkClientBehaviour must define Update().");

                bool reachedWaitingState =
                    SpinWait.SpinUntil(
                        () =>
                        {
                            updateMethod.Invoke(
                                behaviour,
                                null);

                            return behaviour.Client.State ==
                                NetworkClientState
                                    .WaitingForServerHello;
                        },
                        2000);

                Assert.That(
                    reachedWaitingState,
                    Is.True,
                    $"Current state: {behaviour.Client.State}. " +
                    $"Error: {behaviour.Client.LastError}");
            }
            finally
            {
                behaviour?.Client?.Dispose();
                acceptedClient?.Close();
                tcpListener.Stop();

                UnityEngine.Object.DestroyImmediate(
                    gameObject);
            }
        }

        [Test]
        public void Awake_WhenInstanceExists_KeepsFirstInstance()
        {
            var firstObject =
                new GameObject("FirstNetworkClient");

            var secondObject =
                new GameObject("SecondNetworkClient");

            NetworkClientBehaviour firstBehaviour = null;

            try
            {
                firstBehaviour =
                    firstObject.AddComponent<
                        NetworkClientBehaviour>();

                NetworkClientBehaviour secondBehaviour =
                    secondObject.AddComponent<
                        NetworkClientBehaviour>();

                MethodInfo awakeMethod =
                    typeof(NetworkClientBehaviour).GetMethod(
                        "Awake",
                        BindingFlags.Instance |
                        BindingFlags.NonPublic);

                Assert.That(awakeMethod, Is.Not.Null);

                awakeMethod.Invoke(
                    firstBehaviour,
                    null);

                NetworkClient firstClient =
                    firstBehaviour.Client;

                awakeMethod.Invoke(
                    secondBehaviour,
                    null);

                Assert.That(
                    NetworkClientBehaviour.Instance,
                    Is.SameAs(firstBehaviour),
                    "A duplicate must not replace Instance.");

                Assert.That(
                    firstBehaviour.Client,
                    Is.SameAs(firstClient));

                Assert.DoesNotThrow(
                    () => firstClient.Tick());

                Assert.That(
                    secondObject == null,
                    Is.True,
                    "The duplicate GameObject must be destroyed.");
            }
            finally
            {
                if (secondObject != null)
                {
                    UnityEngine.Object.DestroyImmediate(
                        secondObject);
                }

                if (firstObject != null)
                {
                    UnityEngine.Object.DestroyImmediate(
                        firstObject);
                }
            }
        }

        [Test]
        public void Instance_IsRegisteredAndClearedByLifecycle()
        {
            Type behaviourType =
                typeof(NetworkClientBehaviour);

            PropertyInfo instanceProperty =
                behaviourType.GetProperty(
                    "Instance",
                    BindingFlags.Public |
                    BindingFlags.Static);

            Assert.That(
                instanceProperty,
                Is.Not.Null,
                "NetworkClientBehaviour must expose Instance.");

            var gameObject =
                new GameObject("NetworkClientBehaviourTests");

            NetworkClientBehaviour behaviour = null;
            MethodInfo onDestroyMethod = null;

            try
            {
                behaviour =
                    gameObject.AddComponent<
                        NetworkClientBehaviour>();

                MethodInfo awakeMethod =
                    behaviourType.GetMethod(
                        "Awake",
                        BindingFlags.Instance |
                        BindingFlags.NonPublic);

                onDestroyMethod =
                    behaviourType.GetMethod(
                        "OnDestroy",
                        BindingFlags.Instance |
                        BindingFlags.NonPublic);

                Assert.That(awakeMethod, Is.Not.Null);
                Assert.That(onDestroyMethod, Is.Not.Null);

                awakeMethod.Invoke(
                    behaviour,
                    null);

                Assert.That(
                    instanceProperty.GetValue(null),
                    Is.SameAs(behaviour));

                onDestroyMethod.Invoke(
                    behaviour,
                    null);

                Assert.That(
                    instanceProperty.GetValue(null),
                    Is.Null);
            }
            finally
            {
                onDestroyMethod?.Invoke(
                    behaviour,
                    null);

                UnityEngine.Object.DestroyImmediate(
                    gameObject);
            }
        }

        [Test]
        public void OnDestroy_DisposesNetworkClient()
        {
            var gameObject =
                new GameObject("NetworkClientBehaviourTests");

            NetworkClientBehaviour behaviour = null;
            NetworkClient client = null;

            try
            {
                behaviour =
                    gameObject.AddComponent<
                        NetworkClientBehaviour>();

                MethodInfo awakeMethod =
                    typeof(NetworkClientBehaviour).GetMethod(
                        "Awake",
                        BindingFlags.Instance |
                        BindingFlags.NonPublic);

                Assert.That(awakeMethod, Is.Not.Null);

                awakeMethod.Invoke(
                    behaviour,
                    null);

                client =
                    behaviour.Client;

                Assert.That(client, Is.Not.Null);

                MethodInfo onDestroyMethod =
                    typeof(NetworkClientBehaviour).GetMethod(
                        "OnDestroy",
                        BindingFlags.Instance |
                        BindingFlags.NonPublic);

                Assert.That(
                    onDestroyMethod,
                    Is.Not.Null,
                    "NetworkClientBehaviour must define OnDestroy().");

                onDestroyMethod.Invoke(
                    behaviour,
                    null);

                Assert.Throws<ObjectDisposedException>(
                    () => client.Tick());
            }
            finally
            {
                client?.Dispose();

                UnityEngine.Object.DestroyImmediate(
                    gameObject);
            }
        }

        [Test]
        public void Awake_CreatesDisconnectedNetworkClient()
        {
            Type behaviourType =
                typeof(NetworkClient).Assembly.GetType(
                    "TopDownRoguelike.Networking.Client." +
                    "NetworkClientBehaviour");

            Assert.That(
                behaviourType,
                Is.Not.Null,
                "NetworkClientBehaviour has not been created.");

            var gameObject =
                new GameObject("NetworkClientBehaviourTests");

            NetworkClient client = null;

            try
            {
                Component behaviour =
                    gameObject.AddComponent(behaviourType);

                MethodInfo awakeMethod =
                    behaviourType.GetMethod(
                        "Awake",
                        BindingFlags.Instance |
                        BindingFlags.NonPublic);

                Assert.That(
                    awakeMethod,
                    Is.Not.Null,
                    "NetworkClientBehaviour must define Awake().");

                awakeMethod.Invoke(
                    behaviour,
                    null);

                PropertyInfo clientProperty =
                                    behaviourType.GetProperty("Client");

                Assert.That(clientProperty, Is.Not.Null);

                client =
                    clientProperty.GetValue(behaviour)
                    as NetworkClient;

                Assert.That(client, Is.Not.Null);
                Assert.That(
                    client.State,
                    Is.EqualTo(
                        NetworkClientState.Disconnected));
            }
            finally
            {
                client?.Dispose();
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }
    }
}