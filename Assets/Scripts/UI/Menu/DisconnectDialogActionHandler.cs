using TopDownRoguelike.Infrastructure;
using TopDownRoguelike.Gameplay.Networking;
using TopDownRoguelike.Networking.Client;
using UnityEngine;

namespace TopDownRoguelike.Menu.UI
{
    public sealed class DisconnectDialogActionHandler : MonoBehaviour
    {
        [SerializeField] private SceneLoader sceneLoader;
        [SerializeField] private NetworkClientBehaviour networkClientBehaviour;

        private bool handled;

        public void ReturnToMultiplayerMenu()
        {
            if (!TryBegin())
                return;
            DisconnectClient();
            sceneLoader?.LoadMainMenu();
        }

        public void ContinueSinglePlayer()
        {
            if (!TryBegin())
                return;

            NetworkGameBootstrap bootstrap =
                FindObjectOfType<NetworkGameBootstrap>();

            if (bootstrap != null && bootstrap.ContinueAsSinglePlayer())
                return;

            DisconnectClient();
            GameSession.ConfigureSinglePlayer();
        }

        public void ExitToMainMenu()
        {
            if (!TryBegin())
                return;
            DisconnectClient();
            sceneLoader?.LoadMainMenu();
        }

        private bool TryBegin()
        {
            if (handled)
                return false;
            handled = true;
            return true;
        }

        private void DisconnectClient()
        {
            NetworkClientBehaviour behaviour =
                networkClientBehaviour != null
                    ? networkClientBehaviour
                    : NetworkClientBehaviour.Instance;
            behaviour?.Client?.Disconnect(false);
        }
    }
}
