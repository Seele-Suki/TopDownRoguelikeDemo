using TMPro;
using UnityEngine.UI;
using System;
using System.Reflection;
using NUnit.Framework;
using TopDownRoguelike.Networking.Gameplay;
using TopDownRoguelike.Infrastructure;
using UnityEngine;

namespace TopDownRoguelike.Tests.EditMode
{
    public sealed class NetworkGameBootstrapTests
    {
        private const string BootstrapTypeName =
            "TopDownRoguelike.Gameplay.Networking." +
            "NetworkGameBootstrap";

        [Test]
        public void Awake_CreatesEmptyPlayerRegistry()
        {
            Type bootstrapType =
                FindType(BootstrapTypeName);

            Assert.That(
                bootstrapType,
                Is.Not.Null,
                $"{BootstrapTypeName} must exist.");

            Assert.That(
                typeof(MonoBehaviour).IsAssignableFrom(
                    bootstrapType),
                Is.True);

            var bootstrapObject =
                new GameObject("Bootstrap Test");

            bootstrapObject.SetActive(false);

            try
            {
                Component bootstrap =
                    bootstrapObject.AddComponent(
                        bootstrapType);

                MethodInfo awakeMethod =
                    bootstrapType.GetMethod(
                        "Awake",
                        BindingFlags.Instance |
                        BindingFlags.NonPublic);

                Assert.That(awakeMethod, Is.Not.Null);

                awakeMethod.Invoke(bootstrap, null);

                PropertyInfo registryProperty =
                    bootstrapType.GetProperty(
                        "Registry",
                        BindingFlags.Instance |
                        BindingFlags.Public);

                Assert.That(registryProperty, Is.Not.Null);

                var registry =
                    registryProperty.GetValue(bootstrap)
                    as NetworkPlayerRegistry;

                Assert.That(registry, Is.Not.Null);
                Assert.That(registry.Count, Is.EqualTo(0));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(
                    bootstrapObject);
            }
        }

        [Test]
        public void StartInSinglePlayerMode_RegistersOnlyScenePlayer()
        {
            Type bootstrapType =
                FindType(BootstrapTypeName);

            Type cameraFollowType =
                FindType("CameraFollow");

            Type healthBarViewType =
                FindType(
                    "TopDownRoguelike.Gameplay.UI." +
                    "HealthBarView");

            Assert.That(bootstrapType, Is.Not.Null);
            Assert.That(cameraFollowType, Is.Not.Null);
            Assert.That(healthBarViewType,Is.Not.Null);

            var bootstrapObject =
                new GameObject("Bootstrap Test");

            var scenePlayer =
                new GameObject("Scene Player");

            var hostSpawnObject =
                new GameObject("Host Spawn Point");

            var cameraObject =
                new GameObject("Camera Test");

            GameObject healthBarObject = null;

            bootstrapObject.SetActive(false);
            scenePlayer.SetActive(false);
            cameraObject.SetActive(false);

            hostSpawnObject.transform.position =
                new Vector3(3f, -2f, 0f);

            try
            {
                Component healthBarView =
                    CreateHealthBarView(
                        healthBarViewType,
                        out healthBarObject);

                AddInitializedPlayerHealth(
                    scenePlayer);

                cameraObject.AddComponent<Camera>();

                Component cameraFollow =
                    cameraObject.AddComponent(
                        cameraFollowType);

                Component bootstrap =
                    bootstrapObject.AddComponent(
                        bootstrapType);

                SetPrivateField(
                    bootstrap,
                    "healthBarView",
                    healthBarView);

                SetPrivateField(
                    bootstrap,
                    "scenePlayer",
                    scenePlayer);

                SetPrivateField(
                    bootstrap,
                    "hostSpawnPoint",
                    hostSpawnObject.transform);

                SetPrivateField(
                    bootstrap,
                    "cameraFollow",
                    cameraFollow);

                GameSession.ConfigureSinglePlayer();

                InvokePrivate(bootstrap, "Awake");
                InvokePrivate(bootstrap, "Start");

                PropertyInfo registryProperty =
                    bootstrapType.GetProperty("Registry");

                var registry =
                    registryProperty.GetValue(bootstrap)
                        as NetworkPlayerRegistry;

                PropertyInfo localPlayerProperty =
                    bootstrapType.GetProperty("LocalPlayer");

                var localPlayer =
                    localPlayerProperty.GetValue(bootstrap)
                        as GameObject;

                Assert.That(registry, Is.Not.Null);
                Assert.That(registry.Count, Is.EqualTo(1));

                Assert.That(
                    registry.TryGetPlayer(
                        1u,
                        out GameObject registeredPlayer),
                    Is.True);

                Assert.That(
                    registeredPlayer,
                    Is.SameAs(scenePlayer));

                Assert.That(
                    localPlayer,
                    Is.SameAs(scenePlayer));

                Assert.That(scenePlayer.activeSelf, Is.True);

                Assert.That(
                    scenePlayer.transform.position,
                    Is.EqualTo(
                        hostSpawnObject.transform.position));
            }
            finally
            {
                GameSession.Reset();

                UnityEngine.Object.DestroyImmediate(
                    bootstrapObject);

                UnityEngine.Object.DestroyImmediate(
                    scenePlayer);

                UnityEngine.Object.DestroyImmediate(
                    hostSpawnObject);

                UnityEngine.Object.DestroyImmediate(
                    cameraObject);

                if (healthBarObject != null)
                {
                    UnityEngine.Object.DestroyImmediate(
                        healthBarObject);
                }
            }
        }

