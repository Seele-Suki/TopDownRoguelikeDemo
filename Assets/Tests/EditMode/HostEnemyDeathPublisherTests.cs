using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TopDownRoguelike.Infrastructure;
using TopDownRoguelike.Networking.Protocol;
using UnityEngine;
using UnityEngine.TestTools;

namespace TopDownRoguelike.Tests.EditMode
{
    public sealed class HostEnemyDeathPublisherTests
    {
        private readonly List<GameObject> createdObjects =
            new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            GameSession.Reset();

            foreach (GameObject createdObject in createdObjects)
            {
                if (createdObject != null)
                {
                    UnityEngine.Object.DestroyImmediate(createdObject);
                }
            }

            createdObjects.Clear();
        }

        [Test]
        public void EnemyDeath_HostPublishesReliableRemovalOnce()
        {
            GameSession.ConfigureMultiplayerHost();

            Type spawnerType = FindType(
                "TopDownRoguelike.Gameplay.Enemies.EnemySpawner");
            Type healthType = FindType(
                "TopDownRoguelike.Gameplay.Enemies.EnemyHealth");
            Type gameManagerType = FindType(
                "TopDownRoguelike.Gameplay.Core.GameManager");
            Type publisherType = FindType(
                "TopDownRoguelike.Gameplay.Networking." +
                "HostEnemyDeathPublisher");

            Assert.That(spawnerType, Is.Not.Null);
            Assert.That(healthType, Is.Not.Null);
            Assert.That(gameManagerType, Is.Not.Null);
            Assert.That(publisherType, Is.Not.Null);

            GameObject spawnerObject = CreateObject("Death Spawner");
            GameObject publisherObject = CreateObject("Death Publisher");
            GameObject enemyPrefab = CreateObject("Death Enemy Prefab");
            GameObject gameManagerObject = CreateObject("Death Game Manager");

            spawnerObject.SetActive(false);
            publisherObject.SetActive(false);
            gameManagerObject.SetActive(false);
            enemyPrefab.AddComponent(healthType);

            Component spawner = spawnerObject.AddComponent(spawnerType);
            Component publisher = publisherObject.AddComponent(publisherType);
            Component gameManager =
                gameManagerObject.AddComponent(gameManagerType);

            FieldInfo gameManagerField = spawnerType.GetField(
                "gameManager",
                BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(gameManagerField, Is.Not.Null);
            gameManagerField.SetValue(spawner, gameManager);

            int publishCount = 0;
            WorldEntityRemovedPayload published = null;

            Action<WorldEntityRemovedPayload> sender = removed =>
            {
                publishCount++;
                published = removed;
            };

            publisherType.GetMethod("Configure")
                .Invoke(publisher, new object[] { spawner, sender });

            MethodInfo createMethod = spawnerType.GetMethod(
                "TryCreateSpawnedEnemy",
                BindingFlags.Instance | BindingFlags.NonPublic);

            object[] arguments =
            {
                enemyPrefab,
                Vector3.zero,
                null
            };

            Assert.That(
                (bool)createMethod.Invoke(spawner, arguments),
                Is.True);

            GameObject enemy = arguments[2] as GameObject;
            createdObjects.Add(enemy);

            LogAssert.Expect(
                LogType.Error,
                "Destroy may not be called from edit mode! " +
                "Use DestroyImmediate instead.\n" +
                "Destroying an object in edit mode destroys it permanently.");

            InvokeTakeDamage(enemy.GetComponent(healthType), 3);

            Assert.That(publishCount, Is.EqualTo(1));
            Assert.That(published, Is.Not.Null);
            Assert.That(published.EntityId, Is.EqualTo(0x10000001u));
            Assert.That(published.EntityType, Is.EqualTo(NetworkEntityType.Enemy));
            Assert.That(published.Reason, Is.EqualTo(WorldEntityRemovalReason.Died));
        }

        private GameObject CreateObject(string name)
        {
            var result = new GameObject(name);
            createdObjects.Add(result);
            return result;
        }

        private static void InvokeTakeDamage(Component health, int damage)
        {
            MethodInfo method = health.GetType().GetMethod("TakeDamage");
            Type damageType = method.GetParameters()[0].ParameterType;
            object damageInfo = Activator.CreateInstance(damageType);
            damageType.GetField("Damage").SetValue(damageInfo, damage);
            method.Invoke(health, new[] { damageInfo });
        }

        private static Type FindType(string fullName)
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type result = assembly.GetType(fullName, false);

                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }
    }
}
