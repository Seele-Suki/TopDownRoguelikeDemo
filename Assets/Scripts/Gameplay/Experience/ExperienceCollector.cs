using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TopDownRoguelike.Gameplay.Experience
{
    public class ExperienceCollector : MonoBehaviour
    {
        [SerializeField] private LevelSystem levelSystem;

        private void Awake()
        {
            if (levelSystem == null)
            {
                levelSystem = GetComponent<LevelSystem>();
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.TryGetComponent(out ExperienceOrb orb))
            {
                return;
            }

            levelSystem.AddExperience(orb.ExperienceAmount);
            orb.Collect();
        }
    }
}