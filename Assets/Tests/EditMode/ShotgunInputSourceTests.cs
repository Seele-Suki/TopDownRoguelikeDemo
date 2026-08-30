using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace TopDownRoguelike.Tests.EditMode
{
    public sealed class ShotgunInputSourceTests
    {
        private const string InputContractTypeName =
            "TopDownRoguelike.Gameplay.Characters." +
            "IPlayerInputSource";

        private const string LocalInputTypeName =
            "TopDownRoguelike.Gameplay.Characters." +
            "LocalPlayerInputSource";

        private const string RemoteInputTypeName =
            "TopDownRoguelike.Gameplay.Networking." +
            "RemotePlayerInputSource";

        [Test]
        public void ContractExposesReadOnlyShotgunRequestSequence()
        {
            Type inputContractType =
                FindType(
                    InputContractTypeName);

            Assert.That(
                inputContractType,
                Is.Not.Null);

            PropertyInfo property =
                inputContractType.GetProperty(
                    "ShotgunRequestSequence");

            Assert.That(
                property,
                Is.Not.Null,
                "IPlayerInputSource must expose " +
                "ShotgunRequestSequence.");

            Assert.That(
                property.PropertyType,
                Is.EqualTo(typeof(uint)));

            Assert.That(
                property.CanRead,
                Is.True);

            Assert.That(
                property.CanWrite,
                Is.False,
                "ShotgunRequestSequence must be read-only.");
        }

        [Test]
        public void RegisterShotgunRequest_IncrementsOncePerCall()
        {
            Component inputSource =
                CreateComponent(
                    LocalInputTypeName,
                    "Local Shotgun Input Test",
                    out GameObject player);

            try
            {
                PropertyInfo property =
                    inputSource.GetType().GetProperty(
                        "ShotgunRequestSequence");

                Assert.That(
                    property,
                    Is.Not.Null,
                    "LocalPlayerInputSource must expose " +
                    "ShotgunRequestSequence.");

                MethodInfo registerMethod =
                    inputSource.GetType().GetMethod(
                        "RegisterShotgunRequest",
                        BindingFlags.Instance |
                        BindingFlags.NonPublic);

                Assert.That(
                    registerMethod,
                    Is.Not.Null,
                    "LocalPlayerInputSource must retain " +
                    "each shotgun press.");

                Assert.That(
                    (uint)property.GetValue(
                        inputSource),
                    Is.EqualTo(0u));

                registerMethod.Invoke(
                    inputSource,
                    null);

                Assert.That(
                    (uint)property.GetValue(
                        inputSource),
                    Is.EqualTo(1u));

                registerMethod.Invoke(
                    inputSource,
                    null);

                Assert.That(
                    (uint)property.GetValue(
                        inputSource),
                    Is.EqualTo(2u),
                    "Each registered press must increment " +
                    "the sequence exactly once.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(
                    player);
            }
        }

        [Test]
        public void RemoteInputSource_RejectsOlderShotgunSequence()
        {
            Component inputSource =
                CreateComponent(
                    RemoteInputTypeName,
                    "Remote Shotgun Ordering Test",
                    out GameObject player);

            try
            {
                ApplyInputState(
                    inputSource,
                    Vector2.up,
                    Vector2.right,
                    false,
                    10u,
                    40u);

                ApplyInputState(
                    inputSource,
                    Vector2.left,
                    Vector2.down,
                    true,
                    11u,
                    39u);

                Assert.That(
                    ReadProperty<uint>(
                        inputSource,
                        "ShotgunRequestSequence"),
                    Is.EqualTo(40u),
                    "An older shotgun request must not " +
                    "move the sequence backward.");

                Assert.That(
                    ReadProperty<uint>(
                        inputSource,
                        "DashRequestSequence"),
                    Is.EqualTo(11u),
                    "Shotgun filtering must not block a " +
                    "new dash sequence.");

                Assert.That(
                    ReadProperty<Vector2>(
                        inputSource,
                        "MoveDirection"),
                    Is.EqualTo(Vector2.left));

                Assert.That(
                    ReadProperty<Vector2>(
                        inputSource,
                        "AimDirection"),
                    Is.EqualTo(Vector2.down));

                Assert.That(
                    ReadProperty<bool>(
                        inputSource,
                        "IsFireHeld"),
                    Is.True,
                    "Rejecting an old shotgun sequence must " +
                    "not discard continuous input state.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(
                    player);
            }
        }

        [Test]
        public void RemoteInputSource_AcceptsWrapAndRejectsHalfRange()
        {
            Component inputSource =
                CreateComponent(
                    RemoteInputTypeName,
                    "Remote Shotgun Wrap Test",
                    out GameObject player);

            try
            {
                InvokePublic(
                    inputSource,
                    "ClearInput");

                ApplyInputState(
                    inputSource,
                    Vector2.zero,
                    Vector2.right,
                    false,
                    0u,
                    0xFFFFFFFEu);

                ApplyInputState(
                    inputSource,
                    Vector2.zero,
                    Vector2.right,
                    false,
                    0u,
                    0xFFFFFFFFu);

                ApplyInputState(
                    inputSource,
                    Vector2.zero,
                    Vector2.right,
                    false,
                    0u,
                    0u);

                Assert.That(
                    ReadProperty<uint>(
                        inputSource,
                        "ShotgunRequestSequence"),
                    Is.EqualTo(0u),
                    "Sequence wrap from uint.MaxValue to " +
                    "zero must be accepted.");

                ApplyInputState(
                    inputSource,
                    Vector2.zero,
                    Vector2.right,
                    false,
                    0u,
                    0x80000000u);

                Assert.That(
                    ReadProperty<uint>(
                        inputSource,
                        "ShotgunRequestSequence"),
                    Is.EqualTo(0u),
                    "An ambiguous half-range sequence " +
                    "must be rejected.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(
                    player);
            }
        }

        private static Component CreateComponent(
            string typeName,
            string objectName,
            out GameObject gameObject)
        {
            Type componentType =
                FindType(
                    typeName);

            Assert.That(
                componentType,
                Is.Not.Null,
                $"{typeName} must exist.");

            gameObject =
                new GameObject(
                    objectName);

            gameObject.SetActive(false);

            return gameObject.AddComponent(
                componentType);
        }

        private static void ApplyInputState(
            Component inputSource,
            Vector2 moveDirection,
            Vector2 aimDirection,
            bool fireHeld,
            uint dashRequestSequence,
            uint shotgunRequestSequence)
        {
            MethodInfo method =
                inputSource.GetType().GetMethod(
                    "ApplyInputState",
                    BindingFlags.Instance |
                    BindingFlags.Public,
                    null,
                    new[]
                    {
                        typeof(Vector2),
                        typeof(Vector2),
                        typeof(bool),
                        typeof(uint),
                        typeof(uint)
                    },
                    null);

            Assert.That(
                method,
                Is.Not.Null,
                "RemotePlayerInputSource must expose the " +
                "five-argument ApplyInputState overload.");

            method.Invoke(
                inputSource,
                new object[]
                {
                    moveDirection,
                    aimDirection,
                    fireHeld,
                    dashRequestSequence,
                    shotgunRequestSequence
                });
        }

        private static T ReadProperty<T>(
            object target,
            string propertyName)
        {
            PropertyInfo property =
                target.GetType().GetProperty(
                    propertyName,
                    BindingFlags.Instance |
                    BindingFlags.Public);

            Assert.That(
                property,
                Is.Not.Null,
                $"{propertyName} must exist.");

            return (T)property.GetValue(
                target);
        }

        private static void InvokePublic(
            object target,
            string methodName)
        {
            MethodInfo method =
                target.GetType().GetMethod(
                    methodName,
                    BindingFlags.Instance |
                    BindingFlags.Public);

            Assert.That(
                method,
                Is.Not.Null,
                $"{methodName} must exist.");

            method.Invoke(
                target,
                null);
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