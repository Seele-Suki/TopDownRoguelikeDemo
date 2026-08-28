using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace TopDownRoguelike.Tests.EditMode
{
    public sealed class PlayerInputSourceContractTests
    {
        private const string InputSourceTypeName =
            "TopDownRoguelike.Gameplay.Characters.IPlayerInputSource";

        private const string LocalInputSourceTypeName =
            "TopDownRoguelike.Gameplay.Characters.LocalPlayerInputSource";

        private const string PlayerControllerTypeName =
            "PlayerController";

        [Test]
        public void ContractExposesReadOnlyMoveAndAimDirections()
        {
            Type inputSourceType = FindType(InputSourceTypeName);

            Assert.That(
                inputSourceType,
                Is.Not.Null,
                $"{InputSourceTypeName} must exist.");

            Assert.That(
                inputSourceType.IsInterface,
                Is.True);

            AssertReadOnlyVector2Property(
                inputSourceType,
                "MoveDirection");

            AssertReadOnlyVector2Property(
                inputSourceType,
                "AimDirection");
        }

        [Test]
        public void ContractExposesReadOnlyFireState()
        {
            Type inputSourceType =
                FindType(InputSourceTypeName);

            Assert.That(
                inputSourceType,
                Is.Not.Null,
                $"{InputSourceTypeName} must exist.");

            PropertyInfo fireProperty =
                inputSourceType.GetProperty(
                    "IsFireHeld");

            Assert.That(
                fireProperty,
                Is.Not.Null,
                "IPlayerInputSource must expose IsFireHeld.");

            Assert.That(
                fireProperty.PropertyType,
                Is.EqualTo(typeof(bool)));

            Assert.That(
                fireProperty.CanRead,
                Is.True);

            Assert.That(
                fireProperty.CanWrite,
                Is.False,
                "IsFireHeld must be read-only.");
        }

        [Test]
        public void LocalInputSourceIsMonoBehaviourAndImplementsContract()
        {
            Type inputSourceType =
                FindType(InputSourceTypeName);

            Type localInputSourceType =
                FindType(LocalInputSourceTypeName);

            Assert.That(
                inputSourceType,
                Is.Not.Null);

            Assert.That(
                localInputSourceType,
                Is.Not.Null,
                $"{LocalInputSourceTypeName} must exist.");

            Assert.That(
                typeof(MonoBehaviour).IsAssignableFrom(
                    localInputSourceType),
                Is.True);

            Assert.That(
                inputSourceType.IsAssignableFrom(
                    localInputSourceType),
                Is.True);
        }

        [Test]
        public void PlayerControllerAcceptsInputSourceContract()
        {
            Type inputSourceType =
                FindType(InputSourceTypeName);

            Type playerControllerType =
                FindType(PlayerControllerTypeName);

            Assert.That(
                inputSourceType,
                Is.Not.Null);

            Assert.That(
                playerControllerType,
                Is.Not.Null);

            FieldInfo inputSourceField =
                playerControllerType.GetField(
                    "inputSource",
                    BindingFlags.Instance |
                    BindingFlags.NonPublic);

            Assert.That(
                inputSourceField,
                Is.Not.Null,
                "PlayerController must retain its input source.");

            Assert.That(
                inputSourceField.FieldType,
                Is.EqualTo(inputSourceType));

            MethodInfo setInputSourceMethod =
                playerControllerType.GetMethod(
                    "SetInputSource",
                    BindingFlags.Instance |
                    BindingFlags.Public,
                    null,
                    new[]
                    {
                inputSourceType
                    },
                    null);

            Assert.That(
                setInputSourceMethod,
                Is.Not.Null,
                "PlayerController must allow bootstrap code " +
                "to replace its input source.");

            Assert.That(
                playerControllerType.GetField(
                    "mainCamera",
                    BindingFlags.Instance |
                    BindingFlags.NonPublic),
                Is.Null);

            Assert.That(
                playerControllerType.GetMethod(
                    "ReadMovementInput",
                    BindingFlags.Instance |
                    BindingFlags.NonPublic),
                Is.Null);

            Assert.That(
                playerControllerType.GetMethod(
                    "ReadMousePosition",
                    BindingFlags.Instance |
                    BindingFlags.NonPublic),
                Is.Null);
        }

        [Test]
        public void PlayerControllerDisablesItselfWhenInputSourceIsMissing()
        {
            Type playerControllerType =
                FindType(PlayerControllerTypeName);

            var player =
                new GameObject(
                    "Missing Input Source Test");

            player.SetActive(false);

            try
            {
                Rigidbody2D body =
                    player.AddComponent<Rigidbody2D>();

                Component controller =
                    player.AddComponent(
                        playerControllerType);

                MethodInfo awakeMethod =
                    playerControllerType.GetMethod(
                        "Awake",
                        BindingFlags.Instance |
                        BindingFlags.NonPublic);

                Assert.That(
                    awakeMethod,
                    Is.Not.Null);

                LogAssert.Expect(
                    LogType.Error,
                    "PlayerController requires an " +
                    "IPlayerInputSource component.");

                awakeMethod.Invoke(
                    controller,
                    null);

                Assert.That(
                    ((Behaviour)controller).enabled,
                    Is.False);

                Assert.That(
                    body.velocity,
                    Is.EqualTo(Vector2.zero));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(
                    player);
            }
        }

        [Test]
        public void PlayerControllerRejectsNullInputSource()
        {
            Type playerControllerType =
                FindType(PlayerControllerTypeName);

            Type localInputSourceType =
                FindType(LocalInputSourceTypeName);

            Type inputSourceType =
                FindType(InputSourceTypeName);

            var player =
                new GameObject(
                    "Null Input Source Test");

            player.SetActive(false);

            try
            {
                player.AddComponent<Rigidbody2D>();

                player.AddComponent(
                    localInputSourceType);

                Component controller =
                    player.AddComponent(
                        playerControllerType);

                player.SetActive(true);

                MethodInfo setInputSource =
                    playerControllerType.GetMethod(
                        "SetInputSource",
                        BindingFlags.Instance |
                        BindingFlags.Public,
                        null,
                        new[]
                        {
                    inputSourceType
                        },
                        null);

                TargetInvocationException exception =
                    Assert.Throws<TargetInvocationException>(
                        () => setInputSource.Invoke(
                            controller,
                            new object[]
                            {
                        null
                            }));

                Assert.That(
                    exception.InnerException,
                    Is.TypeOf<ArgumentNullException>());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(
                    player);
            }
        }

        private static Type FindType(string fullTypeName)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type result = assembly.GetType(
                    fullTypeName,
                    false);

                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }

        private static void AssertReadOnlyVector2Property(
            Type inputSourceType,
            string propertyName)
        {
            var property =
                inputSourceType.GetProperty(propertyName);

            Assert.That(
                property,
                Is.Not.Null,
                $"{propertyName} must exist.");

            Assert.That(
                property.PropertyType,
                Is.EqualTo(typeof(Vector2)));

            Assert.That(
                property.CanRead,
                Is.True);

            Assert.That(
                property.CanWrite,
                Is.False);
        }
    }
}