using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TopDownRoguelike.Networking.Protocol;
using UnityEngine;

namespace TopDownRoguelike.Tests.EditMode
{
    public sealed class ClientPlayerInputPublisherTests
    {
        private const string PublisherTypeName =
            "TopDownRoguelike.Gameplay.Networking." +
            "ClientPlayerInputPublisher";

        private const string RemoteInputTypeName =
            "TopDownRoguelike.Gameplay.Networking." +
            "RemotePlayerInputSource";

        [Test]
        public void Advance_PublishesLatestInputAtTwentyHertz()
        {
            Type publisherType =
                FindType(PublisherTypeName);

            Type remoteInputType =
                FindType(RemoteInputTypeName);

            Assert.That(
                publisherType,
                Is.Not.Null,
                "ClientPlayerInputPublisher must exist.");

            Assert.That(remoteInputType, Is.Not.Null);

            var publisherObject =
                new GameObject("Input Publisher Test");

            var inputObject =
                new GameObject("Input Source Test");

            publisherObject.SetActive(false);
            inputObject.SetActive(false);

            try
            {
                Component publisher =
                    publisherObject.AddComponent(
                        publisherType);

                Component inputSource =
                    inputObject.AddComponent(
                        remoteInputType);

                remoteInputType.GetMethod(
                        "ApplyInput")
                    .Invoke(
                        inputSource,
                        new object[]
                        {
                            new Vector2(0.6f, 0.8f),
                            Vector2.right
                        });

                var sentInputs =
                    new List<PlayerInputPayload>();

                Action<PlayerInputPayload> sender =
                    sentInputs.Add;

                MethodInfo configureMethod =
                    publisherType.GetMethod(
                        "Configure");

                Assert.That(configureMethod, Is.Not.Null);

                configureMethod.Invoke(
                    publisher,
                    new object[]
                    {
                        inputSource,
                        sender
                    });

                InvokeAdvance(
                    publisher,
                    0.049f);

                Assert.That(
                    sentInputs.Count,
                    Is.EqualTo(0));

                InvokeAdvance(
                    publisher,
                    0.002f);

                Assert.That(
                    sentInputs.Count,
                    Is.EqualTo(1));

                Assert.That(
                    sentInputs[0].MoveX,
                    Is.EqualTo(0.6f).Within(0.001f));

                Assert.That(
                    sentInputs[0].MoveY,
                    Is.EqualTo(0.8f).Within(0.001f));

                Assert.That(
                    sentInputs[0].AimX,
                    Is.EqualTo(1f).Within(0.001f));

                Assert.That(
                    sentInputs[0].AimY,
                    Is.EqualTo(0f).Within(0.001f));

                InvokeAdvance(
                    publisher,
                    0.051f);

                Assert.That(
                    sentInputs.Count,
                    Is.EqualTo(2));
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
        public void Advance_WhenCanSendReturnsFalse_DoesNotSendInput()
        {
            Type publisherType =
                FindType(PublisherTypeName);

            Type remoteInputType =
                FindType(RemoteInputTypeName);

            var publisherObject =
                new GameObject("Disconnected Input Publisher Test");

            var inputObject =
                new GameObject("Disconnected Input Source Test");

            publisherObject.SetActive(false);
            inputObject.SetActive(false);

            try
            {
                Component publisher =
                    publisherObject.AddComponent(publisherType);

                Component inputSource =
                    inputObject.AddComponent(remoteInputType);

                var sentInputs =
                    new List<PlayerInputPayload>();

                MethodInfo configureMethod =
                    publisherType.GetMethod(
                        "ConfigureWithStateGuard",
                        new[]
                        {
                            remoteInputType,
                            typeof(Action<PlayerInputPayload>),
                            typeof(Func<bool>)
                        });

                Assert.That(configureMethod, Is.Not.Null);

                configureMethod.Invoke(
                    publisher,
                    new object[]
                    {
                        inputSource,
                        new Action<PlayerInputPayload>(
                            sentInputs.Add),
                        new Func<bool>(() => false)
                    });

                InvokeAdvance(
                    publisher,
                    0.1f);

                Assert.That(
                    sentInputs,
                    Is.Empty,
                    "A disconnected client must not publish input.");
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
        public void Advance_SendFailure_DoesNotBubbleToGameplay()
        {
            Type publisherType = FindType(PublisherTypeName);
            Type remoteInputType = FindType(RemoteInputTypeName);
            var publisherObject = new GameObject("Input Failure Publisher Test");
            var inputObject = new GameObject("Input Failure Source Test");
            publisherObject.SetActive(false);
            inputObject.SetActive(false);
            try
            {
                Component publisher = publisherObject.AddComponent(publisherType);
                Component inputSource = inputObject.AddComponent(remoteInputType);
                MethodInfo configure = publisherType.GetMethod("Configure");
                configure.Invoke(publisher, new object[]
                {
                    inputSource,
                    new Action<PlayerInputPayload>(_ => throw new InvalidOperationException("send failed"))
                });

                Assert.DoesNotThrow(() => InvokeAdvance(publisher, 0.051f));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(publisherObject);
                UnityEngine.Object.DestroyImmediate(inputObject);
            }
        }

        private static void InvokeAdvance(
            Component publisher,
            float deltaTime)
        {
            MethodInfo advanceMethod =
                publisher.GetType().GetMethod(
                    "Advance",
                    BindingFlags.Instance |
                    BindingFlags.NonPublic);

            Assert.That(
                advanceMethod,
                Is.Not.Null,
                "Advance must exist.");

            advanceMethod.Invoke(
                publisher,
                new object[]
                {
                    deltaTime
                });
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