        [Test]
        public void ConfigureHostPlayers_CreatesTwoRegisteredPlayers()
        {
            Type bootstrapType =
                FindType(BootstrapTypeName);

            Assert.That(bootstrapType, Is.Not.Null);

            var bootstrapObject =
                new GameObject("Bootstrap Test");

            var scenePlayer =
                new GameObject("Scene Player");

            var hostSpawnObject =
                new GameObject("Host Spawn Point");

            var clientSpawnObject =
                new GameObject("Client Spawn Point");

            bootstrapObject.SetActive(false);
            scenePlayer.SetActive(false);

            hostSpawnObject.transform.position =
                new Vector3(-1f, 2f, 0f);

            clientSpawnObject.transform.position =
                new Vector3(3f, -2f, 0f);

            GameObject remotePlayer = null;

            try
            {
                Component bootstrap =
                    bootstrapObject.AddComponent(
                        bootstrapType);

                SetPrivateField(
                    bootstrap,
                    "scenePlayer",
                    scenePlayer);

                SetPrivateField(
                    bootstrap,
                    "hostSpawnPoint",
                    hostSpawnObject.transform);

                SetPrivateField(
                    bootstrap,
                    "clientSpawnPoint",
                    clientSpawnObject.transform);

                InvokePrivate(bootstrap, "Awake");

                InvokePrivate(
                    bootstrap,
                    "ConfigureHostPlayers",
                    11u,
                    22u);

                var registry =
                    bootstrapType
                        .GetProperty("Registry")
                        .GetValue(bootstrap)
                        as NetworkPlayerRegistry;

                var localPlayer =
                    bootstrapType
                        .GetProperty("LocalPlayer")
                        .GetValue(bootstrap)
                        as GameObject;

                remotePlayer =
                    bootstrapType
                        .GetProperty("RemotePlayer")
                        .GetValue(bootstrap)
                        as GameObject;

                Assert.That(registry, Is.Not.Null);
                Assert.That(registry.Count, Is.EqualTo(2));

                Assert.That(
                    registry.TryGetPlayer(
                        11u,
                        out GameObject registeredHost),
                    Is.True);

                Assert.That(
                    registry.TryGetPlayer(
                        22u,
                        out GameObject registeredClient),
                    Is.True);

                Assert.That(
                    registeredHost,
                    Is.SameAs(scenePlayer));

                Assert.That(
                    registeredClient,
                    Is.SameAs(remotePlayer));

                Assert.That(
                    localPlayer,
                    Is.SameAs(scenePlayer));

                Assert.That(
                    remotePlayer,
                    Is.Not.Null);

                Assert.That(
                    remotePlayer,
                    Is.Not.SameAs(scenePlayer));

                Assert.That(
                    scenePlayer.transform.position,
                    Is.EqualTo(
                        hostSpawnObject.transform.position));

                Assert.That(
                    remotePlayer.transform.position,
                    Is.EqualTo(
                        clientSpawnObject.transform.position));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(
                    bootstrapObject);

                if (remotePlayer != null)
                {
                    UnityEngine.Object.DestroyImmediate(
                        remotePlayer);
                }

                UnityEngine.Object.DestroyImmediate(
                    scenePlayer);

                UnityEngine.Object.DestroyImmediate(
                    hostSpawnObject);

                UnityEngine.Object.DestroyImmediate(
                    clientSpawnObject);
            }
        }

