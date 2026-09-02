using System;
using System.Collections.Generic;
using TopDownRoguelike.Gameplay.Core;
using TopDownRoguelike.Gameplay.Upgrades;
using TopDownRoguelike.Infrastructure;
using TopDownRoguelike.Networking.Protocol;
using UnityEngine;

namespace TopDownRoguelike.Gameplay.Networking
{
    public enum NetworkUpgradeState
    {
        Idle = 0,
        WaitingForChoices = 1,
        Completed = 2
    }

    public sealed class NetworkUpgradeCoordinator
        : MonoBehaviour
    {
        private UpgradeManager upgradeManager;
        private GameManager gameManager;
        private uint currentSequence;
        private IReadOnlyList<UpgradeData> currentOptions =
            Array.Empty<UpgradeData>();
        private readonly Dictionary<uint, ushort> submittedChoices =
            new Dictionary<uint, ushort>();

        public UpgradeManager UpgradeManager =>
            upgradeManager;

        public GameManager GameManager =>
            gameManager;

        public NetworkUpgradeState State {
            get;
            private set;
        } = NetworkUpgradeState.Idle;

        public bool IsConfigured =>
            upgradeManager != null &&
            gameManager != null;

        public uint CurrentSequence =>
            currentSequence;

        public IReadOnlyList<UpgradeData> CurrentOptions =>
            currentOptions;

        public IReadOnlyDictionary<uint, ushort> SubmittedChoices =>
            submittedChoices;

        public bool AllChoicesSubmitted =>
            submittedChoices.Count >= 2;

        public event Action<
            uint,
            IReadOnlyList<UpgradeData>>
            UpgradeStarted;

        public void Configure(
            UpgradeManager newUpgradeManager,
            GameManager newGameManager)
        {
            upgradeManager =
                newUpgradeManager ??
                throw new ArgumentNullException(
                    nameof(newUpgradeManager));

            gameManager =
                newGameManager ??
                throw new ArgumentNullException(
                    nameof(newGameManager));

            State = NetworkUpgradeState.Idle;
            currentSequence = 0u;
            currentOptions = Array.Empty<UpgradeData>();
            submittedChoices.Clear();
        }

        public void BeginHostUpgrade(uint sequence)
        {
            if (!IsConfigured)
            {
                throw new InvalidOperationException(
                    "Network upgrade coordinator is not configured.");
            }

            if (!GameSession.IsHost)
            {
                throw new InvalidOperationException(
                    "Only the room host can begin a network upgrade.");
            }

            if (sequence == 0u)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(sequence));
            }

            if (State != NetworkUpgradeState.Idle)
            {
                throw new InvalidOperationException(
                    "A network upgrade is already in progress.");
            }

            IReadOnlyList<UpgradeData> options =
                upgradeManager.GenerateUpgradeOptions();

            if (options.Count == 0)
            {
                throw new InvalidOperationException(
                    "At least one upgrade option is required.");
            }

            var optionIds = new HashSet<ushort>();
            foreach (UpgradeData option in options)
            {
                if (option == null ||
                    option.UpgradeId == 0 ||
                    !optionIds.Add(option.UpgradeId))
                {
                    throw new InvalidOperationException(
                        "Upgrade options must have unique non-zero IDs.");
                }
            }

