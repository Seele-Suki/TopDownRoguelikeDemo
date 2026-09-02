using System;
using System.Collections.Generic;
using TopDownRoguelike.Gameplay.Upgrades;
using TMPro;
using UnityEngine.UI;
using UnityEngine;

namespace TopDownRoguelike.Gameplay.UI
{
    public class UpgradePanelView : MonoBehaviour
    {
        [SerializeField] private List<UpgradeOptionButton> optionButtons = new List<UpgradeOptionButton>();
        [SerializeField] private TMP_Text waitingStatusText;

        private Action<UpgradeData> onUpgradeSelected;

        public bool IsWaitingForRemotePlayer {
            get;
            private set;
        }

        private void Awake()
        {
            Hide();
        }

        public void Show(IReadOnlyList<UpgradeData> upgradeOptions, Action<UpgradeData> selectCallback)
        {
            onUpgradeSelected = selectCallback;
            SetWaitingForRemotePlayer(false);

            gameObject.SetActive(true);

            for (int i = 0; i < optionButtons.Count; i++)
            {
                if (i < upgradeOptions.Count)
                {
                    optionButtons[i].gameObject.SetActive(true);
                    optionButtons[i].Setup(upgradeOptions[i], HandleOptionClicked);
                }
                else
                {
                    optionButtons[i].gameObject.SetActive(false);
                }
            }
        }

        public void Hide()
        {
            gameObject.SetActive(false);
            onUpgradeSelected = null;
            SetWaitingForRemotePlayer(false);
        }

        public void SetWaitingForRemotePlayer(bool waiting)
        {
            IsWaitingForRemotePlayer = waiting;

            if (waitingStatusText != null)
            {
                waitingStatusText.text = waiting
                    ? "Waiting for the other player..."
                    : string.Empty;
                waitingStatusText.gameObject.SetActive(waiting);
            }

            for (int i = 0; i < optionButtons.Count; i++)
            {
                if (optionButtons[i] == null)
                {
                    continue;
                }

                Button button =
                    optionButtons[i].GetComponent<Button>();

                if (button != null)
                {
                    button.interactable = !waiting;
                }
            }
        }

        private void HandleOptionClicked(UpgradeData upgradeData)
        {
            onUpgradeSelected?.Invoke(upgradeData);
        }
    }
}