        [Test]
        public void ConfigureClientPlayers_CreatesTwoRegisteredPlayers()
        {
            Type bootstrapType =
                FindType(BootstrapTypeName);

            Assert.That(bootstrapType, Is.Not.Null);

            var bootstrapObject =
                new GameObject("Bootstrap Test");

            var scenePlayer =
                new GameObject("Scene Player");

            var hostSpawnObject =
                new GameObject("Host Spawn Point");

            var clientSpawnObject =
                new GameObject("Client Spawn Point");

            bootstrapObject.SetActive(false);
            scenePlayer.SetActive(false);

            hostSpawnObject.transform.position =
                new Vector3(-2f, 1f, 0f);

            clientSpawnObject.transform.position =
                new Vector3(4f, -1f, 0f);

            GameObject remotePlayer = null;

            try
            {
                Component bootstrap =
                    bootstrapObject.AddComponent(
                        bootstrapType);

                SetPrivateField(
                    bootstrap,
                    "scenePlayer",
                    scenePlayer);

                SetPrivateField(
                    bootstrap,
                    "hostSpawnPoint",
                    hostSpawnObject.transform);

                SetPrivateField(
                    bootstrap,
                    "clientSpawnPoint",
                    clientSpawnObject.transform);

                InvokePrivate(bootstrap, "Awake");

                InvokePrivate(
                    bootstrap,
                    "ConfigureClientPlayers",
                    22u,
                    11u);

                var registry =
                    bootstrapType
                        .GetProperty("Registry")
                        .GetValue(bootstrap)
                        as NetworkPlayerRegistry;

                var localPlayer =
                    bootstrapType
                        .GetProperty("LocalPlayer")
                        .GetValue(bootstrap)
                        as GameObject;

                remotePlayer =
                    bootstrapType
                        .GetProperty("RemotePlayer")
                        .GetValue(bootstrap)
                        as GameObject;

                Assert.That(registry, Is.Not.Null);
                Assert.That(registry.Count, Is.EqualTo(2));

                Assert.That(
                    registry.TryGetPlayer(
                        22u,
                        out GameObject registeredClient),
                    Is.True);

                Assert.That(
                    registry.TryGetPlayer(
                        11u,
                        out GameObject registeredHost),
                    Is.True);

                Assert.That(
                    registeredClient,
                    Is.SameAs(scenePlayer));

                Assert.That(
                    registeredHost,
                    Is.SameAs(remotePlayer));

                Assert.That(
                    localPlayer,
                    Is.SameAs(scenePlayer));

                Assert.That(
                    remotePlayer,
                    Is.Not.Null);

                Assert.That(
                    scenePlayer.transform.position,
                    Is.EqualTo(
                        clientSpawnObject.transform.position));

                Assert.That(
                    remotePlayer.transform.position,
                    Is.EqualTo(
                        hostSpawnObject.transform.position));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(
                    bootstrapObject);

                if (remotePlayer != null)
                {
                    UnityEngine.Object.DestroyImmediate(
                        remotePlayer);
                }

                UnityEngine.Object.DestroyImmediate(
                    scenePlayer);

                UnityEngine.Object.DestroyImmediate(
                    hostSpawnObject);

                UnityEngine.Object.DestroyImmediate(
                    clientSpawnObject);
            }
        }

