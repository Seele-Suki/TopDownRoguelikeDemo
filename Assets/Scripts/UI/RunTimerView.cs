using TMPro;
using TopDownRoguelike.Gameplay.Core;
using UnityEngine;

namespace TopDownRoguelike.Gameplay.UI
{
    public class RunTimerView : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;
        [SerializeField] private TMP_Text timerText;
        [SerializeField] private TMP_Text debugStateText;

        private int lastDisplayedSecond = -1;

        private void OnEnable()
        {
            if (gameManager != null)
            {
                gameManager.OnStateChanged += HandleStateChanged;
                HandleStateChanged(gameManager.CurrentState);
            }
        }

        private void OnDisable()
        {
            if (gameManager != null)
            {
                gameManager.OnStateChanged -= HandleStateChanged;
            }
        }

        private void Update()
        {
            if (gameManager == null || timerText == null)
            {
                return;
            }

            int totalSeconds =
                Mathf.FloorToInt(gameManager.ElapsedTime);

            if (totalSeconds == lastDisplayedSecond)
            {
                return;
            }

            lastDisplayedSecond = totalSeconds;

            int minutes = totalSeconds / 60;
            int seconds = totalSeconds % 60;

            timerText.text = $"{minutes:00}:{seconds:00}";
        }

        private void HandleStateChanged(GameState gameState)
        {
            if (debugStateText != null)
            {
                debugStateText.text = $"State: {gameState}";
            }
        }
    }
}