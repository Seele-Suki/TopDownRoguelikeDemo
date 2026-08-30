using NUnit.Framework;
using TopDownRoguelike.Gameplay.Networking;
using TopDownRoguelike.Networking.Protocol;
using UnityEngine;

namespace TopDownRoguelike.Tests.EditMode
{
    public sealed class RemoteShotgunVisualSpawnerTests
    {
        [Test]
        public void Tick_SpawnsExactProjectileCount()
        {
            GameObject owner =
                new GameObject(
                    "Remote Shotgun Visual Owner");

            GameObject prefab =
                new GameObject(
                    "Remote Shotgun Visual Prefab");

            try
            {
                RemotePlayerShotgunEventReceiver receiver =
                    owner.AddComponent<
                        RemotePlayerShotgunEventReceiver>();

                RemoteProjectileVisual visual =
                    prefab.AddComponent<
                        RemoteProjectileVisual>();

                receiver.Configure(
                    9u);

                receiver.Enqueue(
                    7u,
                    new PlayerShotgunEvent(
                        9u,
                        1u,
                        1.0f,
                        2.0f,
                        1.0f,
                        0.0f,
                        5u,
                        40.0f,
                        0.75f));

                RemoteShotgunVisualSpawner spawner =
                    owner.AddComponent<
                        RemoteShotgunVisualSpawner>();

                spawner.Configure(
                    receiver,
                    prefab);

                spawner.Tick();

                Assert.That(
                    spawner.ActiveVisualCount,
                    Is.EqualTo(5));

                Assert.That(
                    owner.transform.childCount,
                    Is.EqualTo(5));
            }
            finally
            {
                Object.DestroyImmediate(
                    owner);

                Object.DestroyImmediate(
                    prefab);
            }
        }

