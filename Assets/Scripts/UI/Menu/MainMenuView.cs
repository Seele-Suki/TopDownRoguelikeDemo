using UnityEngine;

namespace TopDownRoguelike.Menu.UI
{
    public class MainMenuView : MonoBehaviour
    {
        public void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}