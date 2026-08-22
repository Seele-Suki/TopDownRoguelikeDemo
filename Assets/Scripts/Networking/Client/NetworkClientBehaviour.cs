using UnityEngine;

namespace TopDownRoguelike.Networking.Client
{
    public sealed class NetworkClientBehaviour
        : MonoBehaviour
    {
        public static NetworkClientBehaviour Instance
        {
            get;
            private set;
        }

        public NetworkClient Client
        {
            get;
            private set;
        }

        private void Awake()
        {
            if (Instance != null &&
                Instance != this)
            {
                enabled =
                    false;

                if (Application.isPlaying)
                {
                    Destroy(
                        gameObject);
                }
                else
                {
                    DestroyImmediate(
                        gameObject);
                }

                return;
            }

            Instance =
                this;

            Client =
                new NetworkClient();

            if (Application.isPlaying)
            {
                DontDestroyOnLoad(
                    gameObject);
            }
        }

        private void Update()
        {
            Client?.Tick();
        }

        private void OnDestroy()
        {
            Client?.Dispose();

            if (Instance == this)
            {
                Instance =
                    null;
            }
        }
    }
}