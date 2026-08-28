using System;
using System.Collections.Generic;
using TopDownRoguelike.Networking.Gameplay;
using TopDownRoguelike.Networking.Protocol;
using UnityEngine;

namespace TopDownRoguelike.Gameplay.Networking
{
    public sealed class HostPlayerStatePublisher
        : MonoBehaviour
    {
        private const float SendIntervalSeconds =
            1f / 20f;

        private NetworkPlayerRegistry registry;
        private IReadOnlyList<uint> playerIds;
        private Action<PlayerStateSnapshotPayload>
            sendSnapshot;
        private float elapsedSeconds;

        public void Configure(
            NetworkPlayerRegistry newRegistry,
            IReadOnlyList<uint> newPlayerIds,
            Action<PlayerStateSnapshotPayload>
                newSendSnapshot)
        {
            registry =
                newRegistry ??
                throw new ArgumentNullException(
                    nameof(newRegistry));

            if (newPlayerIds == null)
            {
                throw new ArgumentNullException(
                    nameof(newPlayerIds));
            }

            playerIds =
                new List<uint>(newPlayerIds);

            sendSnapshot =
                newSendSnapshot ??
                throw new ArgumentNullException(
                    nameof(newSendSnapshot));

            elapsedSeconds = 0f;
            enabled = true;
        }

        private void Update()
        {
            Advance(Time.deltaTime);
        }

        private void Advance(float deltaTime)
        {
            if (registry == null ||
                playerIds == null ||
                sendSnapshot == null ||
                deltaTime <= 0f)
            {
                return;
            }

            elapsedSeconds += deltaTime;

            if (elapsedSeconds < SendIntervalSeconds)
            {
                return;
            }

            var playerStates =
                new List<PlayerStateRecord>(
                    playerIds.Count);

            for (int index = 0;
                index < playerIds.Count;
                index++)
            {
                uint playerId =
                    playerIds[index];

                if (!registry.TryGetPlayer(
                    playerId,
                    out GameObject player) ||
                player == null ||
                !player.TryGetComponent(
                    out PlayerController controller))
                {
                    elapsedSeconds = 0f;
                    return;
                }

                Vector3 position =
                    player.transform.position;

                Vector2 aim =
                    controller.AimDirection;

                playerStates.Add(
                    new PlayerStateRecord(
                        playerId,
                        position.x,
                        position.y,
                        aim.x,
                        aim.y));
            }

            elapsedSeconds %=
                SendIntervalSeconds;

            sendSnapshot(
                new PlayerStateSnapshotPayload(
                    playerStates));
        }

        private void OnDisable()
        {
            elapsedSeconds = 0f;
        }
    }
}