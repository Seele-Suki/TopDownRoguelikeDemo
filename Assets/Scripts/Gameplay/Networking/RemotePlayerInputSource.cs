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

        public void ApplyInput(
            Vector2 moveDirection,
            Vector2 aimDirection)
        {
            MoveDirection =
                NormalizeIfNeeded(
                    moveDirection);

            AimDirection =
                NormalizeIfNeeded(
                    aimDirection);
        }

        public void ClearInput()
        {
            MoveDirection =
                Vector2.zero;

            AimDirection =
                Vector2.zero;
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