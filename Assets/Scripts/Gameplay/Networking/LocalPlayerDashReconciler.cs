using TopDownRoguelike.Networking.Protocol;
using UnityEngine;

namespace TopDownRoguelike.Gameplay.Networking
{
    public sealed class LocalPlayerDashReconciler
        : MonoBehaviour
    {
        [SerializeField]
        private float interpolationSpeed = 20f;

        private uint localPlayerId;
        private PlayerController playerController;
        private Vector3 targetPosition;
        private Vector2 targetAimDirection;
        private bool hasDashTarget;
        private bool restorePlayerController;

        public bool IsDashing
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

            playerController =
                GetComponent<PlayerController>();

            if (playerController == null)
            {
                Debug.LogError(
                    "LocalPlayerDashReconciler requires " +
                    "PlayerController.",
                    this);

                enabled = false;
                return;
            }

            localPlayerId =
                playerId;

            targetPosition =
                transform.position;

            targetAimDirection =
                Vector2.zero;

            hasDashTarget =
                false;

            restorePlayerController =
                false;

            IsDashing =
                false;

            enabled =
                true;
        }

        public void ApplySnapshot(
            PlayerStateSnapshotPayload snapshot)
        {
            if (!enabled ||
                snapshot == null ||
                localPlayerId == 0u)
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
                    state.PlayerId != localPlayerId)
                {
                    continue;
                }

                ApplyState(
                    state);

                return;
            }
        }

        private void ApplyState(
            PlayerStateRecord state)
        {
            targetPosition =
                new Vector3(
                    state.PositionX,
                    state.PositionY,
                    transform.position.z);

            targetAimDirection =
                new Vector2(
                    state.AimX,
                    state.AimY);

            bool wasDashing =
                IsDashing;

            IsDashing =
                state.IsDashing;

            if (IsDashing)
            {
                if (!wasDashing)
                {
                    restorePlayerController =
                        playerController != null &&
                        playerController.enabled;
                }

                if (playerController != null)
                {
                    playerController.enabled =
                        false;
                }

                hasDashTarget =
                    true;

                return;
            }

            if (!wasDashing)
            {
                return;
            }

            transform.position =
                targetPosition;

            ApplyAimDirection();

            hasDashTarget =
                false;

            RestorePlayerController();
        }

        private void Update()
        {
            Advance(
                Time.deltaTime);
        }

        private void Advance(
            float deltaTime)
        {
            if (!hasDashTarget ||
                !IsDashing ||
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

            ApplyAimDirection();
        }

        private void ApplyAimDirection()
        {
            if (targetAimDirection.sqrMagnitude <
                0.0001f)
            {
                return;
            }

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

        private void RestorePlayerController()
        {
            if (playerController != null &&
                restorePlayerController)
            {
                playerController.enabled =
                    true;
            }

            restorePlayerController =
                false;
        }

        private void OnDisable()
        {
            RestorePlayerController();

            hasDashTarget =
                false;

            IsDashing =
                false;
        }
    }
}