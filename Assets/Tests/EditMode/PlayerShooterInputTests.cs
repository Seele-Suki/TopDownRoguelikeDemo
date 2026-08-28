using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace TopDownRoguelike.Tests.EditMode
{
    public sealed class PlayerShooterInputTests
    {
        private const string ShooterTypeName =
            "TopDownRoguelike.Gameplay.Weapons.PlayerShooter";

        private const string InputSourceTypeName =
            "TopDownRoguelike.Gameplay.Characters.IPlayerInputSource";

        [Test]
        public void ShooterUsesPlayerInputSourceContract()
        {
            Type shooterType =
                FindType(ShooterTypeName);

            Assert.That(
                shooterType,
                Is.Not.Null,
                "PlayerShooter must exist.");

            FieldInfo inputSourceField =
                shooterType.GetField(
                    "inputSource",
                    BindingFlags.Instance |
                    BindingFlags.NonPublic);

            Assert.That(
                inputSourceField,
                Is.Not.Null,
                "PlayerShooter must retain an inputSource field.");

            Type inputSourceType =
                FindType(InputSourceTypeName);

            Assert.That(
                inputSourceType,
                Is.Not.Null,
                $"{InputSourceTypeName} must exist.");

            Assert.That(
                inputSourceField.FieldType,
                Is.EqualTo(inputSourceType));

            FieldInfo mainCameraField =
                shooterType.GetField(
                    "mainCamera",
                    BindingFlags.Instance |
                    BindingFlags.NonPublic);

            Assert.That(
                mainCameraField,
                Is.Null,
                "PlayerShooter must not own a camera input field.");
        }

        [Test]
        public void AddFireRate_ReducesCooldownAndKeepsMinimum()
        {
            Type shooterType =
                FindType(ShooterTypeName);

            Assert.That(
                shooterType,
                Is.Not.Null,
                "PlayerShooter must exist.");

            GameObject player =
                new GameObject(
                    "Player Shooter Fire Rate Test");

            player.SetActive(false);

            try
            {
                Type localInputSourceType =
                    FindType(
                        "TopDownRoguelike.Gameplay.Characters." +
                        "LocalPlayerInputSource");

                Assert.That(
                    localInputSourceType,
                    Is.Not.Null);

                player.AddComponent(
                    localInputSourceType);

                Component shooter =
                    player.AddComponent(
                        shooterType);

                MethodInfo awakeMethod =
                    shooterType.GetMethod(
                        "Awake",
                        BindingFlags.Instance |
                        BindingFlags.NonPublic);

                Assert.That(
                    awakeMethod,
                    Is.Not.Null);

                awakeMethod.Invoke(
                    shooter,
                    null);

                MethodInfo addFireRateMethod =
                    shooterType.GetMethod(
                        "AddFireRate",
                        new[]
                        {
                    typeof(float)
                        });

                Assert.That(
                    addFireRateMethod,
                    Is.Not.Null,
                    "AddFireRate(float) must exist.");

                FieldInfo cooldownField =
                    shooterType.GetField(
                        "fireCooldown",
                        BindingFlags.Instance |
                        BindingFlags.NonPublic);

                Assert.That(
                    cooldownField,
                    Is.Not.Null,
                    "fireCooldown field must exist.");

                float originalCooldown =
                    (float)cooldownField.GetValue(
                        shooter);

                addFireRateMethod.Invoke(
                    shooter,
                    new object[]
                    {
                0.05f
                    });

                float reducedCooldown =
                    (float)cooldownField.GetValue(
                        shooter);

                Assert.That(
                    reducedCooldown,
                    Is.EqualTo(
                        originalCooldown - 0.05f)
                        .Within(0.0001f));

                addFireRateMethod.Invoke(
                    shooter,
                    new object[]
                    {
                10f
                    });

                float minimumCooldown =
                    (float)cooldownField.GetValue(
                        shooter);

                Assert.That(
                    minimumCooldown,
                    Is.EqualTo(0.05f)
                        .Within(0.0001f));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(
                    player);
            }
        }

        [Test]
        public void AddProjectileDamage_IncreasesDamageAndKeepsMinimum()
        {
            Type shooterType =
                FindType(ShooterTypeName);

            Assert.That(
                shooterType,
                Is.Not.Null);

            GameObject player =
                new GameObject(
                    "Player Shooter Damage Test");

            player.SetActive(false);

            try
            {
                Type localInputSourceType =
                    FindType(
                        "TopDownRoguelike.Gameplay.Characters." +
                        "LocalPlayerInputSource");

                player.AddComponent(
                    localInputSourceType);

                Component shooter =
                    player.AddComponent(
                        shooterType);

                MethodInfo awakeMethod =
                    shooterType.GetMethod(
                        "Awake",
                        BindingFlags.Instance |
                        BindingFlags.NonPublic);

                awakeMethod.Invoke(
                    shooter,
                    null);

                MethodInfo addDamageMethod =
                    shooterType.GetMethod(
                        "AddProjectileDamage",
                        new[]
                        {
                    typeof(int)
                        });

                Assert.That(
                    addDamageMethod,
                    Is.Not.Null,
                    "AddProjectileDamage(int) must exist.");

                FieldInfo damageField =
                    shooterType.GetField(
                        "projectileDamage",
                        BindingFlags.Instance |
                        BindingFlags.NonPublic);

                Assert.That(
                    damageField,
                    Is.Not.Null,
                    "projectileDamage field must exist.");

                int originalDamage =
                    (int)damageField.GetValue(
                        shooter);

                addDamageMethod.Invoke(
                    shooter,
                    new object[]
                    {
                2
                    });

                int increasedDamage =
                    (int)damageField.GetValue(
                        shooter);

                Assert.That(
                    increasedDamage,
                    Is.EqualTo(
                        originalDamage + 2));

                addDamageMethod.Invoke(
                    shooter,
                    new object[]
                    {
                -100
                    });

                int finalDamage =
                    (int)damageField.GetValue(
                        shooter);

                Assert.That(
                    finalDamage,
                    Is.GreaterThanOrEqualTo(1));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(
                    player);
            }
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