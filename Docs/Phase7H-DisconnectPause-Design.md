# Phase 7H Disconnect Pause Design

## Goal

When a guest leaves during gameplay, the host must pause the complete gameplay simulation while the disconnect dialog is visible. The host can then continue as single-player or leave safely without losing the current run state.

## Architecture

NetworkDisconnectDialogBinder remains the single decision point for whether a disconnect is a host gameplay interruption. For a host receiving RemotePeerLeft while in gameplay, it captures the current Time.timeScale and sets Time.timeScale to 0.

DisconnectDialogActionHandler restores the captured time scale exactly once when the host chooses Continue Single Player. Exit and return-to-menu paths do not need to restore it because the gameplay scene is leaving, but cleanup must still be idempotent.

The existing NetworkGameBootstrap.ContinueAsSinglePlayer() remains responsible for removing the remote player and switching GameSession to single-player. No gameplay authority or progress state is reset.

## Rules

- Only a host in gameplay pauses for RemotePeerLeft.
- Clients never pause because of this host-only continuation flow.
- Single-player mode never starts disconnect pause handling.
- Duplicate disconnect notifications do not overwrite the saved time scale.
- Continue restores the exact pre-pause time scale before gameplay resumes.
- Current health, experience, Boss state, and battle progress remain in memory.
- Menu transitions may leave the time scale unchanged because the gameplay scene is unloaded.

## Tests

EditMode tests must cover host gameplay pause, duplicate notification idempotence, client/non-gameplay no-op behavior, exact time-scale restoration, and continue/exit action integration.
