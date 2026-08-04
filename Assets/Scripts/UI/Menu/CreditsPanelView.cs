using UnityEngine;

namespace TopDownRoguelike.Menu.UI
{
    public class CreditsPanelView : MonoBehaviour
    {
        [Header("引用")]
        [SerializeField] private GameObject panelRoot;

        [Header("链接")]
        [SerializeField]
        private string githubUrl =
            "https://github.com/Seele-Suki/TopDownRoguelikeDemo";

        private void Awake()
        {
            if (panelRoot == null)
            {
                Debug.LogError(
                    "CreditsPanelView: Panel Root is not assigned.");

                enabled = false;
                return;
            }

            panelRoot.SetActive(false);
        }

        public void Show()
        {
            if (panelRoot != null)
            {
                panelRoot.SetActive(true);
            }
        }

        public void Hide()
        {
            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
            }
        }

        public void OpenGithub()
        {
            if (string.IsNullOrWhiteSpace(githubUrl))
            {
                Debug.LogError(
                    "CreditsPanelView: GitHub URL is empty.");

                return;
            }

            Application.OpenURL(githubUrl);
        }
    }
}
