using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TopDownRoguelike.Networking.Client;
using UnityEngine;

namespace TopDownRoguelike.Tests.EditMode
{
    public sealed class MultiplayerMenuViewTests
    {
        [Test]
        public void HandleConnectionFailureClosed_ErrorState_DisconnectsClient()
        {
            Type menuType =
                FindType(
                    "TopDownRoguelike.Menu.UI." +
                    "MultiplayerMenuView");

            Assert.That(menuType, Is.Not.Null);

            var menuObject =
                new GameObject("MultiplayerMenuViewTests");

            var client =
                new RecordingRoomNetworkClient();

            try
            {
                Component menu =
                    menuObject.AddComponent(menuType);

                client.SetState(
                    NetworkClientState.Error);

                SetField(
                    menuType,
                    menu,
                    "networkClient",
                    client);

                SetField(
                    menuType,
                    menu,
                    "isConnecting",
                    true);

                MethodInfo closeMethod =
                    menuType.GetMethod(
                        "HandleConnectionFailureClosed",
                        BindingFlags.Instance |
                        BindingFlags.Public);

                Assert.That(
                    closeMethod,
                    Is.Not.Null,
                    "MultiplayerMenuView must expose a handler " +
                    "for closing the connection failure dialog.");

                closeMethod.Invoke(
                    menu,
                    null);

                Assert.That(
                    client.DisconnectCallCount,
                    Is.EqualTo(1));

                Assert.That(
                    client.State,
                    Is.EqualTo(
                        NetworkClientState.Disconnected));

                FieldInfo connectingField =
                    menuType.GetField(
                        "isConnecting",
                        BindingFlags.Instance |
                        BindingFlags.NonPublic);

                Assert.That(
                    connectingField.GetValue(menu),
                    Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(
                    menuObject);
            }
        }

        [Test]
        public void NetworkState_Error_ShowsNetworkError()
        {
            Type menuType =
                FindType(
                    "TopDownRoguelike.Menu.UI." +
                    "MultiplayerMenuView");

            Type textType =
                FindType("TMPro.TextMeshProUGUI");

            Assert.That(menuType, Is.Not.Null);
            Assert.That(textType, Is.Not.Null);

            var menuObject =
                new GameObject("MultiplayerMenuViewTests");

            var validationObject =
                new GameObject("ValidationText");

            var lobbyPanel =
                new GameObject("RoomLobbyPanel");

            var client =
                new RecordingRoomNetworkClient();

            try
            {
                Component menu =
                    menuObject.AddComponent(menuType);

                Component validationText =
                    validationObject.AddComponent(textType);

                lobbyPanel.SetActive(false);

                SetField(
                    menuType,
                    menu,
                    "validationText",
                    validationText);

                SetField(
                    menuType,
                    menu,
                    "roomLobbyPanel",
                    lobbyPanel);

                SetField(
                    menuType,
                    menu,
                    "networkClient",
                    client);

                SetField(
                    menuType,
                    menu,
                    "isConnecting",
                    true);

                client.SetError(
                    "Server rejected the room request.");

                MethodInfo stateHandler =
                    menuType.GetMethod(
                        "HandleNetworkClientStateChanged",
                        BindingFlags.Instance |
                        BindingFlags.NonPublic);

                Assert.That(stateHandler, Is.Not.Null);

                stateHandler.Invoke(
                    menu,
                    new object[]
                    {
                NetworkClientState.Error
                    });

                string message =
                    (string)
                        textType.GetProperty("text").GetValue(
                            validationText);

                Assert.That(
                    message,
                    Does.Contain(
                        "Server rejected the room request."));

                Assert.That(
                    lobbyPanel.activeSelf,
                    Is.False);

                FieldInfo connectingField =
                    menuType.GetField(
                        "isConnecting",
                        BindingFlags.Instance |
                        BindingFlags.NonPublic);

                Assert.That(
                    connectingField.GetValue(menu),
                    Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(
                    lobbyPanel);

                UnityEngine.Object.DestroyImmediate(
                    validationObject);

                UnityEngine.Object.DestroyImmediate(
                    menuObject);
            }
        }

        [Test]
        public void NetworkState_InRoom_OpensLobby()
        {
            Type menuType =
                FindType(
                    "TopDownRoguelike.Menu.UI." +
                    "MultiplayerMenuView");

            Assert.That(menuType, Is.Not.Null);

            var menuObject =
                new GameObject("MultiplayerMenuViewTests");

            var entryPanel =
                new GameObject("MultiplayerEntryPanel");

            var joinFields =
                new GameObject("JoinFields");

            var lobbyPanel =
                new GameObject("RoomLobbyPanel");

            try
            {
                Component menu =
                    menuObject.AddComponent(menuType);

                entryPanel.SetActive(true);
                joinFields.SetActive(true);
                lobbyPanel.SetActive(false);

                SetField(
                    menuType,
                    menu,
                    "multiplayerEntryPanel",
                    entryPanel);

                SetField(
                    menuType,
                    menu,
                    "joinFields",
                    joinFields);

                SetField(
                    menuType,
                    menu,
                    "roomLobbyPanel",
                    lobbyPanel);

                SetField(
                    menuType,
                    menu,
                    "isConnecting",
                    true);

                MethodInfo stateHandler =
                    menuType.GetMethod(
                        "HandleNetworkClientStateChanged",
                        BindingFlags.Instance |
                        BindingFlags.NonPublic);

                Assert.That(
                    stateHandler,
                    Is.Not.Null,
                    "MultiplayerMenuView must handle " +
                    "network client state changes.");

                stateHandler.Invoke(
                    menu,
                    new object[]
                    {
                NetworkClientState.InRoom
                    });

                Assert.That(entryPanel.activeSelf, Is.False);
                Assert.That(joinFields.activeSelf, Is.False);
                Assert.That(lobbyPanel.activeSelf, Is.True);

                FieldInfo connectingField =
                    menuType.GetField(
                        "isConnecting",
                        BindingFlags.Instance |
                        BindingFlags.NonPublic);

                Assert.That(
                    connectingField.GetValue(menu),
                    Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(
                    lobbyPanel);

                UnityEngine.Object.DestroyImmediate(
                    joinFields);

                UnityEngine.Object.DestroyImmediate(
                    entryPanel);

                UnityEngine.Object.DestroyImmediate(
                    menuObject);
            }
        }

        [Test]
        public void HandleJoinRoom_StartsRealJoinConnection()
        {
            Type menuType =
                FindType(
                    "TopDownRoguelike.Menu.UI." +
                    "MultiplayerMenuView");

            Type inputType =
                FindType("TMPro.TMP_InputField");

            Assert.That(menuType, Is.Not.Null);
            Assert.That(inputType, Is.Not.Null);

            var menuObject =
                new GameObject("MultiplayerMenuViewTests");

            var nicknameObject =
                new GameObject("NicknameInput");

            var addressObject =
                new GameObject("AddressInput");

            var portObject =
                new GameObject("PortInput");

            var roomIdObject =
                new GameObject("RoomIdInput");

            var client =
                new RecordingRoomNetworkClient();

            using (var flow =
                new RoomConnectionFlow(client))
            {
                try
                {
                    Component menu =
                        menuObject.AddComponent(menuType);

                    Component nicknameInput =
                        nicknameObject.AddComponent(inputType);

                    Component addressInput =
                        addressObject.AddComponent(inputType);

                    Component portInput =
                        portObject.AddComponent(inputType);

                    Component roomIdInput =
                        roomIdObject.AddComponent(inputType);

                    inputType.GetProperty("text").SetValue(
                        nicknameInput,
                        " Bronya ");

                    inputType.GetProperty("text").SetValue(
                        addressInput,
                        " ::1 ");

                    inputType.GetProperty("text").SetValue(
                        portInput,
                        " 7777 ");

                    inputType.GetProperty("text").SetValue(
                        roomIdInput,
                        " ROOM-1 ");

                    SetField(
                        menuType,
                        menu,
                        "nicknameInput",
                        nicknameInput);

                    SetField(
                        menuType,
                        menu,
                        "addressInput",
                        addressInput);

                    SetField(
                        menuType,
                        menu,
                        "portInput",
                        portInput);

                    SetField(
                        menuType,
                        menu,
                        "roomIdInput",
                        roomIdInput);

                    SetField(
                        menuType,
                        menu,
                        "connectionFlow",
                        flow);

                    MethodInfo handleJoinMethod =
                        menuType.GetMethod(
                            "HandleJoinRoom");

                    Assert.That(handleJoinMethod, Is.Not.Null);

                    handleJoinMethod.Invoke(
                        menu,
                        null);

                    Assert.That(
                        client.ConnectCallCount,
                        Is.EqualTo(1));

                    Assert.That(
                        client.LastAddress,
                        Is.EqualTo("::1"));

                    Assert.That(
                        client.LastPort,
                        Is.EqualTo(7777));

                    Assert.That(
                        client.JoinRoomCallCount,
                        Is.Zero,
                        "JoinRoom must wait for Connected.");

                    client.SetState(
                        NetworkClientState.Connected);

                    Assert.That(
                        client.JoinRoomCallCount,
                        Is.EqualTo(1));

                    Assert.That(
                        client.LastNickname,
                        Is.EqualTo("Bronya"));

                    Assert.That(
                        client.LastRoomId,
                        Is.EqualTo("ROOM-1"));
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(
                        roomIdObject);

                    UnityEngine.Object.DestroyImmediate(
                        portObject);

                    UnityEngine.Object.DestroyImmediate(
                        addressObject);

                    UnityEngine.Object.DestroyImmediate(
                        nicknameObject);

                    UnityEngine.Object.DestroyImmediate(
                        menuObject);
                }
            }
        }

        [Test]
        public void HandleCreateRoom_StartsRealHostConnection()
        {
            Type menuType =
                FindType(
                    "TopDownRoguelike.Menu.UI." +
                    "MultiplayerMenuView");

            Type inputType =
                FindType("TMPro.TMP_InputField");

            Assert.That(menuType, Is.Not.Null);
            Assert.That(inputType, Is.Not.Null);

            var menuObject =
                new GameObject("MultiplayerMenuViewTests");

            var inputObject =
                new GameObject("NicknameInput");

            var client =
                new RecordingRoomNetworkClient();

            using (var flow =
                new RoomConnectionFlow(client))
            {
                try
                {
                    Component menu =
                        menuObject.AddComponent(menuType);

                    Component nicknameInput =
                        inputObject.AddComponent(inputType);

                    inputType.GetProperty("text").SetValue(
                        nicknameInput,
                        " Seele ");

                    SetField(
                        menuType,
                        menu,
                        "nicknameInput",
                        nicknameInput);

                    SetField(
                        menuType,
                        menu,
                        "hostAddress",
                        "::1");

                    SetField(
                        menuType,
                        menu,
                        "hostPort",
                        7777);

                    SetField(
                        menuType,
                        menu,
                        "connectionFlow",
                        flow);

                    MethodInfo handleCreateMethod =
                        menuType.GetMethod(
                            "HandleCreateRoom");

                    Assert.That(
                        handleCreateMethod,
                        Is.Not.Null);

                    handleCreateMethod.Invoke(
                        menu,
                        null);

                    Assert.That(
                        client.ConnectCallCount,
                        Is.EqualTo(1));

                    Assert.That(
                        client.LastAddress,
                        Is.EqualTo("::1"));

                    Assert.That(
                        client.LastPort,
                        Is.EqualTo(7777));

                    Assert.That(
                        client.CreateRoomCallCount,
                        Is.Zero,
                        "CreateRoom must wait for Connected.");
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(
                        inputObject);

                    UnityEngine.Object.DestroyImmediate(
                        menuObject);
                }
            }
        }

        [Test]
        public void Menu_DefinesRealConnectionFields()
        {
            Type menuType =
                AppDomain.CurrentDomain
                    .GetAssemblies()
                    .Select(
                        assembly =>
                            assembly.GetType(
                                "TopDownRoguelike.Menu.UI." +
                                "MultiplayerMenuView"))
                    .FirstOrDefault(
                        type => type != null);

            Assert.That(
                menuType,
                Is.Not.Null,
                "MultiplayerMenuView was not found.");

            AssertFieldType(
                menuType,
                "networkClientBehaviour",
                "TopDownRoguelike.Networking.Client." +
                "NetworkClientBehaviour");

            AssertFieldType(
                menuType,
                "hostAddress",
                typeof(string).FullName);

            AssertFieldType(
                menuType,
                "hostPort",
                typeof(int).FullName);

            AssertFieldType(
                menuType,
                "roomIdInput",
                "TMPro.TMP_InputField");
        }

        private static Type FindType(
            string fullName)
        {
            return AppDomain.CurrentDomain
                .GetAssemblies()
                .Select(
                    assembly =>
                        assembly.GetType(fullName))
                .FirstOrDefault(
                    type => type != null);
        }

        private static void SetField(
            Type ownerType,
            object owner,
            string fieldName,
            object value)
        {
            FieldInfo field =
                ownerType.GetField(
                    fieldName,
                    BindingFlags.Instance |
                    BindingFlags.NonPublic);

            Assert.That(
                field,
                Is.Not.Null,
                $"Missing field: {fieldName}");

            field.SetValue(
                owner,
                value);
        }

        private static void AssertFieldType(
            Type ownerType,
            string fieldName,
            string expectedTypeName)
        {
            FieldInfo field =
                ownerType.GetField(
                    fieldName,
                    BindingFlags.Instance |
                    BindingFlags.NonPublic);

            Assert.That(
                field,
                Is.Not.Null,
                $"Missing field: {fieldName}");

            Assert.That(
                field.FieldType.FullName,
                Is.EqualTo(expectedTypeName));
        }

        private sealed class RecordingRoomNetworkClient
            : IRoomNetworkClient
        {
            public event Action<NetworkClientState>
                StateChanged;

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

            public int DisconnectCallCount
            {
                get;
                private set;
            }

            public string LastNickname
            {
                get;
                private set;
            } = string.Empty;

            public string LastRoomId
            {
                get;
                private set;
            } = string.Empty;

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
                string nickname,
                string roomId)
            {
                JoinRoomCallCount++;
                LastNickname = nickname;
                LastRoomId = roomId;
            }

            public void Disconnect()
            {
                DisconnectCallCount++;

                State =
                    NetworkClientState.Disconnected;

                StateChanged?.Invoke(
                    State);
            }

            public void SetState(
                NetworkClientState state)
            {
                State = state;

                StateChanged?.Invoke(
                    State);
            }

            public void SetError(
                string message)
            {
                LastError =
                    message;
            }
        }
    }
}