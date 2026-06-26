using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TopDownRoguelike.Gameplay.Combat
{
    public struct DamageInfo
    {
        public int Damage;
        public Vector2 HitDirection;
        public GameObject Source;

        public DamageInfo(int damage, Vector2 hitDirection, GameObject source)
        {
            Damage = damage;
            HitDirection = hitDirection;
            Source = source;
        }
    }
}