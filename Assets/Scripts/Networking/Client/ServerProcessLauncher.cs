using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace TopDownRoguelike.Networking.Client
{
    public enum ServerStartupMode
    {
        Manual,
        Automatic
    }

    public sealed class ServerProcessLauncher
        : MonoBehaviour
    {
        [Header("Server Process")]
        [SerializeField]
        private ServerStartupMode startupMode =
            ServerStartupMode.Manual;

        [SerializeField]
        private string executableFileName =
            "NetworkServer.exe";

        [SerializeField]
        [Range(1, 65535)]
        private int serverPort =
            7777;

        private Process startedProcess;

        public bool IsOwnedByCurrentUnityClient { get; private set; }

        public ServerStartupMode StartupMode =>
            startupMode;

        public bool ShouldStartAutomatically =>
            startupMode ==
            ServerStartupMode.Automatic;

        public string ExecutableFileName =>
            executableFileName;

        public int ServerPort =>
            serverPort;

        public bool HasStartedServerProcess =>
            startedProcess != null &&
            !startedProcess.HasExited;

        public void PrepareForHost()
        {
            if (!ShouldStartAutomatically)
            {
                IsOwnedByCurrentUnityClient = false;
                return;
            }

            StartConfiguredServer();
            IsOwnedByCurrentUnityClient = true;
        }

        private void OnDestroy()
        {
            StopStartedServer();
        }

        private void StopStartedServer()
        {
            Process process =
                startedProcess;

            startedProcess =
                null;

            // A process stored in startedProcess is owned by this launcher.
            // External/manual servers are never assigned to this field.
            IsOwnedByCurrentUnityClient = false;

            if (process == null)
            {
                return;
            }

            try
            {
                if (!process.HasExited)
                {
                    process.Kill();

                    if (!process.WaitForExit(2000))
                    {
                        Debug.LogWarning(
                            "NetworkServer did not exit within " +
                            "the shutdown timeout.",
                            this);
                    }
                }
            }
            catch (InvalidOperationException)
            {
                // The process already exited between checks.
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    "Failed to stop NetworkServer: " +
                    exception.Message,
                    this);
            }
            finally
            {
                process.Dispose();
            }
        }

        public ProcessStartInfo CreateStartInfo()
        {
            string executablePath =
                ResolveExecutablePath();

            return new ProcessStartInfo
            {
                FileName = executablePath,
                Arguments =
                    serverPort.ToString(
                        CultureInfo.InvariantCulture),
                WorkingDirectory =
                    Path.GetDirectoryName(
                        executablePath),
                UseShellExecute = true
            };
        }

        private void StartConfiguredServer()
        {
            if (HasStartedServerProcess)
            {
                return;
            }

            if (startedProcess != null)
            {
                startedProcess.Dispose();
                startedProcess = null;
            }

            startedProcess =
                Process.Start(
                    CreateStartInfo());

            if (startedProcess == null)
            {
                throw new InvalidOperationException(
                    "Operating system did not create " +
                    "the server process.");
            }
        }

        public bool TryCloseOwnedServer()
        {
            if (!IsOwnedByCurrentUnityClient)
            {
                return false;
            }

            StopStartedServer();
            return true;
        }

        private string ResolveExecutablePath()
        {
            if (string.IsNullOrWhiteSpace(
                executableFileName))
            {
                throw new InvalidOperationException(
                    "Server executable file name is empty.");
            }

            if (Path.IsPathRooted(
                executableFileName))
            {
                string rootedPath =
                    Path.GetFullPath(
                        executableFileName);

                EnsureExecutableExists(
                    rootedPath);

                return rootedPath;
            }

            string applicationDirectory =
                Path.GetFullPath(
                    Path.Combine(
                        Application.dataPath,
                        ".."));

            string distributedPath =
                Path.Combine(
                    applicationDirectory,
                    executableFileName);

            if (File.Exists(distributedPath))
            {
                return Path.GetFullPath(
                    distributedPath);
            }

            string developmentPath =
                Path.Combine(
                    applicationDirectory,
                    "NetworkServer",
                    "build",
                    "Debug",
                    executableFileName);

            EnsureExecutableExists(
                developmentPath);

            return Path.GetFullPath(
                developmentPath);
        }

        private static void EnsureExecutableExists(
            string executablePath)
        {
            if (!File.Exists(executablePath))
            {
                throw new FileNotFoundException(
                    "NetworkServer executable was not found.",
                    executablePath);
            }
        }
    }
}