            gameManager.PauseGame();
            currentSequence = sequence;
            currentOptions = options;
            submittedChoices.Clear();
            State = NetworkUpgradeState.WaitingForChoices;
            UpgradeStarted?.Invoke(sequence, options);
        }

        public bool TrySubmitChoice(
            uint playerId,
            ushort upgradeId)
        {
            bool isKnownOption = false;
            for (int i = 0; i < currentOptions.Count; i++)
            {
                if (currentOptions[i] != null &&
                    currentOptions[i].UpgradeId == upgradeId)
                {
                    isKnownOption = true;
                    break;
                }
            }

            if (State != NetworkUpgradeState.WaitingForChoices ||
                playerId == 0u ||
                !isKnownOption ||
                submittedChoices.ContainsKey(playerId))
            {
                return false;
            }

            submittedChoices.Add(playerId, upgradeId);
            ChoiceSubmitted?.Invoke(playerId, upgradeId);
            return true;
        }

        public bool ApplyRemoteUpgradeStart(
            UpgradeStartedPayload payload)
        {
            if (!IsConfigured || payload == null ||
                payload.Sequence == 0u ||
                payload.UpgradeIds.Count < 1 ||
                payload.UpgradeIds.Count > 3 ||
                State != NetworkUpgradeState.Idle)
            {
                return false;
            }

            var options = new List<UpgradeData>();
            var ids = new HashSet<ushort>();
            foreach (ushort upgradeId in payload.UpgradeIds)
            {
                if (!ids.Add(upgradeId) ||
                    !upgradeManager.TryGetUpgradeById(
                        upgradeId,
                        out UpgradeData option))
                {
                    return false;
                }

                options.Add(option);
            }

            currentSequence = payload.Sequence;
            currentOptions = options;
            submittedChoices.Clear();
            gameManager.PauseGame();
            State = NetworkUpgradeState.WaitingForChoices;
            UpgradeStarted?.Invoke(
                payload.Sequence,
                currentOptions);
            return true;
        }

        public event Action<uint, ushort> ChoiceSubmitted;

        public event Action<uint, UpgradeData> UpgradeApplied;

        public event Action<
            uint,
            IReadOnlyDictionary<uint, ushort>>
            UpgradeCompleted;

        public void CompleteHostUpgrade()
        {
            if (!IsConfigured)
            {
                throw new InvalidOperationException(
                    "Network upgrade coordinator is not configured.");
            }

            if (!GameSession.IsHost)
            {
                throw new InvalidOperationException(
                    "Only the room host can complete a network upgrade.");
            }

            if (State != NetworkUpgradeState.WaitingForChoices ||
                !AllChoicesSubmitted)
            {
                throw new InvalidOperationException(
                    "Both players must submit a choice first.");
            }

            foreach (KeyValuePair<uint, ushort> choice
                in submittedChoices)
            {
                UpgradeData selectedOption = null;
                for (int i = 0; i < currentOptions.Count; i++)
                {
                    if (currentOptions[i] != null &&
                        currentOptions[i].UpgradeId == choice.Value)
                    {
                        selectedOption = currentOptions[i];
                        break;
                    }
                }

                UpgradeApplied?.Invoke(
                    choice.Key,
                    selectedOption);
            }

            UpgradeCompleted?.Invoke(
                currentSequence,
                submittedChoices);

            State = NetworkUpgradeState.Completed;
            gameManager.ResumeGame();
        }

        public bool ApplyRemoteUpgradeCompletion(
            uint localPlayerId,
            UpgradeCompletedPayload payload)
        {
            if (!IsConfigured || !GameSession.IsClient ||
                localPlayerId == 0u || payload == null ||
                State != NetworkUpgradeState.WaitingForChoices ||
                payload.Sequence != currentSequence ||
                !payload.Choices.TryGetValue(
                    localPlayerId,
                    out ushort upgradeId))
            {
                return false;
            }

            UpgradeData selectedOption = null;
            for (int i = 0; i < currentOptions.Count; i++)
            {
                if (currentOptions[i] != null &&
                    currentOptions[i].UpgradeId == upgradeId)
                {
                    selectedOption = currentOptions[i];
                    break;
                }
            }

            if (selectedOption == null)
            {
                return false;
            }

            upgradeManager.ApplyUpgrade(selectedOption);
            UpgradeApplied?.Invoke(localPlayerId, selectedOption);
            State = NetworkUpgradeState.Completed;
            gameManager.ResumeGame();
            return true;
        }

        public void ResetState()
        {
            State = NetworkUpgradeState.Idle;
            currentSequence = 0u;
            currentOptions = Array.Empty<UpgradeData>();
            submittedChoices.Clear();
        }
    }
}
