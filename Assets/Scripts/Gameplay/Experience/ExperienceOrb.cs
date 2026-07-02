using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TopDownRoguelike.Gameplay.Experience
{
    public class ExperienceOrb : MonoBehaviour
    {
        [SerializeField] private int experienceAmount = 1;

        public int ExperienceAmount => experienceAmount;

        public void Initialize(int amount)
        {
            experienceAmount = amount;
        }

        public void Collect()
        {
            Destroy(gameObject);
        }
    }
}