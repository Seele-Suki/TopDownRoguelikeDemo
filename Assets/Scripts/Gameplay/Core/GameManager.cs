using System;
using UnityEngine;
using TopDownRoguelike.Gameplay.Characters;

namespace TopDownRoguelike.Gameplay.Core
{
    public class GameManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerHealth playerHealth;

        [Header("Run Configuration")]
        [SerializeField] private RunConfig runConfig;

        [Header("Runtime Debug")]
        [SerializeField] private GameState currentState;
        [SerializeField] private float elapsedTime;

        private bool initialized;
        private GameState stateBeforePause;

        public GameState CurrentState => currentState;
        public float ElapsedTime => elapsedTime;

        public event Action<GameState> OnStateChanged;
        public event Action OnBossTransitionRequested;

        private void Awake()
        {
            if (runConfig == null)
            {
                Debug.LogError("GameManager: RunConfig is not assigned.");
            }
            Time.timeScale = 1f;
        }

        private void Start()
        {
            ChangeState(GameState.Playing);
        }

        private void Update()
        {
            bool clockRunning =
                currentState == GameState.Playing ||
                currentState == GameState.BossTransition ||
                currentState == GameState.BossBattle;

            if (!clockRunning)
            {
                return;
            }

            elapsedTime += Time.deltaTime;

            if (currentState == GameState.Playing && runConfig != null && elapsedTime >= runConfig.BossStartTime)
            {
                ChangeState(GameState.BossTransition);
                OnBossTransitionRequested?.Invoke();
            }
        }

        public void PauseGame()
        {
            if (currentState == GameState.Paused ||
                currentState == GameState.Victory ||
                currentState == GameState.Defeat)
            {
                return;
            }

            stateBeforePause = currentState;
            ChangeState(GameState.Paused);
        }

        public void ResumeGame()
        {
            if (currentState == GameState.Paused)
            {
                ChangeState(stateBeforePause);
            }
        }

        public void StartBossBattle() =>
            ChangeState(GameState.BossBattle);

        public void NotifyVictory() =>
            ChangeState(GameState.Victory);

        public void NotifyDefeat() =>
            ChangeState(GameState.Defeat);

        private void ChangeState(GameState newState)
        {
            if (initialized && currentState == newState)
            {
                return;
            }

            initialized = true;
            currentState = newState;

            Time.timeScale =
                newState == GameState.Paused ||
                newState == GameState.Victory ||
                newState == GameState.Defeat
                    ? 0f
                    : 1f;

            Debug.Log($"Game state changed to: {newState}");
            OnStateChanged?.Invoke(newState);
        }

        private void OnEnable()
        {
            if (playerHealth != null)
            {
                playerHealth.OnDied += HandlePlayerDied;
            }
        }

        private void OnDisable()
        {
            if (playerHealth != null)
            {
                playerHealth.OnDied -= HandlePlayerDied;
            }
        }

        private void HandlePlayerDied()
        {
            NotifyDefeat();
        }
    }
}