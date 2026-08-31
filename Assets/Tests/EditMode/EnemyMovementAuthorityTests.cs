using System;
using System.Reflection;
using NUnit.Framework;
using TopDownRoguelike.Infrastructure;
using UnityEngine;

namespace TopDownRoguelike.Tests.EditMode
{
    public sealed class EnemyMovementAuthorityTests
    {
        private const string MovementTypeName =
            "TopDownRoguelike.Gameplay.Enemies." +
            "EnemyMovement";

        private Type movementType;

        [SetUp]
        public void SetUp()
        {
            movementType =
                FindType(MovementTypeName);

            Assert.That(
                movementType,
                Is.Not.Null,
                "EnemyMovement must exist.");
        }

        [TearDown]
        public void TearDown()
        {
            GameSession.Reset();
        }

        [Test]
        public void MoveDirection_IsPublicReadOnlyState()
        {
            PropertyInfo property =
                movementType.GetProperty(
                    "MoveDirection",
                    BindingFlags.Instance |
                    BindingFlags.Public);

            Assert.That(
                property,
                Is.Not.Null,
                "EnemyMovement must expose its last " +
                "authoritative movement direction.");

            Assert.That(property.CanRead, Is.True);
            Assert.That(
                property.CanWrite,
                Is.False,
                "MoveDirection must be read-only.");
        }

        [TestCase(GameMode.SinglePlayer)]
        [TestCase(GameMode.MultiplayerHost)]
        public void FixedUpdate_AuthoritativeModeMovesTowardTarget(
            GameMode mode)
        {
            ConfigureMode(mode);

            GameObject enemy =
                CreateMovement(
                    out Rigidbody2D body,
                    out Component movement);

            var target =
                new GameObject(
                    "Enemy Movement Target");

            try
            {
                body.position =
                    Vector2.zero;

                target.transform.position =
                    new Vector3(3f, 4f, 0f);

                SetPrivateField(
                    movement,
                    "target",
                    target.transform);

                InvokePrivate(
                    movement,
                    "FixedUpdate");

                Assert.That(
                    body.velocity.x,
                    Is.EqualTo(1.2f).Within(0.001f));

                Assert.That(
                    body.velocity.y,
                    Is.EqualTo(1.6f).Within(0.001f));

                Assert.That(
                    ReadMoveDirection(movement),
                    Is.EqualTo(new Vector2(0.6f, 0.8f)));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(
                    target);

                UnityEngine.Object.DestroyImmediate(
                    enemy);
            }
        }

        [Test]
        public void FixedUpdate_MultiplayerClientStopsLocalMovement()
        {
            GameSession.ConfigureMultiplayerClient();

            GameObject enemy =
                CreateMovement(
                    out Rigidbody2D body,
                    out Component movement);

            var target =
                new GameObject(
                    "Client Enemy Movement Target");

            try
            {
                body.position =
                    Vector2.zero;

                body.velocity =
                    new Vector2(5f, -2f);

                target.transform.position =
                    new Vector3(3f, 4f, 0f);

                SetPrivateField(
                    movement,
                    "target",
                    target.transform);

                InvokePrivate(
                    movement,
                    "FixedUpdate");

                Assert.That(
                    body.velocity,
                    Is.EqualTo(Vector2.zero),
                    "A joining client must not simulate " +
                    "enemy movement locally.");

                Assert.That(
                    ReadMoveDirection(movement),
                    Is.EqualTo(Vector2.zero));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(
                    target);

                UnityEngine.Object.DestroyImmediate(
                    enemy);
            }
        }

        private GameObject CreateMovement(
            out Rigidbody2D body,
            out Component movement)
        {
            var enemy =
                new GameObject(
                    "Enemy Movement Authority Test");

            enemy.SetActive(false);

            body =
                enemy.AddComponent<Rigidbody2D>();

            body.gravityScale =
                0f;

            movement =
                enemy.AddComponent(
                    movementType);

            InvokePrivate(
                movement,
                "Awake");

            enemy.SetActive(true);

            return enemy;
        }

        private Vector2 ReadMoveDirection(
            Component movement)
        {
            PropertyInfo property =
                movementType.GetProperty(
                    "MoveDirection",
                    BindingFlags.Instance |
                    BindingFlags.Public);

            Assert.That(property, Is.Not.Null);

            return (Vector2)property.GetValue(
                movement);
        }

        private static void ConfigureMode(
            GameMode mode)
        {
            switch (mode)
            {
                case GameMode.SinglePlayer:
                    GameSession.ConfigureSinglePlayer();
                    break;

                case GameMode.MultiplayerHost:
                    GameSession.ConfigureMultiplayerHost();
                    break;

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(mode));
            }
        }

        private static void SetPrivateField(
            Component target,
            string fieldName,
            object value)
        {
            FieldInfo field =
                target.GetType().GetField(
                    fieldName,
                    BindingFlags.Instance |
                    BindingFlags.NonPublic);

            Assert.That(
                field,
                Is.Not.Null,
                $"{fieldName} must exist.");

            field.SetValue(target, value);
        }

        private static void InvokePrivate(
            Component target,
            string methodName)
        {
            MethodInfo method =
                target.GetType().GetMethod(
                    methodName,
                    BindingFlags.Instance |
                    BindingFlags.NonPublic);

            Assert.That(
                method,
                Is.Not.Null,
                $"{methodName} must exist.");

            method.Invoke(target, null);
        }

        private static Type FindType(
            string fullTypeName)
        {
            foreach (Assembly assembly in
                AppDomain.CurrentDomain.GetAssemblies())
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
