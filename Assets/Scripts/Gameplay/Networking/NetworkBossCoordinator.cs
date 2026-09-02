using System;
using TopDownRoguelike.Gameplay.Core;
using TopDownRoguelike.Infrastructure;
using UnityEngine;

namespace TopDownRoguelike.Gameplay.Networking
{
    public enum NetworkBossCombatState
    {
        Idle = 0,
        Started = 1,
        Paused = 2,
        Resumed = 3
    }

    public sealed class NetworkBossCoordinator : MonoBehaviour
    {
        private GameManager gameManager;
        private bool applyingRemoteState;

        public GameManager GameManager => gameManager;

        public NetworkBossCombatState State { get; private set; } =
            NetworkBossCombatState.Idle;

        public bool IsConfigured => gameManager != null;

        public event Action<NetworkBossCombatState> StateBroadcastRequested;

        public event Action<NetworkBossCombatState> StateApplied;

        public void Configure(GameManager newGameManager)
        {
            if (gameManager != null)
                gameManager.OnStateChanged -= HandleGameStateChanged;
            gameManager = newGameManager ??
                throw new ArgumentNullException(nameof(newGameManager));
            gameManager.OnStateChanged += HandleGameStateChanged;
            State = NetworkBossCombatState.Idle;
        }

        public void BeginHostBossBattle()
        {
            EnsureConfigured();
            EnsureHost();
            ApplyState(NetworkBossCombatState.Started, true);
        }

        public void PauseHostBossBattle()
        {
            EnsureConfigured();
            EnsureHost();
            ApplyState(NetworkBossCombatState.Paused, true);
        }

        public void ResumeHostBossBattle()
        {
            EnsureConfigured();
            EnsureHost();
            ApplyState(NetworkBossCombatState.Resumed, true);
        }

        public bool ApplyRemoteState(NetworkBossCombatState state)
        {
            EnsureConfigured();

            if (!GameSession.IsClient ||
                (state != NetworkBossCombatState.Started &&
                 state != NetworkBossCombatState.Paused &&
                 state != NetworkBossCombatState.Resumed))
            {
                return false;
            }

            ApplyState(state, false);
            return true;
        }

        private void ApplyState(
            NetworkBossCombatState state,
            bool broadcast)
        {
            State = state;
            applyingRemoteState = true;
            ApplyStateToGameManager(state);
            applyingRemoteState = false;
            StateApplied?.Invoke(state);

            if (broadcast)
            {
                StateBroadcastRequested?.Invoke(state);
            }
        }

        private void HandleGameStateChanged(GameState state)
        {
            if (!GameSession.IsHost || applyingRemoteState)
                return;

            if (state == GameState.BossBattle &&
                State != NetworkBossCombatState.Started)
            {
                State = NetworkBossCombatState.Started;
                StateBroadcastRequested?.Invoke(State);
            }
            else if (state == GameState.Paused &&
                     State == NetworkBossCombatState.Started)
            {
                State = NetworkBossCombatState.Paused;
                StateBroadcastRequested?.Invoke(State);
            }
            else if (state == GameState.Playing &&
                     State == NetworkBossCombatState.Paused)
            {
                State = NetworkBossCombatState.Resumed;
                StateBroadcastRequested?.Invoke(State);
            }
        }

        private void ApplyStateToGameManager(
            NetworkBossCombatState state)
        {
            switch (state)
            {
                case NetworkBossCombatState.Started:
                    gameManager.ApplyNetworkBossStarted();
                    break;

                case NetworkBossCombatState.Paused:
                    gameManager.ApplyNetworkBossPaused();
                    break;

                case NetworkBossCombatState.Resumed:
                    gameManager.ApplyNetworkBossResumed();
                    break;
            }
        }

        private void EnsureConfigured()
        {
            if (!IsConfigured)
            {
                throw new InvalidOperationException(
                    "Network Boss coordinator is not configured.");
            }
        }

        private static void EnsureHost()
        {
            if (!GameSession.IsHost)
            {
                throw new InvalidOperationException(
                    "Only the room host can change Boss combat state.");
            }
        }

        private void OnDestroy()
        {
            if (gameManager != null)
                gameManager.OnStateChanged -= HandleGameStateChanged;
        }
    }
}
