using System;
using TopDownRoguelike.Networking.Protocol;
using UnityEngine;

namespace TopDownRoguelike.Gameplay.Networking
{
    public sealed class RemoteShotgunVisualSpawner
        : MonoBehaviour
    {
        private RemotePlayerShotgunEventReceiver receiver;
        private GameObject visualPrefab;
        private int activeVisualCount;

        public int ActiveVisualCount =>
            activeVisualCount;

        public void Configure(
            RemotePlayerShotgunEventReceiver
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
                out PlayerShotgunEvent shotgunEvent))
            {
                SpawnVolley(
                    shotgunEvent);
            }
        }

        private void SpawnVolley(
            PlayerShotgunEvent shotgunEvent)
        {
            int projectileCount =
                checked(
                    (int)shotgunEvent.ProjectileCount);

            Vector2 centerDirection =
                new Vector2(
                    shotgunEvent.CenterDirectionX,
                    shotgunEvent.CenterDirectionY)
                .normalized;

            float angleStep =
                projectileCount > 1
                    ? shotgunEvent.SpreadAngle /
                      (projectileCount - 1)
                    : 0f;

            float startAngle =
                projectileCount > 1
                    ? -shotgunEvent.SpreadAngle * 0.5f
                    : 0f;

            for (int index = 0;
                index < projectileCount;
                index++)
            {
                float angle =
                    startAngle +
                    angleStep * index;

                Vector2 direction =
                    (Vector2)(
                        Quaternion.Euler(
                            0f,
                            0f,
                            angle) *
                        centerDirection);

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
                        "Remote shotgun visual prefab " +
                        "requires a RemoteProjectileVisual component.",
                        this);

                    continue;
                }

                visual.Initialize(
                    new Vector2(
                        shotgunEvent.OriginX,
                        shotgunEvent.OriginY),
                    direction);

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
                UnityEngine.Object.DestroyImmediate(
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