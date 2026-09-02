using System;
using System.Reflection;
using NUnit.Framework;
using TopDownRoguelike.Infrastructure;
using UnityEngine;

namespace TopDownRoguelike.Tests.EditMode
{
    public sealed class EnemyAttackAuthorityTests
    {
        private const string AttackTypeName =
            "TopDownRoguelike.Gameplay.Enemies.EnemyAttack";

        private const string PlayerHealthTypeName =
            "TopDownRoguelike.Gameplay.Characters.PlayerHealth";

        [TearDown]
        public void TearDown()
        {
            GameSession.Reset();
        }

        [TestCase(GameMode.SinglePlayer, 9)]
        [TestCase(GameMode.MultiplayerHost, 9)]
        [TestCase(GameMode.MultiplayerClient, 10)]
        public void Update_OnlyAuthoritativeModesApplyEnemyDamage(
            GameMode mode,
            int expectedHealth)
        {
            ConfigureMode(mode);

            Type attackType = FindType(AttackTypeName);
            Type playerHealthType = FindType(PlayerHealthTypeName);

            Assert.That(attackType, Is.Not.Null);
            Assert.That(playerHealthType, Is.Not.Null);

            var enemyObject = new GameObject("Enemy Attack Authority Test");
            var playerObject = new GameObject("Enemy Attack Target Test");

            enemyObject.SetActive(false);
            playerObject.SetActive(false);

            try
            {
                Component attack = enemyObject.AddComponent(attackType);
                Component playerHealth =
                    playerObject.AddComponent(playerHealthType);

                playerHealthType.GetMethod(
                        "Awake",
                        BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(playerHealth, null);

                SetPrivateField(
                    attack,
                    "target",
                    playerObject.transform);

                attackType.GetMethod(
                        "Update",
                        BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(attack, null);

                int actualHealth = (int)playerHealthType
                    .GetProperty("CurrentHealth")
                    .GetValue(playerHealth, null);

                Assert.That(
                    actualHealth,
                    Is.EqualTo(expectedHealth),
                    mode == GameMode.MultiplayerClient
                        ? "A joining client must not decide enemy hits."
                        : "An authoritative mode must still apply enemy hits.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(playerObject);
                UnityEngine.Object.DestroyImmediate(enemyObject);
            }
        }

        [Test]
        public void Update_ResolvesDamageableOnTargetParent()
        {
            GameSession.ConfigureMultiplayerHost();
            Type attackType = FindType(AttackTypeName);
            Type playerHealthType = FindType(PlayerHealthTypeName);
            var enemy = new GameObject("Parent Target Enemy");
            var player = new GameObject("Parent Target Player");
            var child = new GameObject("Target Child");
            child.transform.SetParent(player.transform);
            try
            {
                enemy.SetActive(false);
                player.SetActive(false);
                Component attack = enemy.AddComponent(attackType);
                Component health = player.AddComponent(playerHealthType);
                playerHealthType.GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(health, null);
                SetPrivateField(attack, "target", child.transform);
                attackType.GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(attack, null);
                Assert.That((int)playerHealthType.GetProperty("CurrentHealth").GetValue(health), Is.EqualTo(9));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(player);
                UnityEngine.Object.DestroyImmediate(enemy);
            }
        }

        private static void ConfigureMode(GameMode mode)
        {
            switch (mode)
            {
                case GameMode.SinglePlayer:
                    GameSession.ConfigureSinglePlayer();
                    return;

                case GameMode.MultiplayerHost:
                    GameSession.ConfigureMultiplayerHost();
                    return;

                case GameMode.MultiplayerClient:
                    GameSession.ConfigureMultiplayerClient();
                    return;

                default:
                    throw new ArgumentOutOfRangeException(nameof(mode));
            }
        }

        private static void SetPrivateField(
            Component target,
            string fieldName,
            object value)
        {
            FieldInfo field = target.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(field, Is.Not.Null);
            field.SetValue(target, value);
        }

        private static Type FindType(string fullName)
        {
            foreach (Assembly assembly
                in AppDomain.CurrentDomain.GetAssemblies())
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
