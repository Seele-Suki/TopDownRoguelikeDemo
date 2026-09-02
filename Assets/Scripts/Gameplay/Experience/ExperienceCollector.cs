using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TopDownRoguelike.Infrastructure;

namespace TopDownRoguelike.Gameplay.Experience
{
    public class ExperienceCollector : MonoBehaviour
    {
        [SerializeField] private LevelSystem levelSystem;
        [SerializeField] private SharedExperienceState sharedExperienceState;

        private void Awake()
        {
            if (levelSystem == null)
            {
                levelSystem = GetComponent<LevelSystem>();
            }

            if (sharedExperienceState == null)
            {
                sharedExperienceState =
                    FindObjectOfType<SharedExperienceState>();
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (GameSession.IsClient)
            {
                return;
            }

            if (!other.TryGetComponent(out ExperienceOrb orb))
            {
                return;
            }

            if (sharedExperienceState == null)
            {
                sharedExperienceState =
                    FindObjectOfType<SharedExperienceState>();
            }

            if (sharedExperienceState != null)
            {
                sharedExperienceState.AddExperience(
                    orb.ExperienceAmount);
            }
            else
            {
                levelSystem.AddExperience(orb.ExperienceAmount);
            }
            orb.Collect();
        }
    }
}
