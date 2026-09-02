using System;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TopDownRoguelike.Tests.EditMode
{
    public sealed class HealthBarViewTests
    {
        private const string ViewTypeName =
            "TopDownRoguelike.Gameplay.UI.HealthBarView";

        private const string HealthTypeName =
            "TopDownRoguelike.Gameplay.Characters.PlayerHealth";

        [Test]
        public void Bind_RefreshesUntilViewIsUnbound()
        {
            Type viewType = FindType(ViewTypeName);
            Type healthType = FindType(HealthTypeName);

            Assert.That(
                viewType,
                Is.Not.Null,
                "HealthBarView must exist.");

            Assert.That(healthType, Is.Not.Null);

            var viewObject =
                new GameObject("Health Bar View Test");

            var sliderObject =
                new GameObject(
                    "Health Slider",
                    typeof(RectTransform));

            var textObject =
                new GameObject(
                    "Health Text",
                    typeof(RectTransform));

            var playerObject =
                new GameObject("Player Health Test");

            viewObject.SetActive(false);
            playerObject.SetActive(false);

            sliderObject.transform.SetParent(
                viewObject.transform);

            textObject.transform.SetParent(
                viewObject.transform);

            try
            {
                Slider slider =
                    sliderObject.AddComponent<Slider>();

                TMP_Text healthText =
                    textObject.AddComponent<
                        TextMeshProUGUI>();

                Component view =
                    viewObject.AddComponent(viewType);

                SetPrivateField(
                    view,
                    "healthSlider",
                    slider);

                SetPrivateField(
                    view,
                    "healthText",
                    healthText);

                InvokePrivate(view, "Awake");

                Component playerHealth =
                    playerObject.AddComponent(
                        healthType);

                InvokePrivate(
                    playerHealth,
                    "Awake");

                MethodInfo bindMethod =
                    viewType.GetMethod("Bind");

                Assert.That(bindMethod, Is.Not.Null);

                bindMethod.Invoke(
                    view,
                    new object[] { playerHealth });

                Assert.That(slider.minValue, Is.EqualTo(0f));
                Assert.That(slider.maxValue, Is.EqualTo(1f));
                Assert.That(slider.value, Is.EqualTo(1f));
                Assert.That(slider.interactable, Is.False);
                Assert.That(healthText.text, Is.EqualTo("10 / 10"));

                InvokeTakeDamage(
                    playerHealth,
                    3);

                Assert.That(
                    slider.value,
                    Is.EqualTo(0.7f).Within(0.001f));

                Assert.That(
                    healthText.text,
                    Is.EqualTo("7 / 10"));

                MethodInfo unbindMethod =
                    viewType.GetMethod("Unbind");

                Assert.That(unbindMethod, Is.Not.Null);

                unbindMethod.Invoke(view, null);

                InvokeTakeDamage(
                    playerHealth,
                    1);

                Assert.That(
                    slider.value,
                    Is.EqualTo(0.7f).Within(0.001f));

                Assert.That(
                    healthText.text,
                    Is.EqualTo("7 / 10"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(
                    viewObject);

                UnityEngine.Object.DestroyImmediate(
                    playerObject);
            }
        }

        [Test]
        public void BossHealthView_ExposesHideEntryPoint()
        {
            Type viewType = FindType(
                "TopDownRoguelike.Gameplay.UI.BossHealthView");

            Assert.That(viewType, Is.Not.Null);
            Assert.That(
                viewType.GetMethod("Hide"),
                Is.Not.Null,
                "BossHealthView must expose Hide for network removal.");
        }

        private static void InvokeTakeDamage(
            Component playerHealth,
            int damage)
        {
            MethodInfo method =
                playerHealth.GetType().GetMethod(
                    "TakeDamage");

            Assert.That(method, Is.Not.Null);

            Type damageInfoType =
                method.GetParameters()[0].ParameterType;

            object damageInfo =
                Activator.CreateInstance(
                    damageInfoType);

            FieldInfo damageField =
                damageInfoType.GetField("Damage");

            Assert.That(damageField, Is.Not.Null);

            damageField.SetValue(
                damageInfo,
                damage);

            method.Invoke(
                playerHealth,
                new[] { damageInfo });
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

            field.SetValue(target, value);
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

            method.Invoke(target, null);
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
