using System;
using TMPro;
using TopDownRoguelike.Networking.Room;
using UnityEngine;
using UnityEngine.UI;

namespace TopDownRoguelike.Networking.Client
{
    public sealed class DisconnectDialogView : MonoBehaviour
    {
        [SerializeField] private GameObject dialogRoot;
        [SerializeField] private TMP_Text reasonText;
        [SerializeField] private Button confirmReturnButton;
        [SerializeField] private Button continueSinglePlayerButton;
        [SerializeField] private Button exitButton;
        [SerializeField] private UnityEngine.Events.UnityEvent onReturnRequested;
        [SerializeField] private UnityEngine.Events.UnityEvent onContinueSinglePlayerRequested;
        [SerializeField] private UnityEngine.Events.UnityEvent onExitRequested;

        private bool isHandling;
        private bool isVisible;
        private CanvasGroup selfCanvasGroup;

        public bool IsVisible => isVisible;

        public event Action ReturnRequested;
        public event Action ContinueSinglePlayerRequested;
        public event Action ExitRequested;

        private void Awake()
        {
            if (GetComponent<NetworkDisconnectDialogBinder>() == null)
                gameObject.AddComponent<NetworkDisconnectDialogBinder>();

            if (dialogRoot == gameObject)
            {
                selfCanvasGroup = GetComponent<CanvasGroup>();
                if (selfCanvasGroup == null)
                    selfCanvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            WireButton(confirmReturnButton, HandleReturn);
            WireButton(continueSinglePlayerButton, HandleContinueSinglePlayer);
            WireButton(exitButton, HandleExit);
            Hide();
        }

        public bool Show(DisconnectContext context)
        {
            if (isHandling || IsVisible)
                return false;

            DisconnectAction action = DisconnectPolicy.Resolve(context);
            if (action == DisconnectAction.None)
                return false;

            if (reasonText != null)
                reasonText.text = GetReasonText(context.Reason);

            SetButtonVisible(confirmReturnButton,
                action == DisconnectAction.ShowHostDisconnectedDialog ||
                action == DisconnectAction.ReturnToMultiplayerMenu);
            SetButtonVisible(continueSinglePlayerButton,
                action == DisconnectAction.ShowClientDisconnectedDialog);
            SetButtonVisible(exitButton,
                action == DisconnectAction.ShowClientDisconnectedDialog);

            SetDialogVisible(true);
            isVisible = true;
            Debug.Log($"DisconnectDialogView shown: role={context.Role}, gameplay={context.IsInGameplay}, reason={context.Reason}, action={action}.");
            return true;
        }

        public void Hide()
        {
            isHandling = false;
            isVisible = false;
            SetButtonVisible(confirmReturnButton, false);
            SetButtonVisible(continueSinglePlayerButton, false);
            SetButtonVisible(exitButton, false);
            SetDialogVisible(false);
        }

        private void SetDialogVisible(bool visible)
        {
            if (dialogRoot == null)
                return;

            if (dialogRoot == gameObject)
            {
                if (selfCanvasGroup == null)
                    selfCanvasGroup = GetComponent<CanvasGroup>();

                if (selfCanvasGroup != null)
                {
                    selfCanvasGroup.alpha = visible ? 1f : 0f;
                    selfCanvasGroup.interactable = visible;
                    selfCanvasGroup.blocksRaycasts = visible;
                }

                return;
            }

            dialogRoot.SetActive(visible);
        }

        private void HandleReturn() { InvokeOnce(ReturnRequested, onReturnRequested); }
        private void HandleContinueSinglePlayer() { InvokeOnce(ContinueSinglePlayerRequested, onContinueSinglePlayerRequested); }
        private void HandleExit() { InvokeOnce(ExitRequested, onExitRequested); }

        private void InvokeOnce(Action callback, UnityEngine.Events.UnityEvent unityEvent)
        {
            if (isHandling)
                return;
            isHandling = true;
            SetButtonVisible(confirmReturnButton, false);
            SetButtonVisible(continueSinglePlayerButton, false);
            SetButtonVisible(exitButton, false);
            callback?.Invoke();
            unityEvent?.Invoke();
        }

        private static void WireButton(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button == null)
                return;
            button.onClick.RemoveListener(action);
            button.onClick.AddListener(action);
        }

        private static void SetButtonVisible(Button button, bool visible)
        {
            if (button != null)
                button.gameObject.SetActive(visible);
        }

        private static string GetReasonText(DisconnectReason reason)
        {
            switch (reason)
            {
                case DisconnectReason.RemotePeerLeft: return "对方已离开房间";
                case DisconnectReason.ServerClosed: return "服务器已关闭";
                case DisconnectReason.HeartbeatTimeout: return "连接超时";
                case DisconnectReason.TransportError: return "网络连接发生错误";
                default: return "联机连接已断开";
            }
        }
    }
}
