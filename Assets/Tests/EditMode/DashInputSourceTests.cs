using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace TopDownRoguelike.Tests.EditMode
{
    public sealed class DashInputSourceTests
    {
        private const string InputSourceTypeName =
            "TopDownRoguelike.Gameplay.Characters." +
            "IPlayerInputSource";

        private const string LocalInputSourceTypeName =
            "TopDownRoguelike.Gameplay.Characters." +
            "LocalPlayerInputSource";

        [Test]
        public void ContractExposesReadOnlyDashRequestSequence()
        {
            Type inputSourceType =
                FindType(InputSourceTypeName);

            Assert.That(
                inputSourceType,
                Is.Not.Null);

            PropertyInfo sequenceProperty =
                inputSourceType.GetProperty(
                    "DashRequestSequence");

            Assert.That(
                sequenceProperty,
                Is.Not.Null,
                "IPlayerInputSource must expose " +
                "DashRequestSequence.");

            Assert.That(
                sequenceProperty.PropertyType,
                Is.EqualTo(typeof(uint)));

            Assert.That(
                sequenceProperty.CanRead,
                Is.True);

            Assert.That(
                sequenceProperty.CanWrite,
                Is.False,
                "DashRequestSequence must be read-only.");
        }

        [Test]
        public void RegisterDashRequest_IncrementsSequenceOncePerCall()
        {
            Type localInputSourceType =
                FindType(LocalInputSourceTypeName);

            Assert.That(
                localInputSourceType,
                Is.Not.Null);

            var player =
                new GameObject(
                    "Dash Input Source Test");

            player.SetActive(false);

            try
            {
                Component inputSource =
                    player.AddComponent(
                        localInputSourceType);

                PropertyInfo sequenceProperty =
                    localInputSourceType.GetProperty(
                        "DashRequestSequence");

                Assert.That(
                    sequenceProperty,
                    Is.Not.Null,
                    "LocalPlayerInputSource must expose " +
                    "DashRequestSequence.");

                MethodInfo registerMethod =
                    localInputSourceType.GetMethod(
                        "RegisterDashRequest",
                        BindingFlags.Instance |
                        BindingFlags.NonPublic);

                Assert.That(
                    registerMethod,
                    Is.Not.Null,
                    "LocalPlayerInputSource must retain " +
                    "each dash press.");

                Assert.That(
                    (uint)sequenceProperty.GetValue(
                        inputSource),
                    Is.EqualTo(0u));

                registerMethod.Invoke(
                    inputSource,
                    null);

                Assert.That(
                    (uint)sequenceProperty.GetValue(
                        inputSource),
                    Is.EqualTo(1u));

                registerMethod.Invoke(
                    inputSource,
                    null);

                Assert.That(
                    (uint)sequenceProperty.GetValue(
                        inputSource),
                    Is.EqualTo(2u));
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