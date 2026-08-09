using System.Collections;
using System.Net;
using TMPro;
using UnityEngine;

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

        [Header("Connection Simulation")]
        [SerializeField]
        private ConnectionStatusView connectionStatusView;

        [SerializeField]
        [Min(0f)]
        private float simulatedConnectionDelay = 0.8f;

        [SerializeField]
        private bool simulateConnectionFailure;

        private bool isConnecting;

        [Header("Input")]
        [SerializeField] private TMP_InputField nicknameInput;
        [SerializeField] private TMP_InputField addressInput;
        [SerializeField] private TMP_InputField portInput;

        [Header("Message")]
        [SerializeField] private TMP_Text validationText;

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
            if (nicknameInput == null)
            {
                ShowValidation("未配置昵称输入框");
                return;
            }

            string nickname = nicknameInput.text.Trim();

            if (string.IsNullOrWhiteSpace(nickname))
            {
                ShowValidation("请输入玩家昵称");
                nicknameInput.Select();
                nicknameInput.ActivateInputField();
                return;
            }

            if (roomLobbyPanel == null ||
                roomLobbyView == null)
            {
                ShowValidation("房间准备面板未正确配置");
                return;
            }

            ClearValidation();

            if (joinFields != null)
            {
                joinFields.SetActive(false);
            }

            if (multiplayerEntryPanel != null)
            {
                multiplayerEntryPanel.SetActive(false);
            }

            roomLobbyPanel.SetActive(true);
            roomLobbyView.CreateLocalHostRoom(nickname);
        }

        public void HandleJoinRoom()
        {
            if (nicknameInput == null ||
                addressInput == null ||
                portInput == null)
            {
                ShowValidation("联机输入框未正确配置");
                return;
            }

            string nickname = nicknameInput.text.Trim();
            string address = addressInput.text.Trim();
            string portText = portInput.text.Trim();

            if (string.IsNullOrWhiteSpace(nickname))
            {
                ShowValidation("请输入玩家昵称");
                nicknameInput.Select();
                nicknameInput.ActivateInputField();
                return;
            }

            if (string.IsNullOrWhiteSpace(address))
            {
                ShowValidation("请输入房主 IP 地址");
                addressInput.Select();
                addressInput.ActivateInputField();
                return;
            }

            if (!IPAddress.TryParse(address, out _))
            {
                ShowValidation("IP 地址格式不正确");
                addressInput.Select();
                addressInput.ActivateInputField();
                return;
            }

            if (!int.TryParse(portText, out int port) ||
                port < 1 ||
                port > 65535)
            {
                ShowValidation("端口必须在 1 到 65535 之间");
                portInput.Select();
                portInput.ActivateInputField();
                return;
            }

            if (roomLobbyPanel == null ||
                roomLobbyView == null)
            {
                ShowValidation("房间准备面板未正确配置");
                return;
            }

            if (connectionStatusView == null)
            {
                ShowValidation("连接状态面板未正确配置");
                return;
            }

            if (isConnecting)
            {
                return;
            }

            StartCoroutine(
                SimulateJoinRoom(
                    nickname,
                    address,
                    port));
        }

        private IEnumerator SimulateJoinRoom(
            string nickname,
            string address,
            int port)
        {
            isConnecting = true;

            connectionStatusView.ShowConnecting(
                address,
                port);

            yield return new WaitForSecondsRealtime(
                simulatedConnectionDelay);

            if (simulateConnectionFailure)
            {
                isConnecting = false;

                connectionStatusView.ShowFailure(
                    "无法连接到房主，请检查地址、端口和网络状态。");

                yield break;
            }

            connectionStatusView.Hide();
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

            roomLobbyView.CreateLocalClientRoom(
                nickname,
                address,
                port);

            isConnecting = false;
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
            if (roomLobbyView != null)
            {
                roomLobbyView.ResetLocalRoom();
            }

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