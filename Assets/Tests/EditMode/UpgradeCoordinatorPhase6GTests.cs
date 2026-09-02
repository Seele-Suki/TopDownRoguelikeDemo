using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace TopDownRoguelike.Tests.EditMode
{
    public sealed class UpgradeCoordinatorPhase6GTests
    {
        private const string UpgradeManagerTypeName =
            "TopDownRoguelike.Gameplay.Upgrades.UpgradeManager";

        private const string UpgradeDataTypeName =
            "TopDownRoguelike.Gameplay.Upgrades.UpgradeData";

        private const string GameManagerTypeName =
            "TopDownRoguelike.Gameplay.Core.GameManager";

        private const string CoordinatorTypeName =
            "TopDownRoguelike.Gameplay.Networking.NetworkUpgradeCoordinator";

        [Test]
        public void UpgradeManager_ExposesReadOnlyCurrentOptions()
        {
            Type upgradeManagerType = FindType(UpgradeManagerTypeName);
            Type upgradeDataType = FindType(UpgradeDataTypeName);

            Assert.That(upgradeManagerType, Is.Not.Null);
            Assert.That(upgradeDataType, Is.Not.Null);

            Component manager =
                (Component)new GameObject("Upgrade Manager Test")
                    .AddComponent(upgradeManagerType);

            try
            {
                PropertyInfo property =
                    upgradeManagerType.GetProperty(
                        "CurrentOptions",
                        BindingFlags.Instance |
                        BindingFlags.Public);

                Assert.That(property, Is.Not.Null);
                Assert.That(property.CanRead, Is.True);
                Assert.That(property.CanWrite, Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(manager.gameObject);
            }
        }

        [Test]
        public void NetworkUpgradeCoordinator_ConfigureEntersIdleConfiguredState()
        {
            GameObject root = new GameObject("Upgrade Coordinator Test");
            Type upgradeManagerType = FindType(UpgradeManagerTypeName);
            Type gameManagerType = FindType(GameManagerTypeName);
            Type coordinatorType = FindType(CoordinatorTypeName);

            Assert.That(upgradeManagerType, Is.Not.Null);
            Assert.That(gameManagerType, Is.Not.Null);
            Assert.That(coordinatorType, Is.Not.Null);

            Component manager =
                root.AddComponent(upgradeManagerType);
            Component gameManager =
                root.AddComponent(gameManagerType);
            Component coordinator =
                root.AddComponent(coordinatorType);

            try
            {
                coordinatorType.GetMethod(
                        "Configure",
                        BindingFlags.Instance |
                        BindingFlags.Public)
                    .Invoke(
                        coordinator,
                        new object[] { manager, gameManager });

                bool isConfigured = (bool)coordinatorType
                    .GetProperty("IsConfigured")
                    .GetValue(coordinator, null);
                object state = coordinatorType
                    .GetProperty("State")
                    .GetValue(coordinator, null);

                Assert.That(isConfigured, Is.True);
                Assert.That(
                    state.ToString(),
                    Is.EqualTo("Idle"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void NetworkUpgradeCoordinator_RejectsMissingDependencies()
        {
            Type coordinatorType = FindType(CoordinatorTypeName);
            Assert.That(coordinatorType, Is.Not.Null);

            Component coordinator =
                (Component)new GameObject(
                    "Upgrade Coordinator Validation Test")
                    .AddComponent(coordinatorType);

            try
            {
                TargetInvocationException exception =
                    Assert.Throws<TargetInvocationException>(
                        () => coordinatorType.GetMethod(
                                "Configure",
                                BindingFlags.Instance |
                                BindingFlags.Public)
                            .Invoke(
                                coordinator,
                                new object[] { null, null }));

                Assert.That(
                    exception.InnerException,
                    Is.TypeOf<ArgumentNullException>());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(coordinator.gameObject);
            }
        }

        [Test]
        public void NetworkUpgradeCoordinator_ExposesHostUpgradeStartFlow()
        {
            Type coordinatorType = FindType(CoordinatorTypeName);
            Assert.That(coordinatorType, Is.Not.Null);

            Assert.That(
                coordinatorType.GetMethod(
                    "BeginHostUpgrade",
                    BindingFlags.Instance |
                    BindingFlags.Public),
                Is.Not.Null);

            Assert.That(
                coordinatorType.GetEvent(
                    "UpgradeStarted",
                    BindingFlags.Instance |
                    BindingFlags.Public),
                Is.Not.Null);
        }

        [Test]
        public void NetworkUpgradeCoordinator_ExposesChoiceValidationFlow()
        {
            Type coordinatorType = FindType(CoordinatorTypeName);
            Assert.That(coordinatorType, Is.Not.Null);

            Assert.That(
                coordinatorType.GetMethod(
                    "TrySubmitChoice",
                    BindingFlags.Instance |
                    BindingFlags.Public),
                Is.Not.Null);

            Assert.That(
                coordinatorType.GetProperty(
                    "AllChoicesSubmitted",
                    BindingFlags.Instance |
                    BindingFlags.Public),
                Is.Not.Null);

            Assert.That(
                coordinatorType.GetProperty(
                    "SubmittedChoices",
                    BindingFlags.Instance |
                    BindingFlags.Public),
                Is.Not.Null);
        }

        [Test]
        public void NetworkUpgradeCoordinator_ExposesAuthoritativeCompletionFlow()
        {
            Type coordinatorType = FindType(CoordinatorTypeName);
            Assert.That(coordinatorType, Is.Not.Null);

            Assert.That(
                coordinatorType.GetMethod(
                    "CompleteHostUpgrade",
                    BindingFlags.Instance |
                    BindingFlags.Public),
                Is.Not.Null);

            Assert.That(
                coordinatorType.GetEvent(
                    "UpgradeApplied",
                    BindingFlags.Instance |
                    BindingFlags.Public),
                Is.Not.Null);

            Assert.That(
                coordinatorType.GetEvent(
                    "UpgradeCompleted",
                    BindingFlags.Instance |
                    BindingFlags.Public),
                Is.Not.Null);
        }

        [Test]
        public void NetworkUpgradeCoordinator_ExposesRemoteCompletionFlow()
        {
            Type coordinatorType = FindType(CoordinatorTypeName);
            Assert.That(coordinatorType, Is.Not.Null);

            Assert.That(
                coordinatorType.GetMethod(
                    "ApplyRemoteUpgradeCompletion",
                    BindingFlags.Instance |
                    BindingFlags.Public),
                Is.Not.Null);
        }

        [Test]
        public void NetworkUpgradeCoordinator_ExposesReusableIdleResetFlow()
        {
            Type coordinatorType = FindType(CoordinatorTypeName);
            Assert.That(coordinatorType, Is.Not.Null);
            Assert.That(
                coordinatorType.GetMethod(
                    "ResetState",
                    BindingFlags.Instance | BindingFlags.Public),
                Is.Not.Null);
        }

        [Test]
        public void UpgradeManager_ExposesNetworkWaitingState()
        {
            Type upgradeManagerType = FindType(UpgradeManagerTypeName);
            Assert.That(upgradeManagerType, Is.Not.Null);

            Assert.That(
                upgradeManagerType.GetMethod(
                    "SetNetworkWaiting",
                    BindingFlags.Instance |
                    BindingFlags.Public),
                Is.Not.Null);
        }

        private static Type FindType(string fullName)
        {
            foreach (Assembly assembly
                in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type result = assembly.GetType(fullName, false);

                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }
    }
}
