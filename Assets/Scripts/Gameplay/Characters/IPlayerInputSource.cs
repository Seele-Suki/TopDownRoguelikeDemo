using UnityEngine;

namespace TopDownRoguelike.Gameplay.Characters
{
    public interface IPlayerInputSource
    {
        Vector2 MoveDirection { get; }

        Vector2 AimDirection { get; }

        bool IsFireHeld { get; }

        uint DashRequestSequence { get; }

        uint ShotgunRequestSequence { get; }
    }
}