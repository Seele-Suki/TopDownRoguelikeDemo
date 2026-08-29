using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace TopDownRoguelike.Tests.EditMode
{
    public sealed class DashSkillInputTests
    {
        private const string DashSkillTypeName =
            "DashSkill";

        private const string InputSourceTypeName =
            "TopDownRoguelike.Gameplay.Characters." +
            "IPlayerInputSource";

        private const string RemoteInputSourceTypeName =
            "TopDownRoguelike.Gameplay.Networking." +
            "RemotePlayerInputSource";

        [Test]
        public void DashSkillUsesPlayerInputSourceContract()
        {
            Type dashSkillType =
                FindType(DashSkillTypeName);

            Type inputSourceType =
                FindType(InputSourceTypeName);

            Assert.That(
                dashSkillType,
                Is.Not.Null,
                "DashSkill must exist.");

            Assert.That(
                inputSourceType,
                Is.Not.Null,
                "IPlayerInputSource must exist.");

            FieldInfo inputSourceField =
                dashSkillType.GetField(
                    "inputSource",
                    BindingFlags.Instance |
                    BindingFlags.NonPublic);

            Assert.That(
                inputSourceField,
                Is.Not.Null,
                "DashSkill must retain an inputSource field.");

            Assert.That(
                inputSourceField.FieldType,
                Is.EqualTo(inputSourceType));

            MethodInfo setInputSourceMethod =
                FindSetInputSourceMethod(
                    dashSkillType,
                    inputSourceType);

            Assert.That(
                setInputSourceMethod,
                Is.Not.Null,
                "DashSkill must expose " +
                "SetInputSource(IPlayerInputSource).");

            FieldInfo dashKeyField =
                dashSkillType.GetField(
                    "dashKey",
                    BindingFlags.Instance |
                    BindingFlags.NonPublic);

            Assert.That(
                dashKeyField,
                Is.Null,
                "DashSkill must not read a dash key directly.");
        }

        [Test]
        public void TryConsumeDashRequest_ConsumesNewSequenceOnlyOnce()
        {
            CreateSubject(
                out GameObject player,
                out Component dashSkill,
                out Component inputSource);

            try
            {
                ApplyInputState(
                    inputSource,
                    Vector2.up,
                    Vector2.right,
                    10u);

                SetInputSource(
                    dashSkill,
                    inputSource);

                Assert.That(
                    InvokePrivate<bool>(
                        dashSkill,
                        "TryConsumeDashRequest"),
                    Is.False,
                    "Binding an input source must not consume " +
                    "an old request.");

                ApplyInputState(
                    inputSource,
                    Vector2.up,
                    Vector2.right,
                    11u);

                Assert.That(
                    InvokePrivate<bool>(
                        dashSkill,
                        "TryConsumeDashRequest"),
                    Is.True,
                    "A newer dash sequence must produce " +
                    "one request.");

                Assert.That(
                    InvokePrivate<bool>(
                        dashSkill,
                        "TryConsumeDashRequest"),
                    Is.False,
                    "The same sequence must not be consumed twice.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(
                    player);
            }
        }

        [Test]
        public void GetDashDirection_PrefersMoveDirection()
        {
            CreateSubject(
                out GameObject player,
                out Component dashSkill,
                out Component inputSource);

            try
            {
                ApplyInputState(
                    inputSource,
                    Vector2.up,
                    Vector2.left,
                    0u);

                SetInputSource(
                    dashSkill,
                    inputSource);

                Vector2 direction =
                    InvokePrivate<Vector2>(
                        dashSkill,
                        "GetDashDirection");

                Assert.That(
                    direction,
                    Is.EqualTo(Vector2.up));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(
                    player);
            }
        }

        [Test]
        public void GetDashDirection_UsesAimWhenMoveIsZero()
        {
            CreateSubject(
                out GameObject player,
                out Component dashSkill,
                out Component inputSource);

            try
            {
                ApplyInputState(
                    inputSource,
                    Vector2.zero,
                    Vector2.left,
                    0u);

                SetInputSource(
                    dashSkill,
                    inputSource);

                Vector2 direction =
                    InvokePrivate<Vector2>(
                        dashSkill,
                        "GetDashDirection");

                Assert.That(
                    direction,
                    Is.EqualTo(Vector2.left));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(
                    player);
            }
        }

        private static void CreateSubject(
            out GameObject player,
            out Component dashSkill,
            out Component inputSource)
        {
            Type dashSkillType =
                FindType(DashSkillTypeName);

            Type remoteInputSourceType =
                FindType(RemoteInputSourceTypeName);

            Assert.That(
                dashSkillType,
                Is.Not.Null,
                "DashSkill must exist.");

            Assert.That(
                remoteInputSourceType,
                Is.Not.Null,
                "RemotePlayerInputSource must exist.");

            player =
                new GameObject(
                    "Dash Skill Input Test");

            player.SetActive(false);

            inputSource =
                player.AddComponent(
                    remoteInputSourceType);

            dashSkill =
                player.AddComponent(
                    dashSkillType);
        }

        private static void ApplyInputState(
            Component inputSource,
            Vector2 moveDirection,
            Vector2 aimDirection,
            uint dashRequestSequence)
        {
            MethodInfo applyMethod =
                inputSource.GetType().GetMethod(
                    "ApplyInputState",
                    new[]
                    {
                        typeof(Vector2),
                        typeof(Vector2),
                        typeof(bool),
                        typeof(uint)
                    });

            Assert.That(
                applyMethod,
                Is.Not.Null,
                "RemotePlayerInputSource.ApplyInputState " +
                "must exist.");

            applyMethod.Invoke(
                inputSource,
                new object[]
                {
                    moveDirection,
                    aimDirection,
                    false,
                    dashRequestSequence
                });
        }

        private static void SetInputSource(
            Component dashSkill,
            Component inputSource)
        {
            Type inputSourceType =
                FindType(InputSourceTypeName);

            MethodInfo setInputSourceMethod =
                FindSetInputSourceMethod(
                    dashSkill.GetType(),
                    inputSourceType);

            Assert.That(
                setInputSourceMethod,
                Is.Not.Null,
                "DashSkill must expose " +
                "SetInputSource(IPlayerInputSource).");

            setInputSourceMethod.Invoke(
                dashSkill,
                new object[]
                {
                    inputSource
                });
        }

        private static MethodInfo FindSetInputSourceMethod(
            Type dashSkillType,
            Type inputSourceType)
        {
            if (dashSkillType == null ||
                inputSourceType == null)
            {
                return null;
            }

            return dashSkillType.GetMethod(
                "SetInputSource",
                BindingFlags.Instance |
                BindingFlags.Public,
                null,
                new[]
                {
                    inputSourceType
                },
                null);
        }

        private static T InvokePrivate<T>(
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

            return (T)method.Invoke(
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