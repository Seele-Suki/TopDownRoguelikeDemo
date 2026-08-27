using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace TopDownRoguelike.Tests.EditMode
{
    public sealed class PlayerControllerInputTests
    {
        private const string PlayerControllerTypeName =
            "PlayerController";

        private const string LocalInputSourceTypeName =
            "TopDownRoguelike.Gameplay.Characters." +
            "LocalPlayerInputSource";

        private Type playerControllerType;
        private Type localInputSourceType;

        [SetUp]
        public void SetUp()
        {
            playerControllerType =
                FindType(PlayerControllerTypeName);

            localInputSourceType =
                FindType(LocalInputSourceTypeName);

            Assert.That(
                playerControllerType,
                Is.Not.Null);

            Assert.That(
                localInputSourceType,
                Is.Not.Null);
        }

        [Test]
        public void MoveUsesCachedDirectionAndMoveSpeed()
        {
            GameObject player =
                CreatePlayer(
                    out Rigidbody2D body,
                    out Component controller,
                    out _);

            try
            {
                SetPrivateField(
                    controller,
                    "moveInput",
                    new Vector2(
                        0.6f,
                        0.8f));

                SetPrivateField(
                    controller,
                    "moveSpeed",
                    5f);

                InvokePrivate(
                    controller,
                    "Move");

                Assert.That(
                    body.velocity.x,
                    Is.EqualTo(3f).Within(0.001f));

                Assert.That(
                    body.velocity.y,
                    Is.EqualTo(4f).Within(0.001f));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(
                    player);
            }
        }

        [Test]
        public void RotateUsesCachedAimDirection()
        {
            GameObject player =
                CreatePlayer(
                    out Rigidbody2D body,
                    out Component controller,
                    out _);

            try
            {
                SetPrivateField(
                    controller,
                    "aimDirection",
                    Vector2.up);

                InvokePrivate(
                    controller,
                    "RotateToAimDirection");

                Assert.That(
                    body.rotation,
                    Is.EqualTo(90f).Within(0.001f));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(
                    player);
            }
        }

        [Test]
        public void ZeroAimDirectionKeepsCurrentRotation()
        {
            GameObject player =
                CreatePlayer(
                    out Rigidbody2D body,
                    out Component controller,
                    out _);

            try
            {
                body.rotation =
                    37f;

                SetPrivateField(
                    controller,
                    "aimDirection",
                    Vector2.zero);

                InvokePrivate(
                    controller,
                    "RotateToAimDirection");

                Assert.That(
                    body.rotation,
                    Is.EqualTo(37f).Within(0.001f));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(
                    player);
            }
        }

        [Test]
        public void ValidInputSourceReEnablesController()
        {
            GameObject player =
                CreatePlayer(
                    out _,
                    out Component controller,
                    out Component localInputSource);

            try
            {
                Behaviour controllerBehaviour =
                    (Behaviour)controller;

                controllerBehaviour.enabled =
                    false;

                MethodInfo setInputSource =
                    playerControllerType.GetMethod(
                        "SetInputSource",
                        BindingFlags.Instance |
                        BindingFlags.Public);

                Assert.That(
                    setInputSource,
                    Is.Not.Null);

                setInputSource.Invoke(
                    controller,
                    new object[]
                    {
                        localInputSource
                    });

                Assert.That(
                    controllerBehaviour.enabled,
                    Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(
                    player);
            }
        }

        private GameObject CreatePlayer(
            out Rigidbody2D body,
            out Component controller,
            out Component localInputSource)
        {
            var player =
                new GameObject(
                    "Player Controller Input Test");

            player.SetActive(false);

            body =
                player.AddComponent<Rigidbody2D>();

            localInputSource =
                player.AddComponent(
                    localInputSourceType);

            controller =
                player.AddComponent(
                    playerControllerType);

            InvokePrivate(
                controller,
                "Awake");

            player.SetActive(true);

            return player;
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

            method.Invoke(
                target,
                null);
        }

        private static Type FindType(
            string fullTypeName)
        {
            foreach (var assembly in
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