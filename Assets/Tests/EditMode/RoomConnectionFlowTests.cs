using System;
using System.Reflection;
using NUnit.Framework;
using TopDownRoguelike.Networking.Client;
using TopDownRoguelike.Networking.Protocol;
using TopDownRoguelike.Infrastructure;

namespace TopDownRoguelike.Tests.EditMode
{
    public sealed class RoomConnectionFlowTests
    {
        [Test]
        public void Error_ClearsPendingRequestAndAllowsRetry()
        {
            var client =
                new FakeRoomNetworkClient();

            using (var flow =
                new RoomConnectionFlow(client))
            {
                RoomConnectionRequest firstRequest =
                    RoomConnectionRequest.CreateHost(
                        "FirstHost",
                        "::1",
                        7777);

                flow.BeginHost(
                    firstRequest);

                client.SetState(
                    NetworkClientState.Error);

                client.SetState(
                    NetworkClientState.Connected);

                Assert.That(
                    client.CreateRoomCallCount,
                    Is.Zero,
                    "A failed request must not run after " +
                    "a delayed Connected state.");

                client.SetState(
                    NetworkClientState.Disconnected);

                RoomConnectionRequest retryRequest =
                    RoomConnectionRequest.CreateHost(
                        "RetryHost",
                        "::1",
                        7777);

                flow.BeginHost(
                    retryRequest);

                client.SetState(
                    NetworkClientState.Connected);

                Assert.That(
                    client.CreateRoomCallCount,
                    Is.EqualTo(1));

                Assert.That(
                    client.LastNickname,
                    Is.EqualTo("RetryHost"));
            }
        }

        [Test]
        public void BeginJoin_WaitsForConnectedBeforeJoiningRoom()
        {
            Type flowType =
                typeof(NetworkClient).Assembly.GetType(
                    "TopDownRoguelike.Networking.Client." +
                    "RoomConnectionFlow");

            Assert.That(flowType, Is.Not.Null);

            var client =
                new FakeRoomNetworkClient();

            object flow =
                Activator.CreateInstance(
                    flowType,
                    client);

            try
            {
                MethodInfo beginJoinMethod =
                    flowType.GetMethod("BeginJoin");

                Assert.That(
                    beginJoinMethod,
                    Is.Not.Null,
                    "RoomConnectionFlow must define BeginJoin().");

                RoomConnectionRequest request =
                    RoomConnectionRequest.CreateJoin(
                        " Bronya ",
                        " ::1 ",
                        " 7777 ");

                beginJoinMethod.Invoke(
                    flow,
                    new object[]
                    {
                request
                    });

                Assert.That(client.ConnectCallCount, Is.EqualTo(1));
                Assert.That(client.LastAddress, Is.EqualTo("::1"));
                Assert.That(client.LastPort, Is.EqualTo(7777));

                Assert.That(
                    client.JoinRoomCallCount,
                    Is.Zero,
                    "Joining must wait for Connected.");

                Assert.That(
                    client.CreateRoomCallCount,
                    Is.Zero);

                client.SetState(
                    NetworkClientState.Connected);

                Assert.That(
                    client.JoinRoomCallCount,
                    Is.EqualTo(1));

                Assert.That(
                    client.LastNickname,
                    Is.EqualTo("Bronya"));

                Assert.That(
                    client.CreateRoomCallCount,
                    Is.Zero);
            }
            finally
            {
                (flow as IDisposable)?.Dispose();
            }
        }

        [Test]
        public void BeginHost_WaitsForConnectedBeforeCreatingRoom()
        {
            Type flowType =
                typeof(NetworkClient).Assembly.GetType(
                    "TopDownRoguelike.Networking.Client." +
                    "RoomConnectionFlow");

            Assert.That(
                flowType,
                Is.Not.Null,
                "RoomConnectionFlow has not been created.");

            var client =
                new FakeRoomNetworkClient();

            object flow =
                Activator.CreateInstance(
                    flowType,
                    client);

            try
            {
                MethodInfo beginHostMethod =
                    flowType.GetMethod("BeginHost");

                Assert.That(beginHostMethod, Is.Not.Null);

                RoomConnectionRequest request =
                    RoomConnectionRequest.CreateHost(
                        " Seele ",
                        " ::1 ",
                        7777);

                beginHostMethod.Invoke(
                    flow,
                    new object[]
                    {
                request
                    });

                Assert.That(client.ConnectCallCount, Is.EqualTo(1));
                Assert.That(client.LastAddress, Is.EqualTo("::1"));
                Assert.That(client.LastPort, Is.EqualTo(7777));

                Assert.That(
                    client.CreateRoomCallCount,
                    Is.Zero,
                    "Room creation must wait for Connected.");

                client.SetState(
                    NetworkClientState.Connected);

                Assert.That(
                    client.CreateRoomCallCount,
                    Is.EqualTo(1));

                Assert.That(
                    client.LastNickname,
                    Is.EqualTo("Seele"));
            }
            finally
            {
                (flow as IDisposable)?.Dispose();
            }
        }

