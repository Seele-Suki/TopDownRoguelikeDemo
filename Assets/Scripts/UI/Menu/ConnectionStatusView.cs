using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TopDownRoguelike.Menu.UI
{
    public sealed class ConnectionStatusView : MonoBehaviour
    {
        [Header("Panel")]
        [SerializeField] private GameObject panelRoot;

        [Header("Texts")]
        [SerializeField] private TMP_Text statusTitleText;
        [SerializeField] private TMP_Text statusMessageText;

        [Header("Actions")]
        [SerializeField] private Button closeButton;

        private void Awake()
        {
            Hide();
        }

        public void ShowConnecting(
            string address,
            int port)
        {
            if (panelRoot != null)
            {
                panelRoot.SetActive(true);
            }

            if (statusTitleText != null)
            {
                statusTitleText.text = "正在连接";
            }

            if (statusMessageText != null)
            {
                bool isIpv6 = address.Contains(":");

                string endpoint = isIpv6
                    ? $"[{address}]:{port}"
                    : $"{address}:{port}";

                statusMessageText.text =
                    $"正在连接 {endpoint}，请稍候";
            }

            if (closeButton != null)
            {
                closeButton.gameObject.SetActive(false);
            }
        }

        public void ShowFailure(string message)
        {
            if (panelRoot != null)
            {
                panelRoot.SetActive(true);
            }

            if (statusTitleText != null)
            {
                statusTitleText.text = "连接失败";
            }

            if (statusMessageText != null)
            {
                statusMessageText.text = message;
            }

            if (closeButton != null)
            {
                closeButton.gameObject.SetActive(true);
                closeButton.interactable = true;
            }
        }

        public void Hide()
        {
            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
            }
        }
    }
}