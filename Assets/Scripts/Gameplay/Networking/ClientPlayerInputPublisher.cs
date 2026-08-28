using System;
using TopDownRoguelike.Gameplay.Characters;
using TopDownRoguelike.Networking.Protocol;
using UnityEngine;

namespace TopDownRoguelike.Gameplay.Networking
{
    public sealed class ClientPlayerInputPublisher
        : MonoBehaviour
    {
        private const float SendIntervalSeconds =
            1f / 20f;

        private IPlayerInputSource inputSource;
        private Action<PlayerInputPayload> sendInput;
        private Func<bool> canSendInput;
        private float elapsedSeconds;

        public void Configure(
            IPlayerInputSource newInputSource,
            Action<PlayerInputPayload> newSendInput)
        {
            ConfigureWithStateGuard(
                newInputSource,
                newSendInput,
                () => true);
        }

        public void ConfigureWithStateGuard(
            IPlayerInputSource newInputSource,
            Action<PlayerInputPayload> newSendInput,
            Func<bool> newCanSendInput)
        {
            inputSource =
                newInputSource ??
                throw new ArgumentNullException(
                    nameof(newInputSource));

            sendInput =
                newSendInput ??
                throw new ArgumentNullException(
                    nameof(newSendInput));

            canSendInput =
                newCanSendInput ??
                throw new ArgumentNullException(
                    nameof(newCanSendInput));

            elapsedSeconds =
                0f;

            enabled =
                true;
        }

        private void Update()
        {
            Advance(
                Time.deltaTime);
        }

        private void Advance(
            float deltaTime)
        {
            if (inputSource == null ||
                sendInput == null ||
                canSendInput == null ||
                !canSendInput() ||
                deltaTime <= 0f)
            {
                elapsedSeconds = 0f;
                return;
            }

            elapsedSeconds +=
                deltaTime;

            if (elapsedSeconds <
                SendIntervalSeconds)
            {
                return;
            }

            elapsedSeconds %=
                SendIntervalSeconds;

            Vector2 movement =
                inputSource.MoveDirection;

            Vector2 aim =
                inputSource.AimDirection;

            sendInput(
                new PlayerInputPayload(
                    movement.x,
                    movement.y,
                    aim.x,
                    aim.y));
        }

        private void OnDisable()
        {
            elapsedSeconds =
                0f;
        }
    }
}
