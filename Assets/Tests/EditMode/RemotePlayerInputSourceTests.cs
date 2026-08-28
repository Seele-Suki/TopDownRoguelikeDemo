using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace TopDownRoguelike.Tests.EditMode
{
    public sealed class RemotePlayerInputSourceTests
    {
        private const string InputSourceTypeName =
            "TopDownRoguelike.Gameplay.Characters." +
            "IPlayerInputSource";

        private const string RemoteInputSourceTypeName =
            "TopDownRoguelike.Gameplay.Networking." +
            "RemotePlayerInputSource";

        [Test]
        public void Type_ImplementsPlayerInputSourceContract()
        {
            Type inputSourceType =
                FindType(InputSourceTypeName);

            Type remoteInputSourceType =
                FindType(RemoteInputSourceTypeName);

            Assert.That(inputSourceType, Is.Not.Null);

            Assert.That(
                remoteInputSourceType,
                Is.Not.Null,
                "RemotePlayerInputSource must exist.");

            Assert.That(
                typeof(MonoBehaviour).IsAssignableFrom(
                    remoteInputSourceType),
                Is.True);

            Assert.That(
                inputSourceType.IsAssignableFrom(
                    remoteInputSourceType),
                Is.True);
        }

        [Test]
        public void ApplyInput_StoresMovementAndNormalizedAim()
        {
            Component remoteInput =
                CreateRemoteInput(
                    out GameObject inputObject);

            try
            {
                MethodInfo applyInputMethod =
                    remoteInput.GetType().GetMethod(
                        "ApplyInput");

                Assert.That(
                    applyInputMethod,
                    Is.Not.Null,
                    "ApplyInput must exist.");

                applyInputMethod.Invoke(
                    remoteInput,
                    new object[]
                    {
                        new Vector2(0.6f, 0.8f),
                        new Vector2(3f, 4f)
                    });

                Assert.That(
                    ReadDirection(
                        remoteInput,
                        "MoveDirection"),
                    Is.EqualTo(
                        new Vector2(0.6f, 0.8f)));

                Assert.That(
                    ReadDirection(
                        remoteInput,
                        "AimDirection"),
                    Is.EqualTo(
                        new Vector2(0.6f, 0.8f)));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(
                    inputObject);
            }
        }

        [Test]
        public void ApplyInputWithFireState_StoresFireHeldState()
        {
            Component remoteInput =
                CreateRemoteInput(
                    out GameObject inputObject);

            try
            {
                MethodInfo applyInputMethod =
                    remoteInput.GetType().GetMethod(
                        "ApplyInputWithFireState",
                        new[]
                        {
                    typeof(Vector2),
                    typeof(Vector2),
                    typeof(bool)
                        });

                Assert.That(
                    applyInputMethod,
                    Is.Not.Null,
                    "ApplyInput must accept FireHeld.");

                applyInputMethod.Invoke(
                    remoteInput,
                    new object[]
                    {
                Vector2.zero,
                Vector2.right,
                true
                    });

                PropertyInfo fireProperty =
                    remoteInput.GetType().GetProperty(
                        "IsFireHeld");

                Assert.That(
                    fireProperty,
                    Is.Not.Null,
                    "RemotePlayerInputSource must expose IsFireHeld.");

                Assert.That(
                    (bool)fireProperty.GetValue(
                        remoteInput),
                    Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(
                    inputObject);
            }
        }

        [Test]
        public void ApplyInput_ClampsMovementMagnitudeToOne()
        {
            Component remoteInput =
                CreateRemoteInput(
                    out GameObject inputObject);

            try
            {
                MethodInfo applyInputMethod =
                    remoteInput.GetType().GetMethod(
                        "ApplyInput");

                applyInputMethod.Invoke(
                    remoteInput,
                    new object[]
                    {
                        new Vector2(3f, 4f),
                        Vector2.right
                    });

                Vector2 movement =
                    ReadDirection(
                        remoteInput,
                        "MoveDirection");

                Assert.That(
                    movement,
                    Is.EqualTo(
                        new Vector2(0.6f, 0.8f)));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(
                    inputObject);
            }
        }

        [Test]
        public void ClearInput_ResetsMovementAndAim()
        {
            Component remoteInput =
                CreateRemoteInput(
                    out GameObject inputObject);

            try
            {
                MethodInfo applyInputMethod =
                    remoteInput.GetType().GetMethod(
                        "ApplyInput");

                MethodInfo clearInputMethod =
                    remoteInput.GetType().GetMethod(
                        "ClearInput");

                Assert.That(clearInputMethod, Is.Not.Null);

                applyInputMethod.Invoke(
                    remoteInput,
                    new object[]
                    {
                        Vector2.left,
                        Vector2.up
                    });

                clearInputMethod.Invoke(
                    remoteInput,
                    null);

                Assert.That(
                    ReadDirection(
                        remoteInput,
                        "MoveDirection"),
                    Is.EqualTo(Vector2.zero));

                Assert.That(
                    ReadDirection(
                        remoteInput,
                        "AimDirection"),
                    Is.EqualTo(Vector2.zero));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(
                    inputObject);
            }
        }

        private static Component CreateRemoteInput(
            out GameObject inputObject)
        {
            Type remoteInputSourceType =
                FindType(RemoteInputSourceTypeName);

            Assert.That(
                remoteInputSourceType,
                Is.Not.Null,
                "RemotePlayerInputSource must exist.");

            inputObject =
                new GameObject(
                    "Remote Input Source Test");

            inputObject.SetActive(false);

            return inputObject.AddComponent(
                remoteInputSourceType);
        }

        private static Vector2 ReadDirection(
            Component remoteInput,
            string propertyName)
        {
            PropertyInfo property =
                remoteInput.GetType().GetProperty(
                    propertyName);

            Assert.That(
                property,
                Is.Not.Null,
                $"{propertyName} must exist.");

            return (Vector2)property.GetValue(
                remoteInput);
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