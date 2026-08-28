using TopDownRoguelike.Gameplay.Characters;
using UnityEngine;

namespace TopDownRoguelike.Gameplay.Networking
{
    public sealed class RemotePlayerInputSource
        : MonoBehaviour,
          IPlayerInputSource
    {
        public Vector2 MoveDirection
        {
            get;
            private set;
        }

        public Vector2 AimDirection
        {
            get;
            private set;
        }

        public bool IsFireHeld
        {
            get;
            private set;
        }

        public void ApplyInput(
            Vector2 moveDirection,
            Vector2 aimDirection)
        {
            ApplyInputWithFireState(
                moveDirection,
                aimDirection,
                false);
        }

        public void ApplyInputWithFireState(
            Vector2 moveDirection,
            Vector2 aimDirection,
            bool fireHeld)
        {
            MoveDirection =
                NormalizeIfNeeded(
                    moveDirection);

            AimDirection =
                NormalizeIfNeeded(
                    aimDirection);

            IsFireHeld =
                fireHeld;
        }

        public void ClearInput()
        {
            MoveDirection =
                Vector2.zero;

            AimDirection =
                Vector2.zero;

            IsFireHeld =
                false;
        }

        private static Vector2 NormalizeIfNeeded(
            Vector2 direction)
        {
            if (direction.sqrMagnitude >
                1f)
            {
                return direction.normalized;
            }

            return direction;
        }

        private void OnDisable()
        {
            ClearInput();
        }
    }
}