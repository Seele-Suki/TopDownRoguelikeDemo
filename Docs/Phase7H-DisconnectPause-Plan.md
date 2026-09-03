# Phase 7H Disconnect Pause Implementation Plan

> Implement task-by-task with test checkpoints.

**Goal:** Freeze the host gameplay simulation while the guest-disconnect dialog is awaiting a decision, then restore gameplay exactly once.

**Architecture:** Add a small Networking-layer pause controller that captures the current Time.timeScale once and sets it to zero for the host gameplay RemotePeerLeft case. The controller restores the captured value on Continue Single Player and clears itself on menu/scene teardown. Existing NetworkGameBootstrap.ContinueAsSinglePlayer() remains responsible for removing network gameplay state and preserving the run.

**Tech Stack:** Unity 2022.3, C#, Unity Test Framework, Time.timeScale, existing disconnect policy and dialog events.

---

### Task 1: Pause state controller

**Files:**
- Create: Assets/Scripts/Networking/Client/DisconnectPauseController.cs
- Test: Assets/Tests/EditMode/DisconnectPauseControllerTests.cs

- [ ] Write tests for host gameplay pause, duplicate pause idempotence, exact restoration, and non-paused client/non-gameplay cases.
- [ ] Run Unity EditMode tests and confirm the new tests fail because the controller does not exist.
- [ ] Implement a static controller with TryPause(DisconnectContext), Restore(), and Clear(); only RoomRole.Host, gameplay context, and RemotePeerLeft may pause.
- [ ] Run the focused tests and the existing disconnect policy tests.

### Task 2: Binder integration

**Files:**
- Modify: Assets/Scripts/Networking/Client/NetworkDisconnectDialogBinder.cs
- Test: Assets/Tests/EditMode/NetworkDisconnectDialogBinderTests.cs or the existing disconnect dialog test file.

- [ ] Add a test proving that a host gameplay RemotePeerLeft notification pauses before the dialog is shown and duplicate notifications do not overwrite the saved scale.
- [ ] Call the controller from HandleDisconnect before dialogView.Show(...).
- [ ] Ensure non-host, non-gameplay, and non-remote disconnect reasons never pause.

### Task 3: Action and lifecycle integration

**Files:**
- Modify: Assets/Scripts/UI/Menu/DisconnectDialogActionHandler.cs
- Modify: Assets/Scripts/Networking/Client/DisconnectDialogView.cs
- Modify: Assets/Scripts/Gameplay/Networking/NetworkGameBootstrap.cs
- Test: Assets/Tests/EditMode/DisconnectDialogActionHandlerTests.cs and NetworkGameBootstrapTests.cs

- [ ] Add tests that Continue Single Player restores the captured scale once before continuing, and return/exit paths clear pause state.
- [ ] Restore pause state in the continue action before ContinueAsSinglePlayer().
- [ ] Clear pause state on return/exit paths and during bootstrap destruction.
- [ ] Preserve the current player, health, experience, Boss, and GameManager state; do not reload the gameplay scene for Continue Single Player.

### Task 4: Verification

- [ ] Run all relevant Unity EditMode tests with no new failures.
- [ ] Build the client and verify no C# compiler errors.
- [ ] In a two-client build, disconnect Guest, confirm Host enemies/Boss/projectiles/timer freeze while the dialog is visible, then confirm Continue Single Player resumes the same run.
- [ ] Verify Host Exit returns to the menu and a new room can be created afterward.