        [Test]
        public void BindCameraToLocalPlayer_AssignsBootstrapLocalPlayer()
        {
            Type bootstrapType =
                FindType(BootstrapTypeName);

            Type cameraFollowType =
                FindType("CameraFollow");

            Assert.That(bootstrapType, Is.Not.Null);
            Assert.That(cameraFollowType, Is.Not.Null);

            var bootstrapObject =
                new GameObject("Bootstrap Test");

            var cameraObject =
                new GameObject("Camera Test");

            var localPlayer =
                new GameObject("Local Player");

            bootstrapObject.SetActive(false);
            cameraObject.SetActive(false);

            try
            {
                cameraObject.AddComponent<Camera>();

                Component cameraFollow =
                    cameraObject.AddComponent(
                        cameraFollowType);

                Component bootstrap =
                    bootstrapObject.AddComponent(
                        bootstrapType);

                SetPrivateField(
                    bootstrap,
                    "localPlayer",
                    localPlayer);

                SetPrivateField(
                    bootstrap,
                    "cameraFollow",
                    cameraFollow);

                InvokePrivate(
                    bootstrap,
                    "BindCameraToLocalPlayer");

                PropertyInfo targetProperty =
                    cameraFollowType.GetProperty(
                        "Target",
                        BindingFlags.Instance |
                        BindingFlags.Public);

                Assert.That(
                    targetProperty,
                    Is.Not.Null,
                    "CameraFollow must expose Target.");

                var actualTarget =
                    targetProperty.GetValue(cameraFollow)
                        as Transform;

                Assert.That(
                    actualTarget,
                    Is.SameAs(localPlayer.transform));

                Assert.That(
                    ((Behaviour)cameraFollow).enabled,
                    Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(
                    bootstrapObject);

                UnityEngine.Object.DestroyImmediate(
                    cameraObject);

                UnityEngine.Object.DestroyImmediate(
                    localPlayer);
            }
        }

        [Test]
        public void BindHealthBarToLocalPlayer_BindsBootstrapLocalPlayer()
        {
            Type bootstrapType =
                FindType(BootstrapTypeName);

            Type healthBarViewType =
                FindType(
                    "TopDownRoguelike.Gameplay.UI." +
                    "HealthBarView");

            Assert.That(bootstrapType, Is.Not.Null);
            Assert.That(healthBarViewType, Is.Not.Null);

            var bootstrapObject =
                new GameObject("Bootstrap Test");

            var localPlayer =
                new GameObject("Local Player");

            GameObject healthBarObject = null;

            bootstrapObject.SetActive(false);
            localPlayer.SetActive(false);

            try
            {
                Component playerHealth =
                    AddInitializedPlayerHealth(
                        localPlayer);

                Component healthBarView =
                    CreateHealthBarView(
                        healthBarViewType,
                        out healthBarObject);

                Component bootstrap =
                    bootstrapObject.AddComponent(
                        bootstrapType);

                SetPrivateField(
                    bootstrap,
                    "localPlayer",
                    localPlayer);

                SetPrivateField(
                    bootstrap,
                    "healthBarView",
                    healthBarView);

                InvokePrivate(
                    bootstrap,
                    "BindHealthBarToLocalPlayer");

                FieldInfo boundHealthField =
                    healthBarViewType.GetField(
                        "boundPlayerHealth",
                        BindingFlags.Instance |
                        BindingFlags.NonPublic);

                Assert.That(boundHealthField, Is.Not.Null);

                Assert.That(
                    boundHealthField.GetValue(healthBarView),
                    Is.SameAs(playerHealth));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(
                    bootstrapObject);

                UnityEngine.Object.DestroyImmediate(
                    localPlayer);

                if (healthBarObject != null)
                {
                    UnityEngine.Object.DestroyImmediate(
                        healthBarObject);
                }
            }
        }

        private static Component AddInitializedPlayerHealth(
    GameObject playerObject)
        {
            Type playerHealthType =
                FindType(
                    "TopDownRoguelike.Gameplay.Characters." +
                    "PlayerHealth");

            Assert.That(playerHealthType, Is.Not.Null);

            Component playerHealth =
                playerObject.AddComponent(
                    playerHealthType);

            InvokePrivate(
                playerHealth,
                "Awake");

            return playerHealth;
        }

        private static Component CreateHealthBarView(
            Type healthBarViewType,
            out GameObject viewObject)
        {
            viewObject =
                new GameObject("Health Bar View Test");

            viewObject.SetActive(false);

            var sliderObject =
                new GameObject(
                    "Health Slider",
                    typeof(RectTransform));

            var textObject =
                new GameObject(
                    "Health Text",
                    typeof(RectTransform));

            sliderObject.transform.SetParent(
                viewObject.transform);

            textObject.transform.SetParent(
                viewObject.transform);

            Slider slider =
                sliderObject.AddComponent<Slider>();

            TMP_Text healthText =
                textObject.AddComponent<
                    TextMeshProUGUI>();

            Component healthBarView =
                viewObject.AddComponent(
                    healthBarViewType);

            SetPrivateField(
                healthBarView,
                "healthSlider",
                slider);

            SetPrivateField(
                healthBarView,
                "healthText",
                healthText);

            return healthBarView;
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
            foreach (var assembly in
                AppDomain.CurrentDomain.GetAssemblies())
            {
                Type result =
                    assembly.GetType(fullTypeName, false);

                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }
    }
}