        [Test]
        public void
    HostAndClientVolley_ProduceMatchingSymmetricDirections()
        {
            System.Type skillType =
                FindType(
                    "TopDownRoguelike.Gameplay.Characters." +
                    "ShotgunSkill");

            System.Type inputType =
                FindType(
                    "TopDownRoguelike.Gameplay.Networking." +
                    "RemotePlayerInputSource");

            System.Type poolType =
                FindType(
                    "TopDownRoguelike.Gameplay.Weapons." +
                    "ProjectilePool");

            System.Type projectileType =
                FindType(
                    "TopDownRoguelike.Gameplay.Weapons." +
                    "Projectile");

            Assert.That(skillType, Is.Not.Null);
            Assert.That(inputType, Is.Not.Null);
            Assert.That(poolType, Is.Not.Null);
            Assert.That(projectileType, Is.Not.Null);

            GameObject hostPlayer =
                new GameObject(
                    "Host Shotgun Direction Test");

            GameObject hostPoolObject =
                new GameObject(
                    "Host Shotgun Direction Pool");

            GameObject hostProjectilePrefab =
                new GameObject(
                    "Host Projectile Direction Prefab");

            GameObject clientPlayer =
                new GameObject(
                    "Client Shotgun Direction Test");

            GameObject clientVisualPrefab =
                new GameObject(
                    "Client Projectile Visual Prefab");

            hostPlayer.SetActive(false);
            hostPoolObject.SetActive(false);
            hostProjectilePrefab.SetActive(false);

            try
            {
                Component projectilePrefab =
                    hostProjectilePrefab.AddComponent(
                        projectileType);

                Component projectilePool =
                    hostPoolObject.AddComponent(
                        poolType);

                SetPrivateField(
                    projectilePool,
                    "projectilePrefab",
                    projectilePrefab);

                SetPrivateField(
                    projectilePool,
                    "initialSize",
                    0);

                Component inputSource =
                    hostPlayer.AddComponent(
                        inputType);

                Component shotgunSkill =
                    hostPlayer.AddComponent(
                        skillType);

                SetPrivateField(
                    shotgunSkill,
                    "projectilePool",
                    projectilePool);

                SetPrivateField(
                    shotgunSkill,
                    "firePoint",
                    hostPlayer.transform);

                SetPrivateField(
                    shotgunSkill,
                    "projectileCount",
                    5);

                SetPrivateField(
                    shotgunSkill,
                    "spreadAngle",
                    40f);

                SetPrivateField(
                    shotgunSkill,
                    "projectileDamage",
                    1);

                SetPrivateField(
                    shotgunSkill,
                    "penetrationCount",
                    0);

                Vector2 centerDirection =
                    new Vector2(
                        3f,
                        4f).normalized;

                var applyInputState =
                    inputType.GetMethod(
                        "ApplyInputState",
                        new[]
                        {
                            typeof(Vector2),
                            typeof(Vector2),
                            typeof(bool),
                            typeof(uint),
                            typeof(uint)
                        });

                Assert.That(
                    applyInputState,
                    Is.Not.Null);

                applyInputState.Invoke(
                    inputSource,
                    new object[]
                    {
                        Vector2.zero,
                        centerDirection,
                        false,
                        0u,
                        0u
                    });

                var setInputSource =
                    skillType.GetMethod(
                        "SetInputSource",
                        System.Reflection.BindingFlags.Instance |
                        System.Reflection.BindingFlags.Public);

                Assert.That(
                    setInputSource,
                    Is.Not.Null);

                setInputSource.Invoke(
                    shotgunSkill,
                    new object[]
                    {
                        inputSource
                    });

                var fireShotgun =
                    skillType.GetMethod(
                        "FireShotgun",
                        System.Reflection.BindingFlags.Instance |
                        System.Reflection.BindingFlags.NonPublic);

                Assert.That(
                    fireShotgun,
                    Is.Not.Null);

                fireShotgun.Invoke(
                    shotgunSkill,
                    null);

                RemotePlayerShotgunEventReceiver receiver =
                    clientPlayer.AddComponent<
                        RemotePlayerShotgunEventReceiver>();

                receiver.Configure(
                    9u);

                receiver.Enqueue(
                    7u,
                    new PlayerShotgunEvent(
                        9u,
                        1u,
                        0f,
                        0f,
                        centerDirection.x,
                        centerDirection.y,
                        5u,
                        40f,
                        0.75f));

                clientVisualPrefab.AddComponent<
                    RemoteProjectileVisual>();

                RemoteShotgunVisualSpawner spawner =
                    clientPlayer.AddComponent<
                        RemoteShotgunVisualSpawner>();

                spawner.Configure(
                    receiver,
                    clientVisualPrefab);

                spawner.Tick();

                Assert.That(
                    hostPoolObject.transform.childCount,
                    Is.EqualTo(5));

                Assert.That(
                    clientPlayer.transform.childCount,
                    Is.EqualTo(5));

                float[] expectedAngles =
                {
                    -20f,
                    -10f,
                    0f,
                    10f,
                    20f
                };

                for (int index = 0;
                    index < expectedAngles.Length;
                    index++)
                {
                    Component hostProjectile =
                        hostPoolObject.transform
                            .GetChild(index)
                            .GetComponent(
                                projectileType);

                    Assert.That(
                        hostProjectile,
                        Is.Not.Null);

                    Vector2 hostDirection =
                        ReadPrivateVector2(
                            hostProjectile,
                            "moveDirection");

                    RemoteProjectileVisual clientVisual =
                        clientPlayer.transform
                            .GetChild(index)
                            .GetComponent<
                                RemoteProjectileVisual>();

                    Assert.That(
                        clientVisual,
                        Is.Not.Null);

                    Assert.That(
                        Vector2.Distance(
                            hostDirection,
                            clientVisual.Direction),
                        Is.LessThan(0.0001f),
                        $"Projectile {index} differs between " +
                        "Host and Client.");

                    Assert.That(
                        Vector2.SignedAngle(
                            centerDirection,
                            hostDirection),
                        Is.EqualTo(
                            expectedAngles[index])
                            .Within(0.001f),
                        $"Projectile {index} is not at its " +
                        "expected symmetric angle.");
                }
            }
            finally
            {
                Object.DestroyImmediate(
                    hostPlayer);

                Object.DestroyImmediate(
                    hostPoolObject);

                Object.DestroyImmediate(
                    hostProjectilePrefab);

                Object.DestroyImmediate(
                    clientPlayer);

                Object.DestroyImmediate(
                    clientVisualPrefab);
            }
        }

        private static System.Type FindType(
            string fullTypeName)
        {
            foreach (System.Reflection.Assembly assembly
                in System.AppDomain.CurrentDomain
                    .GetAssemblies())
            {
                System.Type type =
                    assembly.GetType(
                        fullTypeName,
                        false);

                if (type != null)
                {
                    return type;
                }
            }

            return null;
        }

        private static void SetPrivateField(
            object target,
            string fieldName,
            object value)
        {
            System.Reflection.FieldInfo field =
                target.GetType().GetField(
                    fieldName,
                    System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.NonPublic);

            Assert.That(
                field,
                Is.Not.Null,
                $"{fieldName} was not found.");

            field.SetValue(
                target,
                value);
        }

        private static Vector2 ReadPrivateVector2(
            object target,
            string fieldName)
        {
            System.Reflection.FieldInfo field =
                target.GetType().GetField(
                    fieldName,
                    System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.NonPublic);

            Assert.That(
                field,
                Is.Not.Null,
                $"{fieldName} was not found.");

            return (Vector2)field.GetValue(
                target);
        }
    }
}