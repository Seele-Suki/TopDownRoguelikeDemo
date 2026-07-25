using UnityEngine;

namespace TopDownRoguelike.Gameplay.Skills
{
    [CreateAssetMenu(
        fileName = "NewShotgunData",
        menuName = "TopDown Roguelike/Skills/Shotgun Data")]
    public class ShotgunData : ScriptableObject
    {
        [Header("Base Settings")]
        [SerializeField, Min(0f)] private float cooldown = 4f;
        [SerializeField, Min(1)] private int projectileCount = 5;
        [SerializeField, Range(0f, 180f)] private float spreadAngle = 40f;
        [SerializeField, Min(1)] private int projectileDamage = 1;
        [SerializeField, Min(0)] private int penetrationCount = 1;

        [Header("Upgrade Limits")]
        [SerializeField, Min(0f)] private float minCooldown = 1.5f;
        [SerializeField, Min(1)] private int maxProjectileCount = 11;
        [SerializeField, Min(0)] private int maxPenetrationCount = 3;

        public float Cooldown => cooldown;
        public int ProjectileCount => projectileCount;
        public float SpreadAngle => spreadAngle;
        public int ProjectileDamage => projectileDamage;
        public int PenetrationCount => penetrationCount;

        public float MinCooldown => minCooldown;
        public int MaxProjectileCount => maxProjectileCount;
        public int MaxPenetrationCount => maxPenetrationCount;
    }
}