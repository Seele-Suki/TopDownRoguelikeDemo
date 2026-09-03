using System;
using System.Reflection;
using System.Diagnostics;
using System.IO;
using System.Threading;
using NUnit.Framework;
using UnityEngine;

namespace TopDownRoguelike.Tests.EditMode
{
    public sealed class ServerProcessLauncherTests
    {
        [Test]
        public void PrepareForHost_ManualMode_DoesNotStartProcess()
        {
            var gameObject =
                new GameObject("ServerProcessLauncherTests");

            try
            {
                var launcher =
                    gameObject.AddComponent<
                        TopDownRoguelike.Networking.Client
                            .ServerProcessLauncher>();

                MethodInfo prepareMethod =
                    launcher.GetType().GetMethod(
                        "PrepareForHost",
                        BindingFlags.Instance |
                        BindingFlags.Public);

                Assert.That(
                    prepareMethod,
                    Is.Not.Null,
                    "Launcher must expose PrepareForHost().");

                prepareMethod.Invoke(
                    launcher,
                    null);

                Assert.That(
                    launcher.HasStartedServerProcess,
                    Is.False,
                    "Manual mode must use an externally started server.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(
                    gameObject);
            }
        }

        [Test]
        public void Component_DoesNotStartServerFromUnityStart()
        {
            MethodInfo startMethod =
                typeof(
                    TopDownRoguelike.Networking.Client
                        .ServerProcessLauncher)
                .GetMethod(
                    "Start",
                    BindingFlags.Instance |
                    BindingFlags.NonPublic);

            Assert.That(
                startMethod,
                Is.Null,
                "Server startup must wait for the host creation command.");
        }

        [Test]
        public void OnDestroy_StopsOwnedServerProcess()
        {
            var gameObject =
                new GameObject("ServerProcessLauncherTests");

            Process ownedProcess =
                StartLongRunningTestProcess();

            int processId =
                ownedProcess.Id;

            try
            {
                var launcher =
                    gameObject.AddComponent<
                        TopDownRoguelike.Networking.Client
                            .ServerProcessLauncher>();

                SetPrivateField(
                    launcher,
                    "startedProcess",
                    ownedProcess);

                MethodInfo onDestroyMethod =
                    launcher.GetType().GetMethod(
                        "OnDestroy",
                        BindingFlags.Instance |
                        BindingFlags.NonPublic);

                Assert.That(
                    onDestroyMethod,
                    Is.Not.Null,
                    "Launcher must stop its owned process on destroy.");

                onDestroyMethod.Invoke(
                    launcher,
                    null);

                bool processStopped =
                    SpinWait.SpinUntil(
                        () => !IsProcessRunning(processId),
                        2000);

                Assert.That(
                    processStopped,
                    Is.True,
                    "Owned process was not stopped.");
            }
            finally
            {
                StopTestProcessIfRunning(
                    processId);

                UnityEngine.Object.DestroyImmediate(
                    gameObject);
            }
        }

        [Test]
        public void OnDestroy_DoesNotStopUnownedProcess()
        {
            var gameObject =
                new GameObject("ServerProcessLauncherTests");

            Process manualProcess =
                StartLongRunningTestProcess();

            try
            {
                var launcher =
                    gameObject.AddComponent<
                        TopDownRoguelike.Networking.Client
                            .ServerProcessLauncher>();

                MethodInfo onDestroyMethod =
                    launcher.GetType().GetMethod(
                        "OnDestroy",
                        BindingFlags.Instance |
                        BindingFlags.NonPublic);

                Assert.That(
                    onDestroyMethod,
                    Is.Not.Null,
                    "Launcher must define OnDestroy.");

                onDestroyMethod.Invoke(
                    launcher,
                    null);

                Assert.That(
                    manualProcess.HasExited,
                    Is.False,
                    "Launcher must not stop an unowned process.");
            }
            finally
            {
                if (!manualProcess.HasExited)
                {
                    manualProcess.Kill();
                    manualProcess.WaitForExit(2000);
                }

                manualProcess.Dispose();

                UnityEngine.Object.DestroyImmediate(
                    gameObject);
            }
        }

        [Test]
        public void TryCloseOwnedServer_OnlyStopsOwnedProcess()
        {
            var gameObject = new GameObject("ServerProcessLauncherTests");
            var launcher = gameObject.AddComponent<TopDownRoguelike.Networking.Client.ServerProcessLauncher>();

            Assert.That(launcher.TryCloseOwnedServer(), Is.False);

            UnityEngine.Object.DestroyImmediate(gameObject);
        }

