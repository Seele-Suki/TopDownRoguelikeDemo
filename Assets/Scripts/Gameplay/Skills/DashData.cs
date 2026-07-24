using UnityEngine;

namespace TopDownRoguelike.Gameplay.Skills
{
    [CreateAssetMenu(
        fileName = "NewDashData",
        menuName = "TopDown Roguelike/Skills/Dash Data")]
    public class DashData : ScriptableObject
    {
        [Header("Dash Settings")]
        [SerializeField, Min(0.1f)] private float dashSpeed = 12f;
        [SerializeField, Min(0.01f)] private float dashDuration = 0.15f;
        [SerializeField, Min(0f)] private float cooldown = 1.5f;

        public float DashSpeed => dashSpeed;
        public float DashDuration => dashDuration;
        public float Cooldown => cooldown;
    }
}