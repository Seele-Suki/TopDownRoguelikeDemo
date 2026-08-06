using UnityEngine;
using TopDownRoguelike.Infrastructure;

namespace TopDownRoguelike.Menu.UI
{
    public sealed class SinglePlayerSetupView : MonoBehaviour
    {
        [Header("页面")]
        [SerializeField] private GameObject mainMenuPanel;
        [SerializeField] private GameObject setupPanel;

        [Header("场景加载")]
        [SerializeField] private SceneLoader sceneLoader;

        [Header("角色卡片")]
        [SerializeField] private SelectionCardView rangedCharacterCard;
        [SerializeField] private SelectionCardView meleeCharacterCard;

        [Header("难度卡片")]
        [SerializeField] private SelectionCardView normalDifficultyCard;
        [SerializeField] private SelectionCardView hardDifficultyCard;
        [SerializeField] private SelectionCardView hellDifficultyCard;

        private void Awake()
        {
            rangedCharacterCard.AddClickListener(SelectRangedCharacter);
            normalDifficultyCard.AddClickListener(SelectNormalDifficulty);
        }

        private void Update()
        {
            if (setupPanel != null &&
                setupPanel.activeSelf &&
                Input.GetKeyDown(KeyCode.Escape))
            {
                CloseSetupPanel();
            }
        }

        public void OpenSetupPanel()
        {
            GameSession.ConfigureSinglePlayer();

            if (mainMenuPanel != null)
            {
                mainMenuPanel.SetActive(false);
            }

            if (setupPanel != null)
            {
                setupPanel.SetActive(true);
            }

            ResetCards();
        }

        public void CloseSetupPanel()
        {
            GameSession.ClearSelection();

            if (setupPanel != null)
            {
                setupPanel.SetActive(false);
            }

            if (mainMenuPanel != null)
            {
                mainMenuPanel.SetActive(true);
            }
        }

        private void SelectRangedCharacter()
        {
            GameSession.SelectCharacter(CharacterId.Ranged);

            rangedCharacterCard.SetSelected(true);
            meleeCharacterCard.SetSelected(false);

            TryStartSinglePlayer();
        }

        private void SelectNormalDifficulty()
        {
            GameSession.SelectDifficulty(DifficultyId.Normal);

            normalDifficultyCard.SetSelected(true);
            hardDifficultyCard.SetSelected(false);
            hellDifficultyCard.SetSelected(false);

            TryStartSinglePlayer();
        }

        private void TryStartSinglePlayer()
        {
            if (!GameSession.HasCompleteSelection)
            {
                return;
            }

            if (sceneLoader == null)
            {
                Debug.LogError("SinglePlayerSetupView 没有配置 SceneLoader 引用。");
                return;
            }

            sceneLoader.LoadGameplayScene();
        }

        private void ResetCards()
        {
            rangedCharacterCard.SetAvailable(true);
            rangedCharacterCard.SetSelected(false);

            meleeCharacterCard.SetAvailable(false);
            meleeCharacterCard.SetSelected(false);

            normalDifficultyCard.SetAvailable(true);
            normalDifficultyCard.SetSelected(false);

            hardDifficultyCard.SetAvailable(false);
            hardDifficultyCard.SetSelected(false);

            hellDifficultyCard.SetAvailable(false);
            hellDifficultyCard.SetSelected(false);
        }
    }
}