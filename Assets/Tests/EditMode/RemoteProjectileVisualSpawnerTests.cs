using NUnit.Framework;
using TopDownRoguelike.Gameplay.Networking;
using TopDownRoguelike.Networking.Protocol;
using UnityEngine;

namespace TopDownRoguelike.Tests.EditMode
{
    public sealed class RemoteProjectileVisualSpawnerTests
    {
        [Test]
        public void Tick_ConsumesShotAndCreatesVisual()
        {
            GameObject receiverObject =
                new GameObject(
                    "Remote Shot Receiver Test");

            GameObject spawnerObject =
                new GameObject(
                    "Remote Shot Spawner Test");

            GameObject prefab =
                new GameObject(
                    "Remote Projectile Prefab");

            try
            {
                RemotePlayerShotEventReceiver receiver =
                    receiverObject.AddComponent<
                        RemotePlayerShotEventReceiver>();

                RemoteProjectileVisual prefabVisual =
                    prefab.AddComponent<
                        RemoteProjectileVisual>();

                RemoteProjectileVisualSpawner spawner =
                    spawnerObject.AddComponent<
                        RemoteProjectileVisualSpawner>();

                receiver.Configure(
                    9u);

                spawner.Configure(
                    receiver,
                    prefab);

                receiver.Enqueue(
                    9u,
                    new PlayerShotEvent(
                        9u,
                        1u,
                        3.0f,
                        -2.0f,
                        0.6f,
                        0.8f));

                spawner.Tick();

                Assert.That(
                    receiver.PendingCount,
                    Is.EqualTo(0));

                Assert.That(
                    spawner.ActiveVisualCount,
                    Is.EqualTo(1));

                RemoteProjectileVisual[] visuals =
                    spawnerObject.GetComponentsInChildren<
                        RemoteProjectileVisual>();

                Assert.That(
                    visuals.Length,
                    Is.EqualTo(1));

                Assert.That(
                    visuals[0].transform.position.x,
                    Is.EqualTo(3.0f));

                Assert.That(
                    visuals[0].transform.position.y,
                    Is.EqualTo(-2.0f));
            }
            finally
            {
                Object.DestroyImmediate(
                    receiverObject);

                Object.DestroyImmediate(
                    spawnerObject);

                Object.DestroyImmediate(
                    prefab);
            }
        }

        [Test]
        public void Configure_RejectsMissingPrefab()
        {
            GameObject receiverObject =
                new GameObject(
                    "Remote Shot Receiver Test");

            GameObject spawnerObject =
                new GameObject(
                    "Remote Shot Spawner Test");

            try
            {
                RemotePlayerShotEventReceiver receiver =
                    receiverObject.AddComponent<
                        RemotePlayerShotEventReceiver>();

                RemoteProjectileVisualSpawner spawner =
                    spawnerObject.AddComponent<
                        RemoteProjectileVisualSpawner>();

                receiver.Configure(
                    9u);

                Assert.Throws<System.ArgumentNullException>(
                    () =>
                        spawner.Configure(
                            receiver,
                            null));
            }
            finally
            {
                Object.DestroyImmediate(
                    receiverObject);

                Object.DestroyImmediate(
                    spawnerObject);
            }
        }

        [Test]
        public void SpawnedVisual_DoesNotContainDamageComponent()
        {
            GameObject receiverObject =
                new GameObject(
                    "Remote Shot Receiver Test");

            GameObject spawnerObject =
                new GameObject(
                    "Remote Shot Spawner Test");

            GameObject prefab =
                new GameObject(
                    "Remote Projectile Prefab");

            try
            {
                RemotePlayerShotEventReceiver receiver =
                    receiverObject.AddComponent<
                        RemotePlayerShotEventReceiver>();

                prefab.AddComponent<
                    RemoteProjectileVisual>();

                RemoteProjectileVisualSpawner spawner =
                    spawnerObject.AddComponent<
                        RemoteProjectileVisualSpawner>();

                receiver.Configure(
                    9u);

                spawner.Configure(
                    receiver,
                    prefab);

                receiver.Enqueue(
                    9u,
                    new PlayerShotEvent(
                        9u,
                        1u,
                        0.0f,
                        0.0f,
                        1.0f,
                        0.0f));

                spawner.Tick();

                MonoBehaviour[] components =
                    spawnerObject.GetComponentsInChildren<
                        MonoBehaviour>();

                for (int index = 0;
                    index < components.Length;
                    index++)
                {
                    Assert.That(
                        components[index].GetType().Name,
                        Is.Not.EqualTo("Projectile"),
                        "Remote spawned visuals must not " +
                        "contain the damage-enabled Projectile component.");
                }
            }
            finally
            {
                Object.DestroyImmediate(
                    receiverObject);

                Object.DestroyImmediate(
                    spawnerObject);

                Object.DestroyImmediate(
                    prefab);
            }
        }
    }
}