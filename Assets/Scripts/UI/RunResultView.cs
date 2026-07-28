using TMPro;
using TopDownRoguelike.Gameplay.Core;
using TopDownRoguelike.Gameplay.Experience;
using UnityEngine;

namespace TopDownRoguelike.Gameplay.UI
{
    public class RunResultView : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;
        [SerializeField] private LevelSystem levelSystem;
        [SerializeField] private GameObject resultPanel;
        [SerializeField] private TMP_Text resultTitleText;
        [SerializeField] private TMP_Text resultStatsText;

        private void Awake()
        {
            if (gameManager == null ||
                levelSystem == null ||
                resultPanel == null ||
                resultTitleText == null ||
                resultStatsText == null)
            {
                Debug.LogError(
                    "RunResultView: Required references are missing.");

                enabled = false;
                return;
            }

            resultPanel.SetActive(false);
        }

        private void OnEnable()
        {
            if (gameManager != null)
            {
                gameManager.OnStateChanged += HandleStateChanged;
            }
        }

        private void OnDisable()
        {
            if (gameManager != null)
            {
                gameManager.OnStateChanged -= HandleStateChanged;
            }
        }

        private void HandleStateChanged(GameState gameState)
        {
            if (gameState == GameState.Victory)
            {
                ShowResult(true);
            }
            else if (gameState == GameState.Defeat)
            {
                ShowResult(false);
            }
        }

        private void ShowResult(bool isVictory)
        {
            int totalSeconds =
                Mathf.FloorToInt(gameManager.ElapsedTime);

            int minutes = totalSeconds / 60;
            int seconds = totalSeconds % 60;

            resultTitleText.text =
                isVictory ? "挑战成功" : "挑战失败";

            resultStatsText.text =
                $"存活时间  {minutes:00}:{seconds:00}\n" +
                $"角色等级  Lv.{levelSystem.CurrentLevel}\n" +
                $"击杀敌人  {gameManager.KillCount}";

            resultPanel.SetActive(true);
        }
    }
}