        [Test]
        public void NetworkClient_ImplementsRoomConnectionContract()
        {
            Type contractType =
                typeof(NetworkClient).Assembly.GetType(
                    "TopDownRoguelike.Networking.Client." +
                    "IRoomNetworkClient");

            Assert.That(
                contractType,
                Is.Not.Null,
                "IRoomNetworkClient has not been created.");

            Assert.That(
                contractType.IsInterface,
                Is.True);

            Assert.That(
                contractType.IsAssignableFrom(
                    typeof(NetworkClient)),
                Is.True,
                "NetworkClient must implement " +
                "IRoomNetworkClient.");

            Assert.That(
                contractType.GetEvent("StateChanged"),
                Is.Not.Null);

            Assert.That(
                contractType.GetProperty("State"),
                Is.Not.Null);

            Assert.That(
                contractType.GetProperty("LastError"),
                Is.Not.Null);

            Assert.That(
                contractType.GetEvent("RoomStateChanged"),
                Is.Not.Null,
                "IRoomNetworkClient must expose RoomStateChanged.");

            Assert.That(
                contractType.GetEvent("ErrorReceived"),
                Is.Not.Null,
                "IRoomNetworkClient must expose ErrorReceived.");

            Assert.That(
                contractType.GetEvent("GameStarted"),
                Is.Not.Null,
                "IRoomNetworkClient must expose GameStarted.");

            Assert.That(
                contractType.GetProperty("PlayerId"),
                Is.Not.Null,
                "IRoomNetworkClient must expose PlayerId.");

            Assert.That(
                contractType.GetProperty("CurrentRoomId"),
                Is.Not.Null,
                "IRoomNetworkClient must expose CurrentRoomId.");

            PropertyInfo currentRoomStateProperty =
                contractType.GetProperty(
                    "CurrentRoomState");

            Assert.That(
                currentRoomStateProperty,
                Is.Not.Null,
                "IRoomNetworkClient must expose CurrentRoomState.");

            Assert.That(
                currentRoomStateProperty.PropertyType.FullName,
                Is.EqualTo(
                    "TopDownRoguelike.Networking.Protocol." +
                    "RoomStateSnapshot"));

            Assert.That(
                contractType.GetMethod("Connect"),
                Is.Not.Null);

            Assert.That(
                contractType.GetMethod("CreateRoom"),
                Is.Not.Null);

            MethodInfo joinRoomMethod =
                contractType.GetMethod(
                    "JoinRoom",
                    new[]
                    {
                        typeof(string)
                    });

            Assert.That(
                joinRoomMethod,
                Is.Not.Null,
                "IRoomNetworkClient must define " +
                "JoinRoom(string nickname).");

            Assert.That(
                contractType.GetMethod(
                    "JoinRoom",
                    new[]
                    {
            typeof(string),
            typeof(string)
                    }),
                Is.Null,
                "IRoomNetworkClient must not require a room ID.");

            MethodInfo setPlayerSelectionMethod =
                contractType.GetMethod(
                    "SetPlayerSelection",
                    new[]
                    {
                        typeof(CharacterId),
                        typeof(DifficultyId)
                    });

            Assert.That(
                setPlayerSelectionMethod,
                Is.Not.Null,
                "IRoomNetworkClient must expose " +
                "SetPlayerSelection(CharacterId, DifficultyId).");

            Assert.That(
                contractType.GetMethod("Disconnect"),
                Is.Not.Null);

            Assert.That(
                contractType.GetMethod("LeaveRoom"),
                Is.Not.Null,
                "IRoomNetworkClient must expose LeaveRoom().");
        }

        private sealed class FakeRoomNetworkClient
            : IRoomNetworkClient
        {
            public event Action<NetworkClientState>
                StateChanged;

            public event Action<RoomStateSnapshot>
                RoomStateChanged
            {
                add
                {
                }
                remove
                {
                }
            }

            public event Action<string>
                ErrorReceived
            {
                add
                {
                }
                remove
                {
                }
            }

            public event Action
                GameStarted
            {
                add
                {
                }
                remove
                {
                }
            }

            public uint PlayerId =>
                0u;

            public string CurrentRoomId =>
                string.Empty;

            public RoomStateSnapshot CurrentRoomState =>
                null;

            public NetworkClientState State
            {
                get;
                private set;
            } = NetworkClientState.Disconnected;

            public string LastError
            {
                get;
                private set;
            } = string.Empty;

            public int ConnectCallCount
            {
                get;
                private set;
            }

            public int CreateRoomCallCount
            {
                get;
                private set;
            }

            public int JoinRoomCallCount
            {
                get;
                private set;
            }

            public string LastAddress
            {
                get;
                private set;
            } = string.Empty;

            public int LastPort
            {
                get;
                private set;
            }

            public string LastNickname
            {
                get;
                private set;
            } = string.Empty;

            public void Connect(
                string address,
                int port)
            {
                ConnectCallCount++;
                LastAddress = address;
                LastPort = port;

                State =
                    NetworkClientState.ConnectingTcp;
            }

            public void CreateRoom(
                string nickname)
            {
                CreateRoomCallCount++;
                LastNickname = nickname;
            }

            public void JoinRoom(
                string nickname)
            {
                JoinRoomCallCount++;
                LastNickname = nickname;
            }

            public void SetPlayerSelection(
                CharacterId character,
                DifficultyId difficulty)
            {
            }

            public void SetReady(
                bool ready)
            {
            }

            public void StartGame()
            {
            }

            public void LeaveRoom()
            {
            }

            public void Disconnect()
            {
                State =
                    NetworkClientState.Disconnected;
            }

            public void SetState(
                NetworkClientState state)
            {
                State = state;

                StateChanged?.Invoke(
                    State);
            }
        }
    }
}