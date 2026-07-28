using TMPro;
using TopDownRoguelike.Gameplay.Core;
using UnityEngine;

namespace TopDownRoguelike.Gameplay.UI
{
    public class BossTransitionView : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;
        [SerializeField] private TMP_Text warningText;

        private void Awake()
        {
            if (gameManager == null ||
                warningText == null)
            {
                Debug.LogError(
                    "BossTransitionView: " +
                    "Required references are missing.");

                enabled = false;
                return;
            }

            warningText.enabled = false;
        }

        private void OnEnable()
        {
            if (gameManager != null)
            {
                gameManager.OnStateChanged +=
                    HandleStateChanged;

                HandleStateChanged(
                    gameManager.CurrentState);
            }
        }

        private void OnDisable()
        {
            if (gameManager != null)
            {
                gameManager.OnStateChanged -=
                    HandleStateChanged;
            }
        }

        private void HandleStateChanged(
            GameState gameState)
        {
            if (warningText != null)
            {
                warningText.enabled =
                    gameState ==
                    GameState.BossTransition;
            }
        }
    }
}