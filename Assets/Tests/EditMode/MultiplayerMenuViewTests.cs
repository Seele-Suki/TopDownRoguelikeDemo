using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.IO;
using NUnit.Framework;
using TopDownRoguelike.Infrastructure;
using TopDownRoguelike.Networking.Client;
using TopDownRoguelike.Networking.Protocol;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

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
        public void RoomStateChanged_AppliesSnapshotToLobby()
        {
            Type menuType =
                FindType(
                    "TopDownRoguelike.Menu.UI." +
                    "MultiplayerMenuView");

            Type lobbyType =
                FindType("RoomLobbyView");

            Assert.That(menuType, Is.Not.Null);
            Assert.That(lobbyType, Is.Not.Null);

            var menuObject =
                new GameObject("MultiplayerMenuViewTests");

            var lobbyObject =
                new GameObject("RoomLobbyViewTests");

            var client =
                new RecordingRoomNetworkClient();

            try
            {
                Component menu =
                    menuObject.AddComponent(menuType);

                Component lobby =
                    lobbyObject.AddComponent(lobbyType);

                SetField(
                    menuType,
                    menu,
                    "networkClient",
                    client);

                SetField(
                    menuType,
                    menu,
                    "roomLobbyView",
                    lobby);

                MethodInfo stateHandler =
                    menuType.GetMethod(
                        "HandleNetworkClientStateChanged",
                        BindingFlags.Instance |
                        BindingFlags.NonPublic);

                Assert.That(
                    stateHandler,
                    Is.Not.Null);

                stateHandler.Invoke(
                    menu,
                    new object[]
                    {
                        NetworkClientState.InRoom
                    });

                var players =
                    new List<RoomPlayerSnapshot>
                    {
                        new RoomPlayerSnapshot(
                            42u,
                            true,
                            false,
                            CharacterId.Ranged,
                            "RealHost"),
                        new RoomPlayerSnapshot(
                            77u,
                            false,
                            false,
                            CharacterId.Ranged,
                            "RealClient")
                    };

                var snapshot =
                    new RoomStateSnapshot(
                        "ROOM-9",
                        RoomStateStatus.Waiting,
                        DifficultyId.Normal,
                        players);

                client.EmitRoomState(
                    77u,
                    snapshot);

                FieldInfo localPlayerIdField =
                    lobbyType.GetField(
                        "localPlayerId",
                        BindingFlags.Instance |
                        BindingFlags.NonPublic);

                Assert.That(
                    localPlayerIdField,
                    Is.Not.Null);

                Assert.That(
                    localPlayerIdField.GetValue(lobby),
                    Is.EqualTo(77));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(
                    lobbyObject);

                UnityEngine.Object.DestroyImmediate(
                    menuObject);
            }
        }

        [Test]
        public void InRoom_ForwardsConnectionEndpointToLobby()
        {
            Type menuType =
                FindType(
                    "TopDownRoguelike.Menu.UI." +
                    "MultiplayerMenuView");

            Type lobbyType =
                FindType("RoomLobbyView");

            Assert.That(menuType, Is.Not.Null);
            Assert.That(lobbyType, Is.Not.Null);

            var menuObject =
                new GameObject("EndpointMenuTests");

            var lobbyObject =
                new GameObject("EndpointLobbyTests");

            var addressObject =
                new GameObject("EndpointAddressText");

            addressObject.transform.SetParent(
                lobbyObject.transform);

            var client =
                new RecordingRoomNetworkClient();

            try
            {
                Component menu =
                    menuObject.AddComponent(menuType);

                Component lobby =
                    lobbyObject.AddComponent(lobbyType);

                TMP_Text addressText =
                    addressObject.AddComponent<TextMeshProUGUI>();

                SetField(
                    menuType,
                    menu,
                    "networkClient",
                    client);

                SetField(
                    menuType,
                    menu,
                    "roomLobbyView",
                    lobby);

                SetField(
                    lobbyType,
                    lobby,
                    "addressText",
                    addressText);

                SetField(
                    menuType,
                    menu,
                    "activeConnectionAddress",
                    "::1");

                SetField(
                    menuType,
                    menu,
                    "activeConnectionPort",
                    7777);

                MethodInfo stateHandler =
                    menuType.GetMethod(
                        "HandleNetworkClientStateChanged",
                        BindingFlags.Instance |
                        BindingFlags.NonPublic);

                Assert.That(
                    stateHandler,
                    Is.Not.Null);

                stateHandler.Invoke(
                    menu,
                    new object[]
                    {
                NetworkClientState.InRoom
                    });

                var snapshot =
                    new RoomStateSnapshot(
                        "ROOM-ENDPOINT",
                        RoomStateStatus.Waiting,
                        DifficultyId.Normal,
                        new[]
                        {
                    new RoomPlayerSnapshot(
                        7u,
                        true,
                        false,
                        CharacterId.Ranged,
                        "Host")
                        });

                client.EmitRoomState(
                    7u,
                    snapshot);

                Assert.That(
                    addressText.text,
                    Is.EqualTo("[::1]:7777"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(
                    lobbyObject);

                UnityEngine.Object.DestroyImmediate(
                    menuObject);
            }
        }

        [Test]
        public void ErrorReceived_ShowsServerRoomError()
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
                new GameObject("RoomErrorMenuTests");

            var validationObject =
                new GameObject("RoomErrorValidationText");

            var client =
                new RecordingRoomNetworkClient();

            try
            {
                Component menu =
                    menuObject.AddComponent(menuType);

                TMP_Text validationText =
                    validationObject.AddComponent<TextMeshProUGUI>();

                SetField(
                    menuType,
                    menu,
                    "networkClient",
                    client);

                SetField(
                    menuType,
                    menu,
                    "validationText",
                    validationText);

                MethodInfo stateHandler =
                    menuType.GetMethod(
                        "HandleNetworkClientStateChanged",
                        BindingFlags.Instance |
                        BindingFlags.NonPublic);

                Assert.That(
                    stateHandler,
                    Is.Not.Null);

                stateHandler.Invoke(
                    menu,
                    new object[]
                    {
                NetworkClientState.InRoom
                    });

                client.EmitError(
                    "Room is full.");

                Assert.That(
                    validationText.text,
                    Is.EqualTo("Room is full."));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(
                    validationObject);

                UnityEngine.Object.DestroyImmediate(
                    menuObject);
            }
        }

        [Test]
        public void HandleLeaveRoom_SendsLeaveAndDisconnectsClient()
        {
            Type menuType =
                FindType(
                    "TopDownRoguelike.Menu.UI." +
                    "MultiplayerMenuView");

            Assert.That(menuType, Is.Not.Null);

            var menuObject =
                new GameObject("LeaveRoomMenuTests");

            var lobbyPanel =
                new GameObject("RoomLobbyPanel");

            var entryPanel =
                new GameObject("MultiplayerEntryPanel");

            var client =
                new RecordingRoomNetworkClient();

            client.SetState(
                NetworkClientState.InRoom);

            try
            {
                Component menu =
                    menuObject.AddComponent(menuType);

                SetField(
                    menuType,
                    menu,
                    "networkClient",
                    client);

                SetField(
                    menuType,
                    menu,
                    "roomLobbyPanel",
                    lobbyPanel);

                SetField(
                    menuType,
                    menu,
                    "multiplayerEntryPanel",
                    entryPanel);

                lobbyPanel.SetActive(true);
                entryPanel.SetActive(false);

                MethodInfo leaveMethod =
                    menuType.GetMethod(
                        "HandleLeaveRoom",
                        BindingFlags.Instance |
                        BindingFlags.Public);

                Assert.That(
                    leaveMethod,
                    Is.Not.Null);

                leaveMethod.Invoke(
                    menu,
                    null);

                Assert.That(
                    client.LeaveRoomCallCount,
                    Is.EqualTo(1),
                    "Leaving a room must send LeaveRoom.");

                Assert.That(
                    client.DisconnectCallCount,
                    Is.EqualTo(1),
                    "Leaving a room must disconnect the client.");

                Assert.That(
                    client.State,
                    Is.EqualTo(
                        NetworkClientState.Disconnected),
                    "Leaving a room must leave the client disconnected.");

                Assert.That(
                    lobbyPanel.activeSelf,
                    Is.False);

                Assert.That(
                    entryPanel.activeSelf,
                    Is.True);

                FieldInfo addressField =
                    menuType.GetField(
                        "activeConnectionAddress",
                        BindingFlags.Instance |
                        BindingFlags.NonPublic);

                FieldInfo portField =
                    menuType.GetField(
                        "activeConnectionPort",
                        BindingFlags.Instance |
                        BindingFlags.NonPublic);

                Assert.That(
                    addressField,
                    Is.Not.Null);

                Assert.That(
                    portField,
                    Is.Not.Null);

                Assert.That(
                    addressField.GetValue(menu),
                    Is.EqualTo(string.Empty));

                Assert.That(
                    portField.GetValue(menu),
                    Is.EqualTo(0));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(
                    entryPanel);

                UnityEngine.Object.DestroyImmediate(
                    lobbyPanel);

                UnityEngine.Object.DestroyImmediate(
                    menuObject);
            }
        }

        [Test]
        public void ConnectedAfterRoomState_ReturnsToMultiplayerEntry()
        {
            Type menuType =
                FindType(
                    "TopDownRoguelike.Menu.UI." +
                    "MultiplayerMenuView");

            Assert.That(menuType, Is.Not.Null);

            var menuObject =
                new GameObject("RemoteLeaveMenuTests");

            var lobbyPanel =
                new GameObject("RoomLobbyPanel");

            var entryPanel =
                new GameObject("MultiplayerEntryPanel");

            var client =
                new RecordingRoomNetworkClient();

            try
            {
                Component menu =
                    menuObject.AddComponent(menuType);

                SetField(
                    menuType,
                    menu,
                    "networkClient",
                    client);

                SetField(
                    menuType,
                    menu,
                    "roomLobbyPanel",
                    lobbyPanel);

                SetField(
                    menuType,
                    menu,
                    "multiplayerEntryPanel",
                    entryPanel);

                lobbyPanel.SetActive(true);
                entryPanel.SetActive(false);

                MethodInfo stateHandler =
                    menuType.GetMethod(
                        "HandleNetworkClientStateChanged",
                        BindingFlags.Instance |
                        BindingFlags.NonPublic);

                Assert.That(
                    stateHandler,
                    Is.Not.Null);

                stateHandler.Invoke(
                    menu,
                    new object[]
                    {
                NetworkClientState.InRoom
                    });

                stateHandler.Invoke(
                    menu,
                    new object[]
                    {
                        NetworkClientState.Connected
                    });

                Assert.That(
                    lobbyPanel.activeSelf,
                    Is.False,
                    "Remote room closure must hide the Lobby.");

                Assert.That(
                    entryPanel.activeSelf,
                    Is.True,
                    "Remote room closure must show the multiplayer entry.");

                Assert.That(
                    client.DisconnectCallCount,
                    Is.EqualTo(1),
                    "Remote room closure must disconnect " +
                    "the old server connection.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(
                    entryPanel);

                UnityEngine.Object.DestroyImmediate(
                    lobbyPanel);

                UnityEngine.Object.DestroyImmediate(
                    menuObject);
            }
        }

        [Test]
        public void DestroyMenu_DisconnectsNetworkClient()
        {
            Type menuType =
                FindType(
                    "TopDownRoguelike.Menu.UI." +
                    "MultiplayerMenuView");

            Assert.That(menuType, Is.Not.Null);

            var menuObject =
                new GameObject("DestroyMenuNetworkCleanupTests");

            var client =
                new RecordingRoomNetworkClient();

            try
            {
                Component menu =
                    menuObject.AddComponent(menuType);

                SetField(
                    menuType,
                    menu,
                    "networkClient",
                    client);

                MethodInfo onDestroyMethod =
                    menuType.GetMethod(
                        "OnDestroy",
                        BindingFlags.Instance |
                        BindingFlags.NonPublic);

                Assert.That(
                    onDestroyMethod,
                    Is.Not.Null,
                    "MultiplayerMenuView must define OnDestroy().");

                onDestroyMethod.Invoke(
                    menu,
                    null);

                Assert.That(
                    client.DisconnectCallCount,
                    Is.EqualTo(1),
                    "OnDestroy must disconnect " +
                    "the network client.");
            }
            finally
            {
                if (menuObject != null)
                {
                    UnityEngine.Object.DestroyImmediate(
                        menuObject);
                }
            }
        }

        [Test]
        public void GameStarted_DisablesLobbyActionsAndShowsConfirmation()
        {
            Type menuType =
                FindType(
                    "TopDownRoguelike.Menu.UI." +
                    "MultiplayerMenuView");

            Type lobbyType =
                FindType("RoomLobbyView");

            Assert.That(menuType, Is.Not.Null);
            Assert.That(lobbyType, Is.Not.Null);

            var menuObject =
                new GameObject("GameStartedMenuTests");

            var lobbyObject =
                new GameObject("GameStartedLobbyTests");

            var messageObject =
                new GameObject("RoomMessageText");

            var readyButtonObject =
                new GameObject("ReadyButton");

            var startButtonObject =
                new GameObject("StartButton");

            messageObject.transform.SetParent(
                lobbyObject.transform);

            readyButtonObject.transform.SetParent(
                lobbyObject.transform);

            startButtonObject.transform.SetParent(
                lobbyObject.transform);

            var client =
                new RecordingRoomNetworkClient();

            try
            {
                Component menu =
                    menuObject.AddComponent(menuType);

                Component lobby =
                    lobbyObject.AddComponent(lobbyType);

                TMP_Text messageText =
                    messageObject.AddComponent<TextMeshProUGUI>();

                Button readyButton =
                    readyButtonObject.AddComponent<Button>();

                Button startButton =
                    startButtonObject.AddComponent<Button>();

                SetField(
                    menuType,
                    menu,
                    "networkClient",
                    client);

                SetField(
                    menuType,
                    menu,
                    "roomLobbyView",
                    lobby);

                SetField(
                    lobbyType,
                    lobby,
                    "roomMessageText",
                    messageText);

                SetField(
                    lobbyType,
                    lobby,
                    "readyButton",
                    readyButton);

                SetField(
                    lobbyType,
                    lobby,
                    "startGameButton",
                    startButton);

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
                NetworkClientState.InRoom
                    });

                var players =
                    new List<RoomPlayerSnapshot>
                    {
                new RoomPlayerSnapshot(
                    7u,
                    true,
                    true,
                    CharacterId.Ranged,
                    "Host"),
                new RoomPlayerSnapshot(
                    8u,
                    false,
                    true,
                    CharacterId.Ranged,
                    "Client")
                    };

                var snapshot =
                    new RoomStateSnapshot(
                        "ROOM-START",
                        RoomStateStatus.Waiting,
                        DifficultyId.Normal,
                        players);

                client.EmitRoomState(
                    7u,
                    snapshot);

                Assert.That(
                    readyButton.interactable,
                    Is.True);

                Assert.That(
                    startButton.interactable,
                    Is.True);

                client.EmitGameStarted();

                Assert.That(
                    readyButton.interactable,
                    Is.False);

                Assert.That(
                    startButton.interactable,
                    Is.False);

                Assert.That(
                    messageText.text,
                    Is.EqualTo(
                        "服务器已确认开始游戏"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(
                    lobbyObject);

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

                    inputType.GetProperty("text").SetValue(
                        nicknameInput,
                        " Bronya ");

                    inputType.GetProperty("text").SetValue(
                        addressInput,
                        " ::1 ");

                    inputType.GetProperty("text").SetValue(
                        portInput,
                        " 7777 ");

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
                }
                finally
                {
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
        public void HandleCreateRoom_AutomaticStartupFailure_DoesNotConnect()
        {
            Type menuType =
                FindType(
                    "TopDownRoguelike.Menu.UI." +
                    "MultiplayerMenuView");

            Type inputType =
                FindType("TMPro.TMP_InputField");

            var menuObject =
                new GameObject("MultiplayerMenuViewTests");

            var inputObject =
                new GameObject("NicknameInput");

            var launcherObject =
                new GameObject("ServerProcessLauncherTests");

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

                    var launcher =
                        launcherObject.AddComponent<
                            ServerProcessLauncher>();

                    inputType.GetProperty("text").SetValue(
                        nicknameInput,
                        "Seele");

                    SetField(
                        launcher.GetType(),
                        launcher,
                        "startupMode",
                        ServerStartupMode.Automatic);

                    SetField(
                        launcher.GetType(),
                        launcher,
                        "executableFileName",
                        Path.Combine(
                            Path.GetTempPath(),
                            Guid.NewGuid().ToString("N") +
                            ".exe"));

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
                        "serverProcessLauncher",
                        launcher);

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
                        Is.Zero,
                        "Client must not connect when automatic " +
                        "server startup fails.");
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(
                        launcherObject);

                    UnityEngine.Object.DestroyImmediate(
                        inputObject);

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

            Assert.That(
                menuType.GetField(
                    "roomIdInput",
                    BindingFlags.Instance |
                    BindingFlags.NonPublic),
                Is.Null,
                "MultiplayerMenuView must not require " +
                "a Room ID input.");
        }

        [Test]
        public void MainMenuScene_DoesNotContainRoomIdControls()
        {
            string scenePath =
                Path.Combine(
                    Application.dataPath,
                    "Scenes",
                    "MainMenu.unity");

            string sceneYaml =
                File.ReadAllText(
                    scenePath);

            Assert.That(
                sceneYaml,
                Does.Not.Contain(
                    "m_Name: RoomIdLabel"),
                "MainMenu must not contain RoomIdLabel.");

            Assert.That(
                sceneYaml,
                Does.Not.Contain(
                    "m_Name: RoomIdInput"),
                "MainMenu must not contain RoomIdInput.");

            Assert.That(
                sceneYaml,
                Does.Not.Contain(
                    "roomIdInput:"),
                "MainMenu must not contain a stale " +
                "roomIdInput reference.");
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

        [Test]
        public void RoomLobbySelection_SendsCharacterAndHostDifficulty()
        {
            Type lobbyType =
                FindType("RoomLobbyView");

            var lobbyObject =
                new GameObject("RoomLobbySelectionTests");

            try
            {
                Component lobby =
                    lobbyObject.AddComponent(lobbyType);

                var client =
                    new RecordingRoomNetworkClient();

                MethodInfo bindMethod =
                    lobbyType.GetMethod(
                        "BindNetworkClient",
                        BindingFlags.Instance |
                        BindingFlags.Public);

                Assert.That(
                    bindMethod,
                    Is.Not.Null,
                    "RoomLobbyView must expose BindNetworkClient().");

                bindMethod.Invoke(
                    lobby,
                    new object[]
                    {
                client
                    });

                var snapshot =
                    new RoomStateSnapshot(
                        "ROOM-1",
                        RoomStateStatus.Waiting,
                        DifficultyId.Normal,
                        new List<RoomPlayerSnapshot>
                        {
                    new RoomPlayerSnapshot(
                        7u,
                        true,
                        false,
                        CharacterId.None,
                        "Host")
                        });

                lobbyType.GetMethod(
                    "ApplyNetworkRoomState").Invoke(
                        lobby,
                        new object[]
                        {
                    snapshot,
                    7u
                        });

                MethodInfo selectMethod =
                    lobbyType.GetMethod(
                        "SelectLocalRangedCharacter",
                        BindingFlags.Instance |
                        BindingFlags.NonPublic);

                Assert.That(selectMethod, Is.Not.Null);

                selectMethod.Invoke(
                    lobby,
                    null);

                Assert.That(
                    client.SetPlayerSelectionCallCount,
                    Is.EqualTo(1));

                Assert.That(
                    client.LastCharacter,
                    Is.EqualTo(CharacterId.Ranged));

                Assert.That(
                    client.LastDifficulty,
                    Is.EqualTo(DifficultyId.Normal));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(
                    lobbyObject);
            }
        }

        [Test]
        public void RoomLobbyReady_SendsReadyRequest()
        {
            Type lobbyType =
                FindType("RoomLobbyView");

            var lobbyObject =
                new GameObject("RoomLobbyReadyTests");

            try
            {
                Component lobby =
                    lobbyObject.AddComponent(lobbyType);

                var client =
                    new RecordingRoomNetworkClient();

                lobbyType.GetMethod(
                    "BindNetworkClient").Invoke(
                        lobby,
                        new object[]
                        {
                    client
                        });

                var snapshot =
                    new RoomStateSnapshot(
                        "ROOM-1",
                        RoomStateStatus.Waiting,
                        DifficultyId.Normal,
                        new List<RoomPlayerSnapshot>
                        {
                    new RoomPlayerSnapshot(
                        7u,
                        true,
                        false,
                        CharacterId.Ranged,
                        "Host")
                        });

                lobbyType.GetMethod(
                    "ApplyNetworkRoomState").Invoke(
                        lobby,
                        new object[]
                        {
                    snapshot,
                    7u
                        });

                lobbyType.GetMethod(
                    "ToggleLocalPlayerReady").Invoke(
                        lobby,
                        null);

                Assert.That(
                    client.SetReadyCallCount,
                    Is.EqualTo(1),
                    "Ready button must send through " +
                    "the network client.");

                Assert.That(
                    client.LastReady,
                    Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(
                    lobbyObject);
            }
        }

        [Test]
        public void RoomLobbyStartGame_SendsStartRequest()
        {
            Type lobbyType =
                FindType("RoomLobbyView");

            var lobbyObject =
                new GameObject("RoomLobbyStartGameTests");

            try
            {
                Component lobby =
                    lobbyObject.AddComponent(lobbyType);

                var client =
                    new RecordingRoomNetworkClient();

                lobbyType.GetMethod(
                    "BindNetworkClient").Invoke(
                        lobby,
                        new object[]
                        {
                    client
                        });

                var snapshot =
                    new RoomStateSnapshot(
                        "ROOM-1",
                        RoomStateStatus.Waiting,
                        DifficultyId.Normal,
                        new List<RoomPlayerSnapshot>
                        {
                    new RoomPlayerSnapshot(
                        7u,
                        true,
                        true,
                        CharacterId.Ranged,
                        "Host"),
                    new RoomPlayerSnapshot(
                        8u,
                        false,
                        true,
                        CharacterId.Ranged,
                        "Client")
                        });

                lobbyType.GetMethod(
                    "ApplyNetworkRoomState").Invoke(
                        lobby,
                        new object[]
                        {
                    snapshot,
                    7u
                        });

                lobbyType.GetMethod(
                    "HandleStartGamePrototype").Invoke(
                        lobby,
                        null);

                Assert.That(
                    client.StartGameCallCount,
                    Is.EqualTo(1),
                    "Host start button must send through " +
                    "the network client.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(
                    lobbyObject);
            }
        }

        [Test]
        public void RoomLobbyDifficulty_SendsHostCharacterAndNormalDifficulty()
        {
            Type lobbyType =
                FindType("RoomLobbyView");

            var lobbyObject =
                new GameObject("RoomLobbyDifficultyTests");

            try
            {
                Component lobby =
                    lobbyObject.AddComponent(lobbyType);

                var client =
                    new RecordingRoomNetworkClient();

                lobbyType.GetMethod(
                    "BindNetworkClient").Invoke(
                        lobby,
                        new object[]
                        {
                    client
                        });

                var snapshot =
                    new RoomStateSnapshot(
                        "ROOM-1",
                        RoomStateStatus.Waiting,
                        DifficultyId.None,
                        new List<RoomPlayerSnapshot>
                        {
                    new RoomPlayerSnapshot(
                        7u,
                        true,
                        false,
                        CharacterId.Ranged,
                        "Host")
                        });

                lobbyType.GetMethod(
                    "ApplyNetworkRoomState").Invoke(
                        lobby,
                        new object[]
                        {
                    snapshot,
                    7u
                        });

                MethodInfo selectDifficultyMethod =
                    lobbyType.GetMethod(
                        "SelectLocalNormalDifficulty",
                        BindingFlags.Instance |
                        BindingFlags.NonPublic);

                Assert.That(
                    selectDifficultyMethod,
                    Is.Not.Null,
                    "RoomLobbyView must handle the " +
                    "normal difficulty card.");

                selectDifficultyMethod.Invoke(
                    lobby,
                    null);

                Assert.That(
                    client.SetPlayerSelectionCallCount,
                    Is.EqualTo(1));

                Assert.That(
                    client.LastCharacter,
                    Is.EqualTo(CharacterId.Ranged));

                Assert.That(
                    client.LastDifficulty,
                    Is.EqualTo(DifficultyId.Normal));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(
                    lobbyObject);
            }
        }

        private sealed class RecordingRoomNetworkClient
            : IRoomNetworkClient
        {
            public int SetPlayerSelectionCallCount
            {
                get;
                private set;
            }

            public CharacterId LastCharacter
            {
                get;
                private set;
            }

            public DifficultyId LastDifficulty
            {
                get;
                private set;
            }

            public int SetReadyCallCount
            {
                get;
                private set;
            }

            public bool LastReady
            {
                get;
                private set;
            }

            public int StartGameCallCount
            {
                get;
                private set;
            }

            public event Action<NetworkClientState>
                StateChanged;

            public event Action<string>
                ErrorReceived;

            public event Action<RoomStateSnapshot>
                RoomStateChanged;

            public event Action
                GameStarted;

            public uint PlayerId
            {
                get;
                private set;
            }

            public string CurrentRoomId
            {
                get;
                private set;
            } = string.Empty;

            public RoomStateSnapshot CurrentRoomState
            {
                get;
                private set;
            }

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

            public int LeaveRoomCallCount
            {
                get;
                private set;
            }

            public string LastNickname
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
                string nickname)
            {
                JoinRoomCallCount++;
                LastNickname = nickname;
            }

            public void SetPlayerSelection(
                CharacterId character,
                DifficultyId difficulty)
            {
                SetPlayerSelectionCallCount++;
                LastCharacter = character;
                LastDifficulty = difficulty;
            }

            public void SetReady(
                bool ready)
            {
                SetReadyCallCount++;
                LastReady = ready;
            }

            public void StartGame()
            {
                StartGameCallCount++;
            }

            public void LeaveRoom()
            {
                LeaveRoomCallCount++;
            }

            public void Disconnect()
            {
                DisconnectCallCount++;

                State =
                    NetworkClientState.Disconnected;

                StateChanged?.Invoke(
                    State);
            }

            public void EmitRoomState(
                uint playerId,
                RoomStateSnapshot snapshot)
            {
                PlayerId =
                    playerId;

                CurrentRoomId =
                    snapshot.RoomId;

                CurrentRoomState =
                    snapshot;

                RoomStateChanged?.Invoke(
                    snapshot);
            }

            public void EmitGameStarted()
            {
                GameStarted?.Invoke();
            }

            public void EmitError(
                string message)
            {
                ErrorReceived?.Invoke(
                    message);
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