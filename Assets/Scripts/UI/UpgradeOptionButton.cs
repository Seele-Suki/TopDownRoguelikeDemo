using System;
using TMPro;
using TopDownRoguelike.Gameplay.Upgrades;
using UnityEngine;
using UnityEngine.UI;

namespace TopDownRoguelike.Gameplay.UI
{
    public class UpgradeOptionButton : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text descriptionText;

        private UpgradeData upgradeData;
        private Action<UpgradeData> onClicked;

        private void Awake()
        {
            if (button == null)
            {
                button = GetComponent<Button>();
            }

            button.onClick.AddListener(HandleClick);
        }

        private void OnDestroy()
        {
            button.onClick.RemoveListener(HandleClick);
        }

        public void Setup(UpgradeData data, Action<UpgradeData> clickCallback)
        {
            upgradeData = data;
            onClicked = clickCallback;

            nameText.text = upgradeData.UpgradeName;
            descriptionText.text = upgradeData.Description;
        }

        private void HandleClick()
        {
            onClicked?.Invoke(upgradeData);
        }
    }
}