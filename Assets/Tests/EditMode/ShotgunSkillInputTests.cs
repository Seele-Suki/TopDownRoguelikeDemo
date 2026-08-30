using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace TopDownRoguelike.Tests.EditMode
{
    public sealed class ShotgunSkillInputTests
    {
        private const string ShotgunSkillTypeName =
            "TopDownRoguelike.Gameplay.Characters." +
            "ShotgunSkill";

        private const string InputSourceTypeName =
            "TopDownRoguelike.Gameplay.Characters." +
            "IPlayerInputSource";

        private const string RemoteInputSourceTypeName =
            "TopDownRoguelike.Gameplay.Networking." +
            "RemotePlayerInputSource";

        [Test]
        public void ShotgunSkillUsesPlayerInputSourceContract()
        {
            Type skillType =
                FindType(ShotgunSkillTypeName);

            Type inputSourceType =
                FindType(InputSourceTypeName);

            Assert.That(
                skillType,
                Is.Not.Null,
                "ShotgunSkill must exist.");

            Assert.That(
                inputSourceType,
                Is.Not.Null,
                "IPlayerInputSource must exist.");

            FieldInfo inputSourceField =
                skillType.GetField(
                    "inputSource",
                    BindingFlags.Instance |
                    BindingFlags.NonPublic);

            Assert.That(
                inputSourceField,
                Is.Not.Null,
                "ShotgunSkill must retain an " +
                "inputSource field.");

            Assert.That(
                inputSourceField.FieldType,
                Is.EqualTo(inputSourceType));

            MethodInfo setInputSourceMethod =
                FindSetInputSourceMethod(
                    skillType,
                    inputSourceType);

            Assert.That(
                setInputSourceMethod,
                Is.Not.Null,
                "ShotgunSkill must expose " +
                "SetInputSource(IPlayerInputSource).");

            FieldInfo mainCameraField =
                skillType.GetField(
                    "mainCamera",
                    BindingFlags.Instance |
                    BindingFlags.NonPublic);

            Assert.That(
                mainCameraField,
                Is.Null,
                "ShotgunSkill must not read Camera.main " +
                "or mouse coordinates directly.");
        }

        [Test]
        public void TryConsumeShotgunRequest_ConsumesNewSequenceOnlyOnce()
        {
            CreateSubject(
                out GameObject player,
                out Component shotgunSkill,
                out Component inputSource);

            try
            {
                ApplyInputState(
                    inputSource,
                    Vector2.up,
                    Vector2.right,
                    5u,
                    10u);

                SetInputSource(
                    shotgunSkill,
                    inputSource);

                Assert.That(
                    InvokePrivate<bool>(
                        shotgunSkill,
                        "TryConsumeShotgunRequest"),
                    Is.False,
                    "Binding an input source must not " +
                    "consume an existing request.");

                ApplyInputState(
                    inputSource,
                    Vector2.up,
                    Vector2.right,
                    5u,
                    11u);

                Assert.That(
                    InvokePrivate<bool>(
                        shotgunSkill,
                        "TryConsumeShotgunRequest"),
                    Is.True,
                    "A newer shotgun sequence must " +
                    "produce one request.");

                Assert.That(
                    InvokePrivate<bool>(
                        shotgunSkill,
                        "TryConsumeShotgunRequest"),
                    Is.False,
                    "The same shotgun sequence must not " +
                    "be consumed twice.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(
                    player);
            }
        }

        [Test]
        public void GetShotgunDirection_UsesAimDirection()
        {
            CreateSubject(
                out GameObject player,
                out Component shotgunSkill,
                out Component inputSource);

            try
            {
                ApplyInputState(
                    inputSource,
                    Vector2.up,
                    Vector2.left,
                    0u,
                    0u);

                SetInputSource(
                    shotgunSkill,
                    inputSource);

                Vector2 direction =
                    InvokePrivate<Vector2>(
                        shotgunSkill,
                        "GetShotgunDirection");

                Assert.That(
                    direction,
                    Is.EqualTo(Vector2.left),
                    "Shotgun direction must come from " +
                    "IPlayerInputSource.AimDirection.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(
                    player);
            }
        }

        private static void CreateSubject(
            out GameObject player,
            out Component shotgunSkill,
            out Component inputSource)
        {
            Type shotgunSkillType =
                FindType(ShotgunSkillTypeName);

            Type remoteInputSourceType =
                FindType(RemoteInputSourceTypeName);

            Assert.That(
                shotgunSkillType,
                Is.Not.Null,
                "ShotgunSkill must exist.");

            Assert.That(
                remoteInputSourceType,
                Is.Not.Null,
                "RemotePlayerInputSource must exist.");

            player =
                new GameObject(
                    "Shotgun Skill Input Test");

            player.SetActive(false);

            inputSource =
                player.AddComponent(
                    remoteInputSourceType);

            shotgunSkill =
                player.AddComponent(
                    shotgunSkillType);
        }

        private static void ApplyInputState(
            Component inputSource,
            Vector2 moveDirection,
            Vector2 aimDirection,
            uint dashRequestSequence,
            uint shotgunRequestSequence)
        {
            MethodInfo method =
                inputSource.GetType().GetMethod(
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
                method,
                Is.Not.Null,
                "The five-argument ApplyInputState " +
                "overload must exist.");

            method.Invoke(
                inputSource,
                new object[]
                {
                    moveDirection,
                    aimDirection,
                    false,
                    dashRequestSequence,
                    shotgunRequestSequence
                });
        }

        private static void SetInputSource(
            Component shotgunSkill,
            Component inputSource)
        {
            Type inputSourceType =
                FindType(InputSourceTypeName);

            MethodInfo method =
                FindSetInputSourceMethod(
                    shotgunSkill.GetType(),
                    inputSourceType);

            Assert.That(
                method,
                Is.Not.Null,
                "ShotgunSkill must expose " +
                "SetInputSource(IPlayerInputSource).");

            method.Invoke(
                shotgunSkill,
                new object[]
                {
                    inputSource
                });
        }

        private static MethodInfo FindSetInputSourceMethod(
            Type shotgunSkillType,
            Type inputSourceType)
        {
            if (shotgunSkillType == null ||
                inputSourceType == null)
            {
                return null;
            }

            return shotgunSkillType.GetMethod(
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