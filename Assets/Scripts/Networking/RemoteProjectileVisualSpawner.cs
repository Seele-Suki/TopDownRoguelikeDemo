using System;
using TopDownRoguelike.Networking.Protocol;
using UnityEngine;

namespace TopDownRoguelike.Gameplay.Networking
{
    public sealed class RemoteProjectileVisualSpawner
        : MonoBehaviour
    {
        private RemotePlayerShotEventReceiver
            receiver;

        private GameObject visualPrefab;

        private int activeVisualCount;

        public int ActiveVisualCount =>
            activeVisualCount;

        public void Configure(
            RemotePlayerShotEventReceiver
                newReceiver,
            GameObject newVisualPrefab)
        {
            receiver =
                newReceiver ??
                throw new ArgumentNullException(
                    nameof(newReceiver));

            visualPrefab =
                newVisualPrefab ??
                throw new ArgumentNullException(
                    nameof(newVisualPrefab));

            activeVisualCount =
                0;

            enabled =
                true;
        }

        public void Tick()
        {
            if (receiver == null ||
                visualPrefab == null)
            {
                return;
            }

            while (receiver.TryDequeue(
                out PlayerShotEvent shotEvent))
            {
                Debug.Log(
                    $"RemoteProjectileVisualSpawner: " +
                    $"spawning shot player={shotEvent.PlayerId}, " +
                    $"sequence={shotEvent.ShotSequence}",
                    this);

                GameObject visualObject =
                    Instantiate(
                        visualPrefab,
                        transform);

                RemoteProjectileVisual visual =
                    visualObject.GetComponent<
                        RemoteProjectileVisual>();

                if (visual == null)
                {
                    DestroyVisual(
                        visualObject);

                    Debug.LogError(
                        "Remote projectile prefab requires " +
                        "a RemoteProjectileVisual component.",
                        this);

                    continue;
                }

                visual.Initialize(
                    new Vector2(
                        shotEvent.OriginX,
                        shotEvent.OriginY),
                    new Vector2(
                        shotEvent.DirectionX,
                        shotEvent.DirectionY));

                activeVisualCount++;
            }
        }

        private void Update()
        {
            Tick();
        }

        private void DestroyVisual(
            GameObject visualObject)
        {
            if (visualObject == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(
                    visualObject);
            }
            else
            {
                DestroyImmediate(
                    visualObject);
            }
        }

        private void OnDisable()
        {
            receiver =
                null;

            visualPrefab =
                null;

            activeVisualCount =
                0;
        }
    }
}