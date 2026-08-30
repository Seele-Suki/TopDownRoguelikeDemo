using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TopDownRoguelike.Networking.Protocol;
using UnityEngine;

namespace TopDownRoguelike.Tests.EditMode
{
    public sealed class ShotgunInputFlowTests
    {
        private const string PublisherTypeName =
            "TopDownRoguelike.Gameplay.Networking." +
            "ClientPlayerInputPublisher";

        private const string RemoteInputTypeName =
            "TopDownRoguelike.Gameplay.Networking." +
            "RemotePlayerInputSource";

        private const string BootstrapTypeName =
            "TopDownRoguelike.Gameplay.Networking." +
            "NetworkGameBootstrap";

        private const string RegistryTypeName =
            "TopDownRoguelike.Networking.Gameplay." +
            "NetworkPlayerRegistry";

        [Test]
        public void Advance_PublishesShotgunRequestSequence()
        {
            Component publisher =
                CreateComponent(
                    PublisherTypeName,
                    "Shotgun Publisher Test",
                    out GameObject publisherObject);

            Component remoteInput =
                CreateComponent(
                    RemoteInputTypeName,
                    "Shotgun Publisher Input Test",
                    out GameObject inputObject);

            try
            {
                MethodInfo applyMethod =
                    remoteInput.GetType().GetMethod(
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
                    applyMethod,
                    Is.Not.Null,
                    "RemotePlayerInputSource must accept " +
                    "both request sequences.");

                applyMethod.Invoke(
                    remoteInput,
                    new object[]
                    {
                        Vector2.up,
                        Vector2.right,
                        false,
                        17u,
                        23u
                    });

                var sentInputs =
                    new List<PlayerInputPayload>();

                MethodInfo configureMethod =
                    publisher.GetType().GetMethod(
                        "Configure");

                Assert.That(
                    configureMethod,
                    Is.Not.Null);

                configureMethod.Invoke(
                    publisher,
                    new object[]
                    {
                        remoteInput,
                        new Action<PlayerInputPayload>(
                            sentInputs.Add)
                    });

                InvokePrivate(
                    publisher,
                    "Advance",
                    0.051f);

                Assert.That(
                    sentInputs.Count,
                    Is.EqualTo(1));

                Assert.That(
                    sentInputs[0].DashRequestSequence,
                    Is.EqualTo(17u));

                Assert.That(
                    sentInputs[0].ShotgunRequestSequence,
                    Is.EqualTo(23u),
                    "The client publisher must include the " +
                    "shotgun request sequence.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(
                    publisherObject);

                UnityEngine.Object.DestroyImmediate(
                    inputObject);
            }
        }

        [Test]
        public void HandleRemotePlayerInput_AppliesShotgunSequence()
        {
            Type registryType =
                FindType(RegistryTypeName);

            Assert.That(
                registryType,
                Is.Not.Null);

            Component bootstrap =
                CreateComponent(
                    BootstrapTypeName,
                    "Shotgun Bootstrap Test",
                    out GameObject bootstrapObject);

            Component remoteInput =
                CreateComponent(
                    RemoteInputTypeName,
                    "Host Remote Shotgun Player",
                    out GameObject remotePlayer);

            try
            {
                object registry =
                    Activator.CreateInstance(
                        registryType);

                MethodInfo registerMethod =
                    registryType.GetMethod(
                        "TryRegister");

                Assert.That(
                    registerMethod,
                    Is.Not.Null);

                bool registered =
                    (bool)registerMethod.Invoke(
                        registry,
                        new object[]
                        {
                            22u,
                            remotePlayer
                        });

                Assert.That(
                    registered,
                    Is.True);

                SetPrivateField(
                    bootstrap,
                    "registry",
                    registry);

                var input =
                    new PlayerInputPayload(
                        0.6f,
                        0.8f,
                        -1f,
                        0.25f,
                        true,
                        31u,
                        41u);

                InvokePrivate(
                    bootstrap,
                    "HandleRemotePlayerInput",
                    22u,
                    input);

                Assert.That(
                    ReadUIntProperty(
                        remoteInput,
                        "DashRequestSequence"),
                    Is.EqualTo(31u));

                Assert.That(
                    ReadUIntProperty(
                        remoteInput,
                        "ShotgunRequestSequence"),
                    Is.EqualTo(41u),
                    "The host must apply the forwarded " +
                    "shotgun request sequence.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(
                    bootstrapObject);

                UnityEngine.Object.DestroyImmediate(
                    remotePlayer);
            }
        }

        private static Component CreateComponent(
            string typeName,
            string objectName,
            out GameObject gameObject)
        {
            Type componentType =
                FindType(typeName);

            Assert.That(
                componentType,
                Is.Not.Null,
                $"{typeName} must exist.");

            gameObject =
                new GameObject(objectName);

            gameObject.SetActive(false);

            return gameObject.AddComponent(
                componentType);
        }

        private static uint ReadUIntProperty(
            Component target,
            string propertyName)
        {
            PropertyInfo property =
                target.GetType().GetProperty(
                    propertyName);

            Assert.That(
                property,
                Is.Not.Null,
                $"{propertyName} must exist.");

            return (uint)property.GetValue(
                target);
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

            field.SetValue(
                target,
                value);
        }

        private static void InvokePrivate(
            Component target,
            string methodName,
            params object[] arguments)
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

            method.Invoke(
                target,
                arguments);
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