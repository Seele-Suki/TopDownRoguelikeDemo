using System;
using System.Reflection;
using NUnit.Framework;
using TopDownRoguelike.Networking.Protocol;
using UnityEditor;
using UnityEngine;

namespace TopDownRoguelike.Tests.EditMode
{
    public sealed class EnemyNetworkArchetypeTests
    {
        private const string EnemyDataTypeName =
            "TopDownRoguelike.Gameplay.Enemies.EnemyData";

        [Test]
        public void NetworkEnemyArchetype_UsesStableProtocolValues()
        {
            Assert.That(
                (byte)NetworkEnemyArchetype.Invalid,
                Is.EqualTo(0));

            Assert.That(
                (byte)NetworkEnemyArchetype.Basic,
                Is.EqualTo(1));

            Assert.That(
                (byte)NetworkEnemyArchetype.Fast,
                Is.EqualTo(2));
        }

        [Test]
        public void EnemyData_ExposesReadOnlyNetworkArchetype()
        {
            Type enemyDataType =
                FindType(EnemyDataTypeName);

            Assert.That(
                enemyDataType,
                Is.Not.Null);

            PropertyInfo archetypeProperty =
                enemyDataType.GetProperty(
                    "NetworkArchetype",
                    BindingFlags.Instance |
                    BindingFlags.Public);

            Assert.That(
                archetypeProperty,
                Is.Not.Null,
                "EnemyData.NetworkArchetype must exist.");

            Assert.That(
                archetypeProperty.PropertyType,
                Is.EqualTo(typeof(NetworkEnemyArchetype)));

            Assert.That(
                archetypeProperty.CanRead,
                Is.True);

            Assert.That(
                archetypeProperty.SetMethod,
                Is.Null,
                "NetworkArchetype must be read-only at runtime.");

            ScriptableObject enemyData =
                ScriptableObject.CreateInstance(
                    enemyDataType);

            try
            {
                Assert.That(
                    archetypeProperty.GetValue(
                        enemyData),
                    Is.EqualTo(
                        NetworkEnemyArchetype.Invalid));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(
                    enemyData);
            }
        }

        [TestCase(
            "Assets/ScriptableObjects/Enemies/BasicEnemyData.asset",
            NetworkEnemyArchetype.Basic)]
        [TestCase(
            "Assets/ScriptableObjects/Enemies/FastEnemyData.asset",
            NetworkEnemyArchetype.Fast)]
        public void EnemyDataAsset_UsesExpectedNetworkArchetype(
            string assetPath,
            NetworkEnemyArchetype expectedArchetype)
        {
            UnityEngine.Object enemyData =
                AssetDatabase.LoadAssetAtPath<
                    UnityEngine.Object>(assetPath);

            Assert.That(
                enemyData,
                Is.Not.Null,
                $"EnemyData asset is missing: {assetPath}");

            PropertyInfo archetypeProperty =
                enemyData.GetType().GetProperty(
                    "NetworkArchetype",
                    BindingFlags.Instance |
                    BindingFlags.Public);

            Assert.That(
                archetypeProperty,
                Is.Not.Null);

            Assert.That(
                archetypeProperty.GetValue(enemyData),
                Is.EqualTo(expectedArchetype));
        }

        [Test]
        public void EnemySpawnEntry_ReportsPrefabArchetype()
        {
            GameObject prefab = new GameObject("Fast Enemy Prefab");
            Type enemyDataType = FindType(EnemyDataTypeName);
            Type enemyHealthType = FindType(
                "TopDownRoguelike.Gameplay.Enemies.EnemyHealth");
            Type spawnEntryType = FindType(
                "TopDownRoguelike.Gameplay.Enemies.EnemySpawnEntry");

            Assert.That(enemyDataType, Is.Not.Null);
            Assert.That(enemyHealthType, Is.Not.Null);
            Assert.That(spawnEntryType, Is.Not.Null);

            ScriptableObject data = ScriptableObject.CreateInstance(
                enemyDataType);
            try
            {
                FieldInfo archetypeField = enemyDataType.GetField(
                    "networkArchetype",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                archetypeField.SetValue(data, NetworkEnemyArchetype.Fast);
                Component health = prefab.AddComponent(enemyHealthType);
                enemyHealthType.GetField(
                    "enemyData",
                    BindingFlags.Instance | BindingFlags.NonPublic)
                    .SetValue(health, data);

                object entry = Activator.CreateInstance(spawnEntryType);
                spawnEntryType.GetField(
                    "enemyPrefab",
                    BindingFlags.Instance | BindingFlags.NonPublic)
                    .SetValue(entry, prefab);

                Assert.That(
                    spawnEntryType.GetProperty("NetworkArchetype")
                        .GetValue(entry),
                    Is.EqualTo(NetworkEnemyArchetype.Fast));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(prefab);
                UnityEngine.Object.DestroyImmediate(data);
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
