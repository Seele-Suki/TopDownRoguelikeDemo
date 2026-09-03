using TopDownRoguelike.Networking.Room;
using TopDownRoguelike.Infrastructure;
using UnityEngine;

namespace TopDownRoguelike.Networking.Client
{
    public sealed class NetworkDisconnectDialogBinder : MonoBehaviour
    {
        [SerializeField] private DisconnectDialogView dialogView;
        [SerializeField] private RoomRole role = RoomRole.Client;
        [SerializeField] private bool isInGameplay = true;
        [SerializeField] private bool inferRoleFromGameSession = true;

        private NetworkClient client;

        private void OnEnable()
        {
            Subscribe();
        }

        private void Start()
        {
            Subscribe();
        }

        private void Update()
        {
            // NetworkClientBehaviour is persistent and may be created after
            // this scene's UI objects. Retry until the shared client exists.
            if (client == null)
                Subscribe();
        }

        private void OnDisable()
        {
            if (client != null)
                client.DisconnectOccurred -= HandleDisconnect;
            client = null;
        }

        private void Subscribe()
        {
            if (client != null)
                return;
            if (dialogView == null)
                dialogView = GetComponent<DisconnectDialogView>();
            client = NetworkClientBehaviour.Instance?.Client;
            if (client != null)
            {
                client.DisconnectOccurred += HandleDisconnect;
                Debug.Log($"NetworkDisconnectDialogBinder subscribed on {name}.");
            }
        }

        private void HandleDisconnect(DisconnectReason reason)
        {
            Debug.Log($"NetworkDisconnectDialogBinder received disconnect: {reason} on {name}.");
            RoomRole effectiveRole = role;
            if (inferRoleFromGameSession)
            {
                effectiveRole = GameSession.IsHost
                    ? RoomRole.Host
                    : RoomRole.Client;
            }

            DisconnectContext context =
                new DisconnectContext(effectiveRole, isInGameplay, reason);
            DisconnectPauseController.TryPause(context);
            dialogView?.Show(context);
        }
    }
}
