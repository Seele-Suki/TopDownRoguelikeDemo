using System;
using TopDownRoguelike.Gameplay.Experience;
using TopDownRoguelike.Infrastructure;
using TopDownRoguelike.Networking.Protocol;
using UnityEngine;

namespace TopDownRoguelike.Gameplay.Networking
{
    public sealed class HostSharedExperiencePublisher : MonoBehaviour
    {
        private SharedExperienceState state;
        private Action<SharedExperienceSnapshotPayload> send;
        private uint sequence;

        public void Configure(
            SharedExperienceState newState,
            Action<SharedExperienceSnapshotPayload> newSend)
        {
            state = newState ?? throw new ArgumentNullException(nameof(newState));
            send = newSend ?? throw new ArgumentNullException(nameof(newSend));
            state.StateChanged -= HandleChanged;
            state.StateChanged += HandleChanged;
        }

        private void HandleChanged(int level, int experience, int next)
        {
            if (!GameSession.IsHost || send == null)
            {
                return;
            }

            sequence = unchecked(sequence + 1u);
            if (sequence == 0u) sequence = 1u;
            send(new SharedExperienceSnapshotPayload(
                sequence, level, experience, next));
        }

        private void OnDestroy()
        {
            if (state != null) state.StateChanged -= HandleChanged;
        }
    }
}
