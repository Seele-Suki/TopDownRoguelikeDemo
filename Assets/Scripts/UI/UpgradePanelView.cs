using System;
using System.Collections.Generic;
using TopDownRoguelike.Gameplay.Upgrades;
using UnityEngine;

namespace TopDownRoguelike.Gameplay.UI
{
    public class UpgradePanelView : MonoBehaviour
    {
        [SerializeField] private List<UpgradeOptionButton> optionButtons = new List<UpgradeOptionButton>();

        private Action<UpgradeData> onUpgradeSelected;

        private void Awake()
        {
            Hide();
        }

        public void Show(IReadOnlyList<UpgradeData> upgradeOptions, Action<UpgradeData> selectCallback)
        {
            onUpgradeSelected = selectCallback;

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
        }

        private void HandleOptionClicked(UpgradeData upgradeData)
        {
            onUpgradeSelected?.Invoke(upgradeData);
        }
    }
}