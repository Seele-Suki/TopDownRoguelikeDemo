using TMPro;
using UnityEngine;
using TopDownRoguelike.Networking.Client;
using TopDownRoguelike.Networking.Protocol;
using TopDownRoguelike.Infrastructure;
using System;

namespace TopDownRoguelike.Menu.UI
{
    public sealed class MultiplayerMenuView : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private GameObject mainMenuPanel;
        [SerializeField] private GameObject multiplayerEntryPanel;
        [SerializeField] private GameObject joinFields;
        [SerializeField] private GameObject roomLobbyPanel;

        [Header("Room Lobby")]
        [SerializeField] private RoomLobbyView roomLobbyView;

        [Header("Scene Transition")]
        [SerializeField]
        private SceneLoader sceneLoader;

        [Header("Connection")]
        [SerializeField]
        private ConnectionStatusView connectionStatusView;

        [SerializeField]
        private NetworkClientBehaviour networkClientBehaviour;

        [SerializeField]
        private ServerProcessLauncher serverProcessLauncher;

        [SerializeField]
        private string hostAddress = "::1";

        [SerializeField]
        [Range(1, 65535)]
        private int hostPort = 7777;

        private bool isConnecting;
        private IRoomNetworkClient networkClient;
        private RoomConnectionFlow connectionFlow;

        private string activeConnectionAddress =
            string.Empty;

        private int activeConnectionPort;

        private bool isTransitioningToGameplay;

        [Header("Input")]
        [SerializeField] private TMP_InputField nicknameInput;
        [SerializeField] private TMP_InputField addressInput;
        [SerializeField] private TMP_InputField portInput;

        [Header("Message")]
        [SerializeField] private TMP_Text validationText;

        private void OnDestroy()
        {
            if (networkClient != null)
            {
                if (!isTransitioningToGameplay)
                {
                    networkClient.Disconnect();
                }

                networkClient.StateChanged -=
                    HandleNetworkClientStateChanged;

                networkClient.RoomStateChanged -=
                    HandleRoomStateChanged;

                networkClient.GameStarted -=
                    HandleGameStarted;

                networkClient.ErrorReceived -=
                    HandleNetworkError;
            }

            connectionFlow?.Dispose();

            connectionFlow =
                null;

            networkClient =
                null;
        }

        private void Update()
        {
            if (!Input.GetKeyDown(KeyCode.Escape))
            {
                return;
            }

            if (roomLobbyPanel != null &&
                roomLobbyPanel.activeSelf)
            {
                HandleLeaveRoom();
                return;
            }

            if (multiplayerEntryPanel != null &&
                multiplayerEntryPanel.activeSelf)
            {
                HandleBack();
            }
        }

        public void OpenEntryPanel()
        {
            RefreshHostAddress();

            if (roomLobbyPanel != null)
            {
                roomLobbyPanel.SetActive(false);
            }

            if (mainMenuPanel != null)
            {
                mainMenuPanel.SetActive(false);
            }

            if (multiplayerEntryPanel != null)
            {
                multiplayerEntryPanel.SetActive(true);
            }

            if (joinFields != null)
            {
                joinFields.SetActive(false);
            }

            ClearValidation();

            if (nicknameInput != null)
            {
                nicknameInput.Select();
                nicknameInput.ActivateInputField();
            }
        }

        public void HandleCreateRoom()
        {
            RefreshHostAddress();
            RoomConnectionRequest request;

            try
            {
                request =
                    RoomConnectionRequest.CreateHost(
                        nicknameInput == null
                            ? null
                            : nicknameInput.text,
                        hostAddress,
                        hostPort);
            }
            catch (ArgumentException exception)
            {
                ShowValidation(
                    exception.Message);

                if (exception.ParamName ==
                        "nickname" &&
                    nicknameInput != null)
                {
                    nicknameInput.Select();
                    nicknameInput.ActivateInputField();
                }

                return;
            }

            if (isConnecting)
            {
                return;
            }

            if (!EnsureConnectionFlow())
            {
                return;
            }

            ClearValidation();

            if (joinFields != null)
            {
                joinFields.SetActive(false);
            }

            activeConnectionAddress =
                request.Address;

            activeConnectionPort =
                request.Port;

            try
            {
                serverProcessLauncher?.PrepareForHost();

                isConnecting =
                    true;

                connectionStatusView?.ShowConnecting(
                    request.Address,
                    request.Port);

                connectionFlow.BeginHost(
                    request);
            }
            catch (Exception exception)
            {
                isConnecting =
                    false;

                if (connectionStatusView != null)
                {
                    connectionStatusView.ShowFailure(
                        exception.Message);
                }
                else
                {
                    ShowValidation(
                        exception.Message);
                }
            }
        }