        [Test]
        public void CreateStartInfo_UsesExecutableAndPort()
        {
            var gameObject =
                new GameObject("ServerProcessLauncherTests");

            string temporaryExecutable =
                Path.GetTempFileName();

            try
            {
                var launcher =
                    gameObject.AddComponent<
                        TopDownRoguelike.Networking.Client
                            .ServerProcessLauncher>();

                SetPrivateField(
                    launcher,
                    "executableFileName",
                    temporaryExecutable);

                SetPrivateField(
                    launcher,
                    "serverPort",
                    8123);

                MethodInfo createStartInfoMethod =
                    launcher.GetType().GetMethod(
                        "CreateStartInfo",
                        BindingFlags.Instance |
                        BindingFlags.Public);

                Assert.That(
                    createStartInfoMethod,
                    Is.Not.Null,
                    "Launcher must create ProcessStartInfo.");

                var startInfo =
                    (ProcessStartInfo)
                        createStartInfoMethod.Invoke(
                            launcher,
                            null);

                Assert.That(
                    startInfo.FileName,
                    Is.EqualTo(
                        Path.GetFullPath(
                            temporaryExecutable)));

                Assert.That(
                    startInfo.Arguments,
                    Is.EqualTo("8123"));

                Assert.That(
                    startInfo.WorkingDirectory,
                    Is.EqualTo(
                        Path.GetDirectoryName(
                            Path.GetFullPath(
                                temporaryExecutable))));

                Assert.That(
                    startInfo.UseShellExecute,
                    Is.True);
            }
            finally
            {
                if (File.Exists(temporaryExecutable))
                {
                    File.Delete(temporaryExecutable);
                }

                UnityEngine.Object.DestroyImmediate(
                    gameObject);
            }
        }

        [Test]
        public void DefaultStartupMode_IsManual()
        {
            Type launcherType =
                typeof(
                    TopDownRoguelike.Networking.Client
                        .NetworkClient)
                .Assembly
                .GetType(
                    "TopDownRoguelike.Networking.Client." +
                    "ServerProcessLauncher");

            Assert.That(launcherType, Is.Not.Null);

            var gameObject =
                new GameObject("ServerProcessLauncherTests");

            try
            {
                Component launcher =
                    gameObject.AddComponent(launcherType);

                PropertyInfo startupModeProperty =
                    launcherType.GetProperty("StartupMode");

                PropertyInfo automaticProperty =
                    launcherType.GetProperty(
                        "ShouldStartAutomatically");

                Assert.That(startupModeProperty, Is.Not.Null);
                Assert.That(automaticProperty, Is.Not.Null);

                Assert.That(
                    startupModeProperty.GetValue(launcher)
                        .ToString(),
                    Is.EqualTo("Manual"));

                Assert.That(
                    automaticProperty.GetValue(launcher),
                    Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(
                    gameObject);
            }
        }

        [Test]
        public void Component_ProvidesDefaultServerConfiguration()
        {
            Type launcherType =
                typeof(
                    TopDownRoguelike.Networking.Client
                        .NetworkClient)
                .Assembly
                .GetType(
                    "TopDownRoguelike.Networking.Client." +
                    "ServerProcessLauncher");

            Assert.That(
                launcherType,
                Is.Not.Null,
                "ServerProcessLauncher has not been created.");

            Assert.That(
                typeof(MonoBehaviour).IsAssignableFrom(
                    launcherType),
                Is.True,
                "ServerProcessLauncher must be a MonoBehaviour.");

            var gameObject =
                new GameObject(
                    "ServerProcessLauncherTests");

            try
            {
                Component launcher =
                    gameObject.AddComponent(
                        launcherType);

                PropertyInfo executableFileNameProperty =
                    launcherType.GetProperty(
                        "ExecutableFileName",
                        BindingFlags.Instance |
                        BindingFlags.Public);

                PropertyInfo serverPortProperty =
                    launcherType.GetProperty(
                        "ServerPort",
                        BindingFlags.Instance |
                        BindingFlags.Public);

                Assert.That(
                    executableFileNameProperty,
                    Is.Not.Null);

                Assert.That(
                    serverPortProperty,
                    Is.Not.Null);

                Assert.That(
                    executableFileNameProperty.GetValue(
                        launcher),
                    Is.EqualTo(
                        "NetworkServer.exe"));

                Assert.That(
                    serverPortProperty.GetValue(
                        launcher),
                    Is.EqualTo(7777));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(
                    gameObject);
            }
        }

        private static Process StartLongRunningTestProcess()
        {
            var startInfo =
                new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments =
                        "/c ping 127.0.0.1 -n 30 > nul",
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

            Process process =
                Process.Start(startInfo);

            Assert.That(process, Is.Not.Null);
            Assert.That(process.HasExited, Is.False);

            return process;
        }

        private static bool IsProcessRunning(
            int processId)
        {
            try
            {
                using (Process process =
                    Process.GetProcessById(processId))
                {
                    return !process.HasExited;
                }
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        private static void StopTestProcessIfRunning(
            int processId)
        {
            try
            {
                using (Process process =
                    Process.GetProcessById(processId))
                {
                    if (!process.HasExited)
                    {
                        process.Kill();
                        process.WaitForExit(2000);
                    }
                }
            }
            catch (ArgumentException)
            {
            }
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
                $"Missing field: {fieldName}");

            field.SetValue(
                target,
                value);
        }
    }
}
