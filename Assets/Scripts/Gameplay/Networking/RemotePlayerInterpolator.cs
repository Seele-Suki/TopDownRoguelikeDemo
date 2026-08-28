using TopDownRoguelike.Networking.Protocol;
using UnityEngine;

namespace TopDownRoguelike.Gameplay.Networking
{
    public sealed class RemotePlayerInterpolator
        : MonoBehaviour
    {
        [SerializeField]
        private float interpolationSpeed = 20f;

        private uint remotePlayerId;
        private Vector3 targetPosition;
        private Vector2 targetAimDirection;
        private bool hasTarget;

        public bool IsFireHeld
        {
            get;
            private set;
        }

        public void Configure(
            uint playerId)
        {
            if (playerId == 0u)
            {
                enabled = false;
                return;
            }

            remotePlayerId =
                playerId;

            targetPosition =
                transform.position;

            targetAimDirection =
                Vector2.zero;

            hasTarget =
                false;

            IsFireHeld =
                false;

            enabled =
                true;
        }

        public void ApplySnapshot(
            PlayerStateSnapshotPayload snapshot)
        {
            if (!enabled ||
                snapshot == null ||
                remotePlayerId == 0u)
            {
                return;
            }

            for (int index = 0;
                index < snapshot.Players.Count;
                index++)
            {
                PlayerStateRecord state =
                    snapshot.Players[index];

                if (state == null ||
                    state.PlayerId != remotePlayerId)
                {
                    continue;
                }

                targetPosition =
                    new Vector3(
                        state.PositionX,
                        state.PositionY,
                        transform.position.z);

                targetAimDirection =
                    new Vector2(
                        state.AimX,
                        state.AimY);

                IsFireHeld =
                    state.FireHeld;

                hasTarget =
                    true;

                return;
            }
        }

        private void Update()
        {
            Advance(
                Time.deltaTime);
        }

        private void Advance(
            float deltaTime)
        {
            if (!hasTarget ||
                deltaTime <= 0f)
            {
                return;
            }

            float interpolationFactor =
                Mathf.Clamp01(
                    deltaTime *
                    interpolationSpeed);

            transform.position =
                Vector3.Lerp(
                    transform.position,
                    targetPosition,
                    interpolationFactor);

            if (targetAimDirection.sqrMagnitude >
                0.0001f)
            {
                float angle =
                    Mathf.Atan2(
                        targetAimDirection.y,
                        targetAimDirection.x) *
                    Mathf.Rad2Deg;

                transform.rotation =
                    Quaternion.Euler(
                        0f,
                        0f,
                        angle);
            }
        }

        private void OnDisable()
        {
            hasTarget =
                false;

            IsFireHeld =
                false;
        }
    }
}