        private void RefreshHostAddress()
        {
            hostAddress =
                LocalIpv6AddressResolver.ResolveLocalAddressOrLoopback();
        }

        public void HandleJoinRoom()
        {
            RoomConnectionRequest request;

            try
            {
                request =
                    RoomConnectionRequest.CreateJoin(
                        nicknameInput == null
                            ? null
                            : nicknameInput.text,
                        addressInput == null
                            ? null
                            : addressInput.text,
                        portInput == null
                            ? null
                            : portInput.text);
            }
            catch (ArgumentException exception)
            {
                ShowValidation(
                    exception.Message);

                FocusInputForParameter(
                    exception.ParamName);

                return;
            }

            if (isConnecting)
            {
                return;
            }

            if (!EnsureConnectionFlow())
            {
                return;
            }

            ClearValidation();

            activeConnectionAddress =
                request.Address;

            activeConnectionPort =
                request.Port;

            isConnecting =
                true;

            connectionStatusView?.ShowConnecting(
                request.Address,
                request.Port);

            try
            {
                connectionFlow.BeginJoin(
                    request);
            }
            catch (Exception exception)
            {
                isConnecting =
                    false;

                if (connectionStatusView != null)
                {
                    connectionStatusView.ShowFailure(
                        exception.Message);
                }
                else
                {
                    ShowValidation(
                        exception.Message);
                }
            }
        }

        public void HandleConnectionFailureClosed()
        {
            connectionStatusView?.Hide();

            isConnecting =
                false;

            if (networkClient == null ||
                networkClient.State !=
                    NetworkClientState.Error)
            {
                return;
            }

            networkClient.Disconnect();
        }

        public void ToggleJoinFields()
        {
            if (joinFields == null)
            {
                return;
            }

            bool shouldOpen = !joinFields.activeSelf;
            joinFields.SetActive(shouldOpen);

            ClearValidation();
        }

        public void HandleLeaveRoom()
        {
            if (networkClient != null)
            {
                if (networkClient.State ==
                    NetworkClientState.InRoom)
                {
                    try
                    {
                        networkClient.LeaveRoom();
                    }
                    catch (Exception exception)
                    {
                        ShowValidation(
                            "退出房间请求失败：" +
                            exception.Message);
                    }
                }

                networkClient.Disconnect();
            }

            if (roomLobbyView != null)
            {
                roomLobbyView.ResetLocalRoom();
            }

            activeConnectionAddress =
                string.Empty;

            activeConnectionPort =
                0;

            if (roomLobbyPanel != null)
            {
                roomLobbyPanel.SetActive(false);
            }

            if (joinFields != null)
            {
                joinFields.SetActive(false);
            }

            if (multiplayerEntryPanel != null)
            {
                multiplayerEntryPanel.SetActive(true);
            }

            ClearValidation();

            if (nicknameInput != null)
            {
                nicknameInput.Select();
                nicknameInput.ActivateInputField();
            }
        }

        public void HandleBack()
        {
            if (joinFields != null &&
                joinFields.activeSelf)
            {
                joinFields.SetActive(false);
                ClearValidation();
                return;
            }

            CloseEntryPanel();
        }

        public void CloseEntryPanel()
        {
            if (joinFields != null)
            {
                joinFields.SetActive(false);
            }

            if (multiplayerEntryPanel != null)
            {
                multiplayerEntryPanel.SetActive(false);
            }

            if (mainMenuPanel != null)
            {
                mainMenuPanel.SetActive(true);
            }

            ClearValidation();
        }

        private void HandleNetworkClientStateChanged(
            NetworkClientState state)
        {
            if (state ==
                NetworkClientState.Error)
            {
                isConnecting =
                    false;

                string errorMessage =
                    networkClient == null ||
                    string.IsNullOrWhiteSpace(
                        networkClient.LastError)
                        ? "Unknown network error."
                        : networkClient.LastError;

                if (connectionStatusView != null)
                {
                    connectionStatusView.ShowFailure(
                        errorMessage);
                }
                else
                {
                    ShowValidation(
                        errorMessage);
                }

                return;
            }

            if (state ==
                    NetworkClientState.Connected &&
                roomLobbyPanel != null &&
                roomLobbyPanel.activeSelf)
            {
                if (networkClient != null)
                {
                    networkClient.Disconnect();
                }

                if (roomLobbyView != null)
                {
                    roomLobbyView.ResetLocalRoom();
                }

                roomLobbyPanel.SetActive(false);

                if (joinFields != null)
                {
                    joinFields.SetActive(false);
                }

                if (multiplayerEntryPanel != null)
                {
                    multiplayerEntryPanel.SetActive(true);
                }

                ClearValidation();

                return;
            }

            if (state !=
                NetworkClientState.InRoom)
            {
                return;
            }

            isConnecting =
                false;

            if (networkClient != null)
            {
                networkClient.RoomStateChanged -=
                    HandleRoomStateChanged;

                networkClient.RoomStateChanged +=
                    HandleRoomStateChanged;

                networkClient.GameStarted -=
                    HandleGameStarted;

                networkClient.GameStarted +=
                    HandleGameStarted;

                networkClient.ErrorReceived -=
                    HandleNetworkError;

                networkClient.ErrorReceived +=
                    HandleNetworkError;
            }

            if (roomLobbyView != null)
            {
                roomLobbyView.SetConnectionEndpoint(
                    activeConnectionAddress,
                    activeConnectionPort);
            }

            connectionStatusView?.Hide();

            ClearValidation();

            if (joinFields != null)
            {
                joinFields.SetActive(false);
            }

            if (multiplayerEntryPanel != null)
            {
                multiplayerEntryPanel.SetActive(false);
            }

            if (roomLobbyPanel != null)
            {
                roomLobbyPanel.SetActive(true);
            }
        }

