# Build and Run

## Requirements

- Unity `2022.3.62f3c1`
- Visual Studio 2022 with **Desktop development with C++**
- MSVC v143 x64/x86 build tools
- Windows 10/11 SDK
- CMake 3.20 or newer
- Windows x64

## Build the Server and C++ Tests

From the repository root in a Visual Studio x64 Developer PowerShell:

```powershell
cmake -S NetworkServer -B NetworkServer/build -G "Visual Studio 17 2022" -A x64 -DBUILD_TESTING=ON
cmake --build NetworkServer/build --config Debug
ctest --test-dir NetworkServer/build -C Debug --output-on-failure
```

The server executable is normally:
`NetworkServer/build/Debug/NetworkServer.exe`.
The default TCP and UDP port is `7777`; the server accepts a port argument.

Manual start:

```powershell
.\NetworkServer\build\Debug\NetworkServer.exe 7777
```

The console is the server log. Keep it open while testing connections.

## Unity Server Startup Modes

`ServerProcessLauncher` supports:

- **Manual**: start `NetworkServer.exe` yourself. Unity must not terminate it.
- **Automatic**: host Unity starts the configured executable and terminates
  only that owned process during shutdown.

For development, place the executable at
`NetworkServer/build/Debug/NetworkServer.exe` relative to the project, or set
an absolute executable path in the launcher. For a distribution, place
`NetworkServer.exe` beside the Unity executable so the launcher resolves it
from the application directory.

## Unity Windows Build

In Unity: `File > Build Settings` (中文：`文件 > 生成设置`). Select **Windows,
Mac, Linux**, set **Architecture: x86_64**, and enable **Development Build**
for the test build. Ensure `MainMenu` is index 0 and `SampleScene` is index 1.

Recommended output:

```text
F:\Unity\Builds\GoldenSandCaravan\
```

Build the client as `GoldenSandCaravan.exe`, then copy the server executable
and its required runtime files into the distribution directory as documented
by the CMake output. After Development Build tests pass, produce a second
non-Development build with the same scene list.

## Run Configurations

1. **Editor + server**: start the server on port 7777, run the Unity Editor,
   create a host room, and use `::1` for same-machine testing.
2. **One packaged client**: start the server manually or configure automatic
   startup, then launch `GoldenSandCaravan.exe` and connect to the server.
3. **Two packaged clients**: start one host and one joining client; use the
   host's IPv6 address and port `7777` in the joining client's fields.

The client log is the Unity Console in the Editor or the player log beside the
Windows build. Check the server console for bind, room, heartbeat, forwarding,
and disconnect messages. A connection failure should first be checked against
the address family, port, firewall, and whether the server is listening.

## Distribution Hygiene

Do not commit `NetworkServer/build`, Unity build directories, generated logs,
player logs, or private IPv6 addresses. Keep only source, tests, documentation,
and intentionally distributed runtime files under version control.
