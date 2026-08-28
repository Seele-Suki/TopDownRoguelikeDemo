using NUnit.Framework;
using TopDownRoguelike.Gameplay.Networking;
using UnityEngine;

namespace TopDownRoguelike.Tests.EditMode
{
    public sealed class RemoteProjectileVisualTests
    {
        [Test]
        public void Initialize_SetsPositionAndDirection()
        {
            GameObject projectileObject =
                new GameObject(
                    "Remote Projectile Visual Test");

            try
            {
                RemoteProjectileVisual visual =
                    projectileObject.AddComponent<
                        RemoteProjectileVisual>();

                visual.Initialize(
                    new Vector2(
                        2.0f,
                        3.0f),
                    new Vector2(
                        3.0f,
                        4.0f));

                Assert.That(
                    projectileObject.transform.position.x,
                    Is.EqualTo(2.0f));

                Assert.That(
                    projectileObject.transform.position.y,
                    Is.EqualTo(3.0f));

                Assert.That(
                    visual.Direction.x,
                    Is.EqualTo(0.6f)
                        .Within(0.0001f));

                Assert.That(
                    visual.Direction.y,
                    Is.EqualTo(0.8f)
                        .Within(0.0001f));
            }
            finally
            {
                Object.DestroyImmediate(
                    projectileObject);
            }
        }

        [Test]
        public void Tick_MovesVisualWithoutProjectileDamageComponent()
        {
            GameObject projectileObject =
                new GameObject(
                    "Remote Projectile Visual Test");

            try
            {
                RemoteProjectileVisual visual =
                    projectileObject.AddComponent<
                        RemoteProjectileVisual>();

                visual.Initialize(
                    Vector2.zero,
                    Vector2.right);

                visual.Tick(
                    0.5f);

                Assert.That(
                    projectileObject.transform.position.x,
                    Is.EqualTo(6.0f)
                        .Within(0.0001f));

                MonoBehaviour[] components =
                    projectileObject.GetComponents<
                        MonoBehaviour>();

                for (int index = 0;
                    index < components.Length;
                    index++)
                {
                    Assert.That(
                        components[index].GetType().Name,
                        Is.Not.EqualTo("Projectile"),
                        "Remote visual projectiles must not " +
                        "contain the damage-enabled Projectile component.");
                }
            }
            finally
            {
                Object.DestroyImmediate(
                    projectileObject);
            }
        }

        [Test]
        public void Initialize_RejectsZeroDirection()
        {
            GameObject projectileObject =
                new GameObject(
                    "Remote Projectile Visual Test");

            try
            {
                RemoteProjectileVisual visual =
                    projectileObject.AddComponent<
                        RemoteProjectileVisual>();

                Assert.Throws<System.ArgumentException>(
                    () =>
                        visual.Initialize(
                            Vector2.zero,
                            Vector2.zero));
            }
            finally
            {
                Object.DestroyImmediate(
                    projectileObject);
            }
        }
    }
}