        private void HandleRoomStateChanged(
            RoomStateSnapshot snapshot)
        {
            if (roomLobbyView == null ||
                networkClient == null)
            {
                return;
            }

            roomLobbyView.BindNetworkClient(
                networkClient);

            roomLobbyView.ApplyNetworkRoomState(
                snapshot,
                networkClient.PlayerId);
        }

        private void HandleGameStarted()
        {
            roomLobbyView?.HandleGameStarted();

            if (!TryPrepareGameplaySession())
            {
                return;
            }

            if (sceneLoader == null)
            {
                ShowValidation(
                    "SceneLoader 未配置，无法进入游戏。");

                return;
            }

            isTransitioningToGameplay = true;

            sceneLoader.LoadGameplayScene();
        }

        private bool TryPrepareGameplaySession()
        {
            if (networkClient == null)
            {
                ShowValidation(
                    "网络客户端不存在，无法进入游戏。");

                return false;
            }

            RoomStateSnapshot snapshot =
                networkClient.CurrentRoomState;

            uint localPlayerId =
                networkClient.PlayerId;

            if (snapshot == null ||
                localPlayerId == 0u)
            {
                ShowValidation(
                    "房间或玩家身份数据不完整。");

                return false;
            }

            RoomPlayerSnapshot localPlayer = null;

            foreach (RoomPlayerSnapshot player
                in snapshot.Players)
            {
                if (player.PlayerId == localPlayerId)
                {
                    localPlayer = player;
                    break;
                }
            }

            if (localPlayer == null)
            {
                ShowValidation(
                    "房间中找不到本地玩家。");

                return false;
            }

            if (localPlayer.Character ==
                    CharacterId.None ||
                snapshot.SelectedDifficulty ==
                    DifficultyId.None)
            {
                ShowValidation(
                    "角色或难度数据不完整。");

                return false;
            }

            if (localPlayer.IsHost)
            {
                GameSession.ConfigureMultiplayerHost();
            }
            else
            {
                GameSession.ConfigureMultiplayerClient();
            }

            GameSession.SelectCharacter(
                localPlayer.Character);

            GameSession.SelectDifficulty(
                snapshot.SelectedDifficulty);

            return true;
        }

        private void HandleNetworkError(
            string message)
        {
            isConnecting =
                false;

            ShowValidation(
                string.IsNullOrWhiteSpace(message)
                    ? "服务器返回未知错误。"
                    : message);
        }

        private void FocusInputForParameter(
            string parameterName)
        {
            TMP_InputField targetInput = null;

            switch (parameterName)
            {
                case "nickname":
                    targetInput = nicknameInput;
                    break;

                case "address":
                    targetInput = addressInput;
                    break;

                case "portText":
                    targetInput = portInput;
                    break;
            }

            if (targetInput == null)
            {
                return;
            }

            targetInput.Select();
            targetInput.ActivateInputField();
        }

        private bool EnsureConnectionFlow()
        {
            if (connectionFlow != null)
            {
                return true;
            }

            NetworkClientBehaviour behaviour =
                networkClientBehaviour != null
                    ? networkClientBehaviour
                    : NetworkClientBehaviour.Instance;

            if (behaviour == null ||
                behaviour.Client == null)
            {
                ShowValidation(
                    "网络客户端尚未初始化。");

                return false;
            }

            networkClient =
                behaviour.Client;

            connectionFlow =
                new RoomConnectionFlow(
                    networkClient);

            networkClient.StateChanged +=
                HandleNetworkClientStateChanged;

            return true;
        }

        private void ShowValidation(string message)
        {
            if (validationText != null)
            {
                validationText.text = message;
            }
        }

        private void ClearValidation()
        {
            if (validationText != null)
            {
                validationText.text = string.Empty;
            }
        }
    }
}