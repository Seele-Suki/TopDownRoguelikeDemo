using TopDownRoguelike.Gameplay.Characters;
using UnityEngine;

namespace TopDownRoguelike.Gameplay.Networking
{
    public sealed class RemotePlayerInputSource
        : MonoBehaviour,
          IPlayerInputSource
    {
        private bool hasDashRequestSequence;

        private bool hasShotgunRequestSequence;

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

        public uint DashRequestSequence
        {
            get;
            private set;
        }

        public uint ShotgunRequestSequence
        {
            get;
            private set;
        }

        public void ApplyInput(
            Vector2 moveDirection,
            Vector2 aimDirection)
        {
            ApplyInputState(
                moveDirection,
                aimDirection,
                false,
                DashRequestSequence,
                ShotgunRequestSequence);
        }

        public void ApplyInputWithFireState(
            Vector2 moveDirection,
            Vector2 aimDirection,
            bool fireHeld)
        {
            ApplyInputState(
                moveDirection,
                aimDirection,
                fireHeld,
                DashRequestSequence,
                ShotgunRequestSequence);
        }

        public void ApplyInputState(
            Vector2 moveDirection,
            Vector2 aimDirection,
            bool fireHeld,
            uint dashRequestSequence,
            uint shotgunRequestSequence)
        {
            MoveDirection =
                NormalizeIfNeeded(
                    moveDirection);

            AimDirection =
                NormalizeIfNeeded(
                    aimDirection);

            IsFireHeld =
                fireHeld;

            if (!hasDashRequestSequence ||
                IsNewerSequence(
                    dashRequestSequence,
                    DashRequestSequence))
            {
                DashRequestSequence =
                    dashRequestSequence;

                hasDashRequestSequence =
                    true;
            }

            if (!hasShotgunRequestSequence ||
                IsNewerSequence(
                    shotgunRequestSequence,
                    ShotgunRequestSequence))
            {
                ShotgunRequestSequence =
                    shotgunRequestSequence;

                hasShotgunRequestSequence =
                    true;
            }
        }

        public void ApplyInputState(
            Vector2 moveDirection,
            Vector2 aimDirection,
            bool fireHeld,
            uint dashRequestSequence)
        {
            MoveDirection =
                NormalizeIfNeeded(
                    moveDirection);

            AimDirection =
                NormalizeIfNeeded(
                    aimDirection);

            IsFireHeld =
                fireHeld;

            if (!hasDashRequestSequence ||
                IsNewerSequence(
                    dashRequestSequence,
                    DashRequestSequence))
            {
                DashRequestSequence =
                    dashRequestSequence;

                hasDashRequestSequence =
                    true;
            }
        }

        public void ClearInput()
        {
            MoveDirection =
                Vector2.zero;

            AimDirection =
                Vector2.zero;

            IsFireHeld =
                false;

            DashRequestSequence =
                0u;

            hasDashRequestSequence =
                false;

            ShotgunRequestSequence =
                0u;

            hasShotgunRequestSequence =
                false;
        }

        private static bool IsNewerSequence(
            uint candidate,
            uint baseline)
        {
            uint difference =
                unchecked(
                    candidate -
                    baseline);

            return difference != 0u &&
                difference < 0x80000000u;
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