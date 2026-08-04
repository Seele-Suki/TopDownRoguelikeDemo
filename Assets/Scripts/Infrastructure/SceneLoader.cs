using UnityEngine;
using UnityEngine.SceneManagement;

namespace TopDownRoguelike.Infrastructure
{
    public class SceneLoader : MonoBehaviour
    {
        [Header("场景名称")]
        [SerializeField] private string mainMenuSceneName = "MainMenu";
        [SerializeField] private string gameplaySceneName = "SampleScene";

        public void LoadMainMenu()
        {
            LoadScene(mainMenuSceneName);
        }

        public void LoadGameplayScene()
        {
            LoadScene(gameplaySceneName);
        }

        public void LoadScene(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                Debug.LogError("场景名称不能为空。");
                return;
            }

            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogError(
                    $"场景未加入构建设置，无法加载：{sceneName}");
                return;
            }

            SceneManager.LoadScene(sceneName);
        }
    }
}
