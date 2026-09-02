using System.Collections.Generic;
using TopDownRoguelike.Gameplay.Characters;
using TopDownRoguelike.Gameplay.Experience;
using TopDownRoguelike.Gameplay.Weapons;
using TopDownRoguelike.Gameplay.UI;
using TopDownRoguelike.Infrastructure;
using UnityEngine;
using TopDownRoguelike.Gameplay.Core;

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
        [SerializeField] private ShotgunSkill shotgunSkill;
        [SerializeField] private GameManager gameManager;

        [Header("Upgrade Pool")]
        [SerializeField] private List<UpgradeData> availableUpgrades = new List<UpgradeData>();

        private readonly List<UpgradeData> currentOptions = new List<UpgradeData>();

        public IReadOnlyList<UpgradeData> CurrentOptions =>
            currentOptions;

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
            if (TopDownRoguelike.Infrastructure.GameSession.IsMultiplayer)
            {
                return;
            }

            ShowUpgradeOptions();
        }

        private void ShowUpgradeOptions()
        {
            GenerateUpgradeOptions();

            if (gameManager == null)
            {
                Debug.LogError("UpgradeManager: GameManager is not assigned.");
                return;
            }

            gameManager.PauseGame();

            upgradePanelView.Show(currentOptions, SelectUpgrade);
        }

        private void SelectUpgrade(UpgradeData upgradeData)
        {
            ApplyUpgrade(upgradeData);

            upgradePanelView.Hide();

            gameManager.ResumeGame();
        }

        public IReadOnlyList<UpgradeData> GenerateUpgradeOptions()
        {
            currentOptions.Clear();

            List<UpgradeData> candidateUpgrades =
                new List<UpgradeData>(availableUpgrades);

            int optionCount =
                Mathf.Min(3, candidateUpgrades.Count);

            for (int i = 0; i < optionCount; i++)
            {
                int randomIndex =
                    UnityEngine.Random.Range(0, candidateUpgrades.Count);

                UpgradeData selectedUpgrade =
                    candidateUpgrades[randomIndex];

                currentOptions.Add(selectedUpgrade);
                candidateUpgrades.RemoveAt(randomIndex);
            }

            return currentOptions;
        }

        public bool TryGetUpgradeById(
            ushort upgradeId,
            out UpgradeData upgradeData)
        {
            upgradeData = null;

            if (upgradeId == 0)
            {
                return false;
            }

            for (int i = 0; i < availableUpgrades.Count; i++)
            {
                UpgradeData candidate = availableUpgrades[i];
                if (candidate != null && candidate.UpgradeId == upgradeId)
                {
                    upgradeData = candidate;
                    return true;
                }
            }

            return false;
        }

        public void PresentNetworkOptions(
            IReadOnlyList<UpgradeData> options,
            System.Action<UpgradeData> selectCallback)
        {
            if (options == null || options.Count == 0 ||
                gameManager == null || upgradePanelView == null)
            {
                throw new System.InvalidOperationException(
                    "Network upgrade presentation is not configured.");
            }

            List<UpgradeData> optionsCopy =
                new List<UpgradeData>(options);
            currentOptions.Clear();
            currentOptions.AddRange(optionsCopy);
            gameManager.PauseGame();
            upgradePanelView.Show(currentOptions, selectCallback);
        }

        public void SetNetworkWaiting(bool waiting)
        {
            if (upgradePanelView != null)
            {
                upgradePanelView.SetWaitingForRemotePlayer(waiting);
            }
        }

        public void ApplyUpgrade(UpgradeData upgradeData)
        {
            ApplyUpgradeToComponents(
                upgradeData,
                playerController,
                playerHealth,
                playerShooter,
                dashSkill,
                shotgunSkill);

            if (upgradeData != null)
            {
                Debug.Log(
                    $"Selected upgrade: {upgradeData.UpgradeName}");
            }
        }

        public void ApplyUpgradeToPlayer(
            GameObject targetPlayer,
            UpgradeData upgradeData)
        {
            if (targetPlayer == null)
            {
                return;
            }

            ApplyUpgradeToComponents(
                upgradeData,
                targetPlayer.GetComponent<PlayerController>(),
                targetPlayer.GetComponent<PlayerHealth>(),
                targetPlayer.GetComponent<PlayerShooter>(),
                targetPlayer.GetComponent<DashSkill>(),
                targetPlayer.GetComponent<ShotgunSkill>());
        }

        private static void ApplyUpgradeToComponents(
            UpgradeData upgradeData,
            PlayerController targetController,
            PlayerHealth targetHealth,
            PlayerShooter targetShooter,
            DashSkill targetDash,
            ShotgunSkill targetShotgun)
        {
            if (upgradeData == null)
            {
                return;
            }

            switch (upgradeData.UpgradeType)
            {
                case UpgradeType.MoveSpeedUp:
                    targetController?.AddMoveSpeed(
                        upgradeData.FloatValue);
                    break;

                case UpgradeType.MaxHealthUp:
                    targetHealth?.ApplyAuthoritativeMaxHealthUpgrade(
                        upgradeData.IntValue);
                    break;

                case UpgradeType.FireRateUp:
                    targetShooter?.AddFireRate(
                        upgradeData.FloatValue);
                    break;

                case UpgradeType.ProjectileDamageUp:
                    targetShooter?.AddProjectileDamage(
                        upgradeData.IntValue);
                    targetShotgun?.AddProjectileDamage(
                        upgradeData.IntValue);
                    break;

                case UpgradeType.DashCooldownDown:
                    targetDash?.ReduceCooldown(
                        upgradeData.FloatValue);
                    break;

                case UpgradeType.DashDurationUp:
                    targetDash?.AddDashDuration(
                        upgradeData.FloatValue);
                    break;

                case UpgradeType.ShotgunProjectileCountUp:
                    targetShotgun?.AddProjectileCount(
                        upgradeData.IntValue);
                    break;

                case UpgradeType.ShotgunCooldownDown:
                    targetShotgun?.ReduceCooldown(
                        upgradeData.FloatValue);
                    break;

                case UpgradeType.ShotgunPenetrationUp:
                    targetShotgun?.AddPenetration(
                        upgradeData.IntValue);
                    break;
            }

        }
    }
}
