using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace TopDownRoguelike.Tests.EditMode
{
    public sealed class BossEncounterWorldEntityTests
    {
        private readonly List<GameObject> createdObjects =
            new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject createdObject
                in createdObjects)
            {
                if (createdObject != null)
                {
                    UnityEngine.Object.DestroyImmediate(
                        createdObject);
                }
            }

            createdObjects.Clear();
        }

        [Test]
        public void CurrentBoss_ReturnsCurrentBossObjectReadOnly()
        {
            Type encounterType =
                FindType(
                    "TopDownRoguelike.Gameplay.Bosses." +
                    "BossEncounterController");

            Assert.That(
                encounterType,
                Is.Not.Null,
                "BossEncounterController must exist.");

            GameObject encounterObject =
                new GameObject("Boss Encounter Test");

            GameObject bossObject =
                new GameObject("Current Boss");

            createdObjects.Add(encounterObject);
            createdObjects.Add(bossObject);

            encounterObject.SetActive(false);

            Component encounter =
                encounterObject.AddComponent(
                    encounterType);

            FieldInfo currentBossField =
                encounterType.GetField(
                    "currentBoss",
                    BindingFlags.Instance |
                    BindingFlags.NonPublic);

            Assert.That(
                currentBossField,
                Is.Not.Null,
                "currentBoss field must exist.");

            currentBossField.SetValue(
                encounter,
                bossObject);

            PropertyInfo currentBossProperty =
                encounterType.GetProperty(
                    "CurrentBoss",
                    BindingFlags.Instance |
                    BindingFlags.Public);

            Assert.That(
                currentBossProperty,
                Is.Not.Null,
                "CurrentBoss property must exist.");

            Assert.That(
                currentBossProperty.CanWrite,
                Is.False,
                "CurrentBoss must be read-only.");

            object currentBoss =
                currentBossProperty.GetValue(
                    encounter,
                    null);

            Assert.That(
                currentBoss,
                Is.SameAs(bossObject));
        }

        private static Type FindType(
            string fullTypeName)
        {
            foreach (Assembly assembly
                in AppDomain.CurrentDomain.GetAssemblies())
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