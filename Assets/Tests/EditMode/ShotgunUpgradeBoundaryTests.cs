using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace TopDownRoguelike.Tests.EditMode
{
    public sealed class ShotgunUpgradeBoundaryTests
    {
        private const string ShotgunSkillTypeName =
            "TopDownRoguelike.Gameplay.Characters." +
            "ShotgunSkill";

        private const string ShotgunDataTypeName =
            "TopDownRoguelike.Gameplay.Skills." +
            "ShotgunData";

        [Test]
        public void
            AddProjectileCount_SaturatesWithoutIntegerOverflow()
        {
            Type skillType =
                FindType(ShotgunSkillTypeName);

            Type dataType =
                FindType(ShotgunDataTypeName);

            Assert.That(
                skillType,
                Is.Not.Null,
                "ShotgunSkill was not found.");

            Assert.That(
                dataType,
                Is.Not.Null,
                "ShotgunData was not found.");

            GameObject player =
                new GameObject(
                    "Shotgun Upgrade Boundary Test");

            player.SetActive(false);

            ScriptableObject shotgunData =
                ScriptableObject.CreateInstance(
                    dataType);

            try
            {
                SetPrivateField(
                    shotgunData,
                    "maxProjectileCount",
                    11);

                Component shotgunSkill =
                    player.AddComponent(
                        skillType);

                SetPrivateField(
                    shotgunSkill,
                    "shotgunData",
                    shotgunData);

                SetPrivateField(
                    shotgunSkill,
                    "projectileCount",
                    5);

                MethodInfo addProjectileCount =
                    skillType.GetMethod(
                        "AddProjectileCount",
                        BindingFlags.Instance |
                        BindingFlags.Public);

                Assert.That(
                    addProjectileCount,
                    Is.Not.Null);

                addProjectileCount.Invoke(
                    shotgunSkill,
                    new object[]
                    {
                        int.MaxValue
                    });

                FieldInfo projectileCountField =
                    skillType.GetField(
                        "projectileCount",
                        BindingFlags.Instance |
                        BindingFlags.NonPublic);

                Assert.That(
                    projectileCountField,
                    Is.Not.Null);

                Assert.That(
                    projectileCountField.GetValue(
                        shotgunSkill),
                    Is.EqualTo(11),
                    "A very large upgrade must saturate " +
                    "at MaxProjectileCount without overflow.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(
                    player);

                UnityEngine.Object.DestroyImmediate(
                    shotgunData);
            }
        }

        [Test]
        public void
    AddProjectileDamage_SaturatesWithoutIntegerOverflow()
        {
            Type skillType =
                FindType(ShotgunSkillTypeName);

            Assert.That(
                skillType,
                Is.Not.Null,
                "ShotgunSkill was not found.");

            GameObject player =
                new GameObject(
                    "Shotgun Damage Boundary Test");

            player.SetActive(false);

            try
            {
                Component shotgunSkill =
                    player.AddComponent(
                        skillType);

                SetPrivateField(
                    shotgunSkill,
                    "projectileDamage",
                    5);

                MethodInfo addProjectileDamage =
                    skillType.GetMethod(
                        "AddProjectileDamage",
                        BindingFlags.Instance |
                        BindingFlags.Public);

                Assert.That(
                    addProjectileDamage,
                    Is.Not.Null);

                addProjectileDamage.Invoke(
                    shotgunSkill,
                    new object[]
                    {
                        int.MaxValue
                    });

                FieldInfo damageField =
                    skillType.GetField(
                        "projectileDamage",
                        BindingFlags.Instance |
                        BindingFlags.NonPublic);

                Assert.That(
                    damageField,
                    Is.Not.Null);

                Assert.That(
                    damageField.GetValue(
                        shotgunSkill),
                    Is.EqualTo(int.MaxValue),
                    "A very large damage upgrade must " +
                    "saturate instead of overflowing.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(
                    player);
            }
        }

        [Test]
        public void
    AddPenetration_SaturatesWithoutIntegerOverflow()
        {
            Type skillType =
                FindType(ShotgunSkillTypeName);

            Type dataType =
                FindType(ShotgunDataTypeName);

            Assert.That(
                skillType,
                Is.Not.Null,
                "ShotgunSkill was not found.");

            Assert.That(
                dataType,
                Is.Not.Null,
                "ShotgunData was not found.");

            GameObject player =
                new GameObject(
                    "Shotgun Penetration Boundary Test");

            player.SetActive(false);

            ScriptableObject shotgunData =
                ScriptableObject.CreateInstance(
                    dataType);

            try
            {
                SetPrivateField(
                    shotgunData,
                    "maxPenetrationCount",
                    3);

                Component shotgunSkill =
                    player.AddComponent(
                        skillType);

                SetPrivateField(
                    shotgunSkill,
                    "shotgunData",
                    shotgunData);

                SetPrivateField(
                    shotgunSkill,
                    "penetrationCount",
                    1);

                MethodInfo addPenetration =
                    skillType.GetMethod(
                        "AddPenetration",
                        BindingFlags.Instance |
                        BindingFlags.Public);

                Assert.That(
                    addPenetration,
                    Is.Not.Null);

                addPenetration.Invoke(
                    shotgunSkill,
                    new object[]
                    {
                        int.MaxValue
                    });

                FieldInfo penetrationField =
                    skillType.GetField(
                        "penetrationCount",
                        BindingFlags.Instance |
                        BindingFlags.NonPublic);

                Assert.That(
                    penetrationField,
                    Is.Not.Null);

                Assert.That(
                    penetrationField.GetValue(
                        shotgunSkill),
                    Is.EqualTo(3),
                    "A very large penetration upgrade must " +
                    "saturate at MaxPenetrationCount.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(
                    player);

                UnityEngine.Object.DestroyImmediate(
                    shotgunData);
            }
        }

        [Test]
        public void
    ReduceCooldown_IgnoresNaNAndPreservesFiniteState()
        {
            Type skillType =
                FindType(ShotgunSkillTypeName);

            Type dataType =
                FindType(ShotgunDataTypeName);

            Assert.That(
                skillType,
                Is.Not.Null,
                "ShotgunSkill was not found.");

            Assert.That(
                dataType,
                Is.Not.Null,
                "ShotgunData was not found.");

            GameObject player =
                new GameObject(
                    "Shotgun Cooldown Boundary Test");

            player.SetActive(false);

            ScriptableObject shotgunData =
                ScriptableObject.CreateInstance(
                    dataType);

            try
            {
                SetPrivateField(
                    shotgunData,
                    "minCooldown",
                    1.5f);

                Component shotgunSkill =
                    player.AddComponent(
                        skillType);

                SetPrivateField(
                    shotgunSkill,
                    "shotgunData",
                    shotgunData);

                SetPrivateField(
                    shotgunSkill,
                    "cooldown",
                    4f);

                SetPrivateField(
                    shotgunSkill,
                    "cooldownRemaining",
                    2f);

                MethodInfo reduceCooldown =
                    skillType.GetMethod(
                        "ReduceCooldown",
                        BindingFlags.Instance |
                        BindingFlags.Public);

                Assert.That(
                    reduceCooldown,
                    Is.Not.Null);

                reduceCooldown.Invoke(
                    shotgunSkill,
                    new object[]
                    {
                float.NaN
                    });

                FieldInfo cooldownField =
                    skillType.GetField(
                        "cooldown",
                        BindingFlags.Instance |
                        BindingFlags.NonPublic);

                FieldInfo remainingField =
                    skillType.GetField(
                        "cooldownRemaining",
                        BindingFlags.Instance |
                        BindingFlags.NonPublic);

                Assert.That(
                    cooldownField,
                    Is.Not.Null);

                Assert.That(
                    remainingField,
                    Is.Not.Null);

                Assert.That(
                    cooldownField.GetValue(
                        shotgunSkill),
                    Is.EqualTo(4f),
                    "NaN must not corrupt the effective cooldown.");

                Assert.That(
                    remainingField.GetValue(
                        shotgunSkill),
                    Is.EqualTo(2f),
                    "NaN must not corrupt the remaining cooldown.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(
                    player);

                UnityEngine.Object.DestroyImmediate(
                    shotgunData);
            }
        }

        [Test]
        public void
    ReduceCooldown_ClampsAtMinimumAndAdjustsRemainingCooldown()
        {
            Type skillType =
                FindType(ShotgunSkillTypeName);

            Type dataType =
                FindType(ShotgunDataTypeName);

            Assert.That(
                skillType,
                Is.Not.Null,
                "ShotgunSkill was not found.");

            Assert.That(
                dataType,
                Is.Not.Null,
                "ShotgunData was not found.");

            GameObject player =
                new GameObject(
                    "Shotgun Minimum Cooldown Test");

            player.SetActive(false);

            ScriptableObject shotgunData =
                ScriptableObject.CreateInstance(
                    dataType);

            try
            {
                SetPrivateField(
                    shotgunData,
                    "minCooldown",
                    1.5f);

                Component shotgunSkill =
                    player.AddComponent(
                        skillType);

                SetPrivateField(
                    shotgunSkill,
                    "shotgunData",
                    shotgunData);

                SetPrivateField(
                    shotgunSkill,
                    "cooldown",
                    4f);

                SetPrivateField(
                    shotgunSkill,
                    "cooldownRemaining",
                    3f);

                MethodInfo reduceCooldown =
                    skillType.GetMethod(
                        "ReduceCooldown",
                        BindingFlags.Instance |
                        BindingFlags.Public);

                Assert.That(
                    reduceCooldown,
                    Is.Not.Null);

                reduceCooldown.Invoke(
                    shotgunSkill,
                    new object[]
                    {
                        float.MaxValue
                    });

                FieldInfo cooldownField =
                    skillType.GetField(
                        "cooldown",
                        BindingFlags.Instance |
                        BindingFlags.NonPublic);

                FieldInfo remainingField =
                    skillType.GetField(
                        "cooldownRemaining",
                        BindingFlags.Instance |
                        BindingFlags.NonPublic);

                Assert.That(
                    cooldownField,
                    Is.Not.Null);

                Assert.That(
                    remainingField,
                    Is.Not.Null);

                Assert.That(
                    (float)cooldownField.GetValue(
                        shotgunSkill),
                    Is.EqualTo(1.5f).Within(0.0001f),
                    "Cooldown must not fall below MinCooldown.");

                Assert.That(
                    (float)remainingField.GetValue(
                        shotgunSkill),
                    Is.EqualTo(1.5f).Within(0.0001f),
                    "Remaining cooldown must be shortened " +
                    "to the new effective cooldown.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(
                    player);

                UnityEngine.Object.DestroyImmediate(
                    shotgunData);
            }
        }

        [Test]
        public void
    DamageAndPenetration_StayInGameplayProjectileAndOutOfNetworkEvent()
        {
            Type skillType =
                FindType(ShotgunSkillTypeName);

            Type dataType =
                FindType(ShotgunDataTypeName);

            Type poolType =
                FindType(
                    "TopDownRoguelike.Gameplay.Weapons." +
                    "ProjectilePool");

            Type projectileType =
                FindType(
                    "TopDownRoguelike.Gameplay.Weapons." +
                    "Projectile");

            Type eventType =
                FindType(
                    "TopDownRoguelike.Networking.Protocol." +
                    "PlayerShotgunEvent");

            Assert.That(
                skillType,
                Is.Not.Null);

            Assert.That(
                dataType,
                Is.Not.Null);

            Assert.That(
                poolType,
                Is.Not.Null);

            Assert.That(
                projectileType,
                Is.Not.Null);

            Assert.That(
                eventType,
                Is.Not.Null);

            GameObject player =
                new GameObject(
                    "Host Shotgun Gameplay Upgrade Test");

            GameObject poolObject =
                new GameObject(
                    "Host Shotgun Projectile Pool Test");

            GameObject prefabObject =
                new GameObject(
                    "Host Shotgun Projectile Prefab Test");

            player.SetActive(false);
            poolObject.SetActive(false);
            prefabObject.SetActive(false);

            ScriptableObject shotgunData =
                ScriptableObject.CreateInstance(
                    dataType);

            try
            {
                SetPrivateField(
                    shotgunData,
                    "maxPenetrationCount",
                    3);

                Component projectilePrefab =
                    prefabObject.AddComponent(
                        projectileType);

                Component projectilePool =
                    poolObject.AddComponent(
                        poolType);

                SetPrivateField(
                    projectilePool,
                    "projectilePrefab",
                    projectilePrefab);

                SetPrivateField(
                    projectilePool,
                    "initialSize",
                    0);

                Component shotgunSkill =
                    player.AddComponent(
                        skillType);

                SetPrivateField(
                    shotgunSkill,
                    "shotgunData",
                    shotgunData);

                SetPrivateField(
                    shotgunSkill,
                    "projectilePool",
                    projectilePool);

                SetPrivateField(
                    shotgunSkill,
                    "firePoint",
                    player.transform);

                SetPrivateField(
                    shotgunSkill,
                    "projectileDamage",
                    2);

                SetPrivateField(
                    shotgunSkill,
                    "penetrationCount",
                    0);

                MethodInfo addDamage =
                    skillType.GetMethod(
                        "AddProjectileDamage",
                        BindingFlags.Instance |
                        BindingFlags.Public);

                MethodInfo addPenetration =
                    skillType.GetMethod(
                        "AddPenetration",
                        BindingFlags.Instance |
                        BindingFlags.Public);

                MethodInfo fireProjectile =
                    skillType.GetMethod(
                        "FireProjectile",
                        BindingFlags.Instance |
                        BindingFlags.NonPublic);

                Assert.That(
                    addDamage,
                    Is.Not.Null);

                Assert.That(
                    addPenetration,
                    Is.Not.Null);

                Assert.That(
                    fireProjectile,
                    Is.Not.Null);

                addDamage.Invoke(
                    shotgunSkill,
                    new object[]
                    {
                        3
                    });

                addPenetration.Invoke(
                    shotgunSkill,
                    new object[]
                    {
                        2
                    });

                fireProjectile.Invoke(
                    shotgunSkill,
                    new object[]
                    {
                        Vector2.right
                    });

                Assert.That(
                    poolObject.transform.childCount,
                    Is.EqualTo(1),
                    "Host Gameplay must create one real projectile.");

                Transform projectileTransform =
                    poolObject.transform.GetChild(0);

                Component projectile =
                    projectileTransform.GetComponent(
                        projectileType);

                Assert.That(
                    projectile,
                    Is.Not.Null);

                FieldInfo damageField =
                    projectileType.GetField(
                        "damage",
                        BindingFlags.Instance |
                        BindingFlags.NonPublic);

                FieldInfo penetrationField =
                    projectileType.GetField(
                        "remainingPenetrations",
                        BindingFlags.Instance |
                        BindingFlags.NonPublic);

                Assert.That(
                    damageField,
                    Is.Not.Null);

                Assert.That(
                    penetrationField,
                    Is.Not.Null);

                Assert.That(
                    damageField.GetValue(projectile),
                    Is.EqualTo(5),
                    "The upgraded damage must be applied " +
                    "to the Host Gameplay projectile.");

                Assert.That(
                    penetrationField.GetValue(projectile),
                    Is.EqualTo(2),
                    "The upgraded penetration must be applied " +
                    "to the Host Gameplay projectile.");

                Assert.That(
                    eventType.GetProperty(
                        "ProjectileDamage"),
                    Is.Null,
                    "PlayerShotgunEvent must not expose damage.");

                Assert.That(
                    eventType.GetProperty(
                        "PenetrationCount"),
                    Is.Null,
                    "PlayerShotgunEvent must not expose penetration.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(
                    player);

                UnityEngine.Object.DestroyImmediate(
                    poolObject);

                UnityEngine.Object.DestroyImmediate(
                    prefabObject);

                UnityEngine.Object.DestroyImmediate(
                    shotgunData);
            }
        }

        private static Type FindType(
            string typeName)
        {
            foreach (Assembly assembly
                in AppDomain.CurrentDomain
                    .GetAssemblies())
            {
                Type type =
                    assembly.GetType(
                        typeName,
                        false);

                if (type != null)
                {
                    return type;
                }
            }

            return null;
        }

        private static void SetPrivateField(
            object target,
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
                $"Private field {fieldName} " +
                "was not found.");

            field.SetValue(
                target,
                value);
        }
    }
}