using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TopDownRoguelike.Networking.Gameplay;
using UnityEngine;

namespace TopDownRoguelike.Tests.EditMode
{
    public sealed class HostPlayerHealthCollectionTests
    {
        private readonly List<GameObject> createdObjects =
            new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject createdObject
                in createdObjects)
            {
                if (createdObject != null)
                {
                    UnityEngine.Object.DestroyImmediate(
                        createdObject);
                }
            }

            createdObjects.Clear();
        }

        [Test]
        public void CollectCurrentPlayerHealth_ReturnsCurrentAndMaximumHealth()
        {
            Type publisherType =
                FindType(
                    "TopDownRoguelike.Gameplay.Networking." +
                    "HostWorldSnapshotPublisher");

            Type spawnerType =
                FindType(
                    "TopDownRoguelike.Gameplay.Enemies." +
                    "EnemySpawner");

            Type encounterType =
                FindType(
                    "TopDownRoguelike.Gameplay.Bosses." +
                    "BossEncounterController");

            Type playerHealthType =
                FindType(
                    "TopDownRoguelike.Gameplay.Characters." +
                    "PlayerHealth");

            Assert.That(publisherType, Is.Not.Null);
            Assert.That(spawnerType, Is.Not.Null);
            Assert.That(encounterType, Is.Not.Null);
            Assert.That(playerHealthType, Is.Not.Null);

            var playerRegistry =
                new NetworkPlayerRegistry();

            GameObject player =
                CreateObject("Player");

            Component playerHealth =
                player.AddComponent(
                    playerHealthType);

            FieldInfo currentHealthField =
                playerHealthType.GetField(
                    "currentHealth",
                    BindingFlags.Instance |
                    BindingFlags.NonPublic);

            Assert.That(
                currentHealthField,
                Is.Not.Null);

            currentHealthField.SetValue(
                playerHealth,
                6);

            playerRegistry.TryRegister(
                1u,
                player);

            GameObject spawnerObject =
                CreateObject("Enemy Spawner");

            spawnerObject.SetActive(false);

            Component spawner =
                spawnerObject.AddComponent(
                    spawnerType);

            GameObject encounterObject =
                CreateObject("Boss Encounter");

            encounterObject.SetActive(false);

            Component encounter =
                encounterObject.AddComponent(
                    encounterType);

            GameObject publisherObject =
                CreateObject("Publisher");

            publisherObject.SetActive(false);

            Component publisher =
                publisherObject.AddComponent(
                    publisherType);

            MethodInfo configureMethod =
                publisherType.GetMethod(
                    "ConfigureWorldSources",
                    BindingFlags.Instance |
                    BindingFlags.Public);

            Assert.That(
                configureMethod,
                Is.Not.Null);

            configureMethod.Invoke(
                publisher,
                new object[]
                {
                    playerRegistry,
                    spawner,
                    encounter
                });

            MethodInfo collectMethod =
                publisherType.GetMethod(
                    "CollectCurrentPlayerHealth",
                    BindingFlags.Instance |
                    BindingFlags.Public);

            Assert.That(
                collectMethod,
                Is.Not.Null,
                "CollectCurrentPlayerHealth must exist.");

            object result =
                collectMethod.Invoke(
                    publisher,
                    null);

            var states =
                new List<object>();

            foreach (object state
                in (IEnumerable)result)
            {
                states.Add(state);
            }

            Assert.That(
                states,
                Has.Count.EqualTo(1));

            object playerState =
                states[0];

            Type stateType =
                playerState.GetType();

            PropertyInfo playerIdProperty =
                stateType.GetProperty(
                    "PlayerId");

            PropertyInfo currentHealthProperty =
                stateType.GetProperty(
                    "CurrentHealth");

            PropertyInfo maxHealthProperty =
                stateType.GetProperty(
                    "MaxHealth");

            Assert.That(
                playerIdProperty,
                Is.Not.Null);

            Assert.That(
                currentHealthProperty,
                Is.Not.Null);

            Assert.That(
                maxHealthProperty,
                Is.Not.Null);

            Assert.That(
                playerIdProperty.GetValue(
                    playerState,
                    null),
                Is.EqualTo(1u));

            Assert.That(
                currentHealthProperty.GetValue(
                    playerState,
                    null),
                Is.EqualTo((ushort)6));

            Assert.That(
                maxHealthProperty.GetValue(
                    playerState,
                    null),
                Is.EqualTo((ushort)10));
        }

        [Test]
        public void PlayerHealthState_RejectsZeroMaximumHealth()
        {
            Type stateType =
                FindType(
                    "TopDownRoguelike.Gameplay.Networking." +
                    "PlayerHealthState");

            Assert.That(
                stateType,
                Is.Not.Null,
                "PlayerHealthState must exist.");

            ConstructorInfo constructor =
                stateType.GetConstructor(
                    new Type[]
                    {
                        typeof(uint),
                        typeof(int),
                        typeof(int)
                    });

            Assert.That(
                constructor,
                Is.Not.Null);

            TargetInvocationException exception =
                Assert.Throws<TargetInvocationException>(
                    () =>
                        constructor.Invoke(
                            new object[]
                            {
                                1u,
                                0,
                                0
                            }));

            Assert.That(
                exception.InnerException,
                Is.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void PlayerHealthState_RejectsCurrentHealthAboveMaximum()
        {
            Type stateType =
                FindType(
                    "TopDownRoguelike.Gameplay.Networking." +
                    "PlayerHealthState");

            Assert.That(
                stateType,
                Is.Not.Null);

            ConstructorInfo constructor =
                stateType.GetConstructor(
                    new Type[]
                    {
                        typeof(uint),
                        typeof(int),
                        typeof(int)
                    });

            Assert.That(
                constructor,
                Is.Not.Null);

            TargetInvocationException exception =
                Assert.Throws<TargetInvocationException>(
                    () =>
                        constructor.Invoke(
                            new object[]
                            {
                                1u,
                                11,
                                10
                            }));

            Assert.That(
                exception.InnerException,
                Is.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void PlayerHealthState_ZeroCurrentHealthMeansDead()
        {
            Type stateType =
                FindType(
                    "TopDownRoguelike.Gameplay.Networking." +
                    "PlayerHealthState");

            Assert.That(
                stateType,
                Is.Not.Null);

            ConstructorInfo constructor =
                stateType.GetConstructor(
                    new Type[]
                    {
                        typeof(uint),
                        typeof(int),
                        typeof(int)
                    });

            Assert.That(
                constructor,
                Is.Not.Null);

            object state =
                constructor.Invoke(
                    new object[]
                    {
                        1u,
                        0,
                        10
                    });

            PropertyInfo isDeadProperty =
                stateType.GetProperty(
                    "IsDead");

            Assert.That(
                isDeadProperty,
                Is.Not.Null);

            Assert.That(
                isDeadProperty.GetValue(
                    state,
                    null),
                Is.True);
        }

        private GameObject CreateObject(
            string objectName)
        {
            GameObject result =
                new GameObject(objectName);

            createdObjects.Add(result);
            return result;
        }

        private static Type FindType(
            string fullTypeName)
        {
            foreach (Assembly assembly
                in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type result =
                    assembly.GetType(
                        fullTypeName,
                        false);

                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }
    }
}