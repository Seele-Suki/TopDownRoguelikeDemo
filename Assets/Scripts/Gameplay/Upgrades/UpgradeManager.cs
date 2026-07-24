using System.Collections.Generic;
using TopDownRoguelike.Gameplay.Characters;
using TopDownRoguelike.Gameplay.Experience;
using TopDownRoguelike.Gameplay.Weapons;
using TopDownRoguelike.Gameplay.UI;
using UnityEngine;

namespace TopDownRoguelike.Gameplay.Upgrades
{
    public class UpgradeManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private LevelSystem levelSystem;
        [SerializeField] private PlayerController playerController;
        [SerializeField] private PlayerHealth playerHealth;
        [SerializeField] private PlayerShooter playerShooter;
        [SerializeField] private UpgradePanelView upgradePanelView;
        [SerializeField] private DashSkill dashSkill;

        [Header("Upgrade Pool")]
        [SerializeField] private List<UpgradeData> availableUpgrades = new List<UpgradeData>();

        private readonly List<UpgradeData> currentOptions = new List<UpgradeData>();

        private void OnEnable()
        {
            if (levelSystem != null)
            {
                levelSystem.OnLevelUp += HandleLevelUp;
            }
        }

        private void OnDisable()
        {
            if (levelSystem != null)
            {
                levelSystem.OnLevelUp -= HandleLevelUp;
            }
        }

        private void HandleLevelUp(int newLevel)
        {
            ShowUpgradeOptions();
        }

        private void ShowUpgradeOptions()
        {
            currentOptions.Clear();

            List<UpgradeData> candidateUpgrades = new List<UpgradeData>(availableUpgrades);

            int optionCount = Mathf.Min(3, candidateUpgrades.Count);

            for (int i = 0; i < optionCount; i++)
            {
                int randomIndex = Random.Range(0, candidateUpgrades.Count);
                UpgradeData selectedUpgrade = candidateUpgrades[randomIndex];

                currentOptions.Add(selectedUpgrade);
                candidateUpgrades.RemoveAt(randomIndex);
            }

            Time.timeScale = 0f;

            upgradePanelView.Show(currentOptions, SelectUpgrade);
        }

        private void SelectUpgrade(UpgradeData upgradeData)
        {
            ApplyUpgrade(upgradeData);

            upgradePanelView.Hide();

            Time.timeScale = 1f;
        }

        private void ApplyUpgrade(UpgradeData upgradeData)
        {
            if (upgradeData == null)
            {
                return;
            }

            switch (upgradeData.UpgradeType)
            {
                case UpgradeType.MoveSpeedUp:
                    playerController.AddMoveSpeed(upgradeData.FloatValue);
                    break;

                case UpgradeType.MaxHealthUp:
                    playerHealth.AddMaxHealth(upgradeData.IntValue);
                    break;

                case UpgradeType.FireRateUp:
                    playerShooter.AddFireRate(upgradeData.FloatValue);
                    break;

                case UpgradeType.ProjectileDamageUp:
                    playerShooter.AddProjectileDamage(upgradeData.IntValue);
                    break;

                case UpgradeType.DashCooldownDown:
                    dashSkill.ReduceCooldown(upgradeData.FloatValue);
                    break;

                case UpgradeType.DashDurationUp:
                    dashSkill.AddDashDuration(upgradeData.FloatValue);
                    break;
            }

            Debug.Log($"Selected upgrade: {upgradeData.UpgradeName}");
        }
    }
}