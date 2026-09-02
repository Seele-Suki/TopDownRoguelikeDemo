using System;
using TopDownRoguelike.Infrastructure;
using UnityEngine;

namespace TopDownRoguelike.Gameplay.Experience
{
    public sealed class SharedExperienceState : MonoBehaviour
    {
        [SerializeField] private int currentLevel = 1;
        [SerializeField] private int currentExperience;
        [SerializeField] private int experienceToNextLevel = 10;
        [SerializeField] private float experienceGrowthMultiplier = 1.25f;

        public int CurrentLevel => currentLevel;
        public int CurrentExperience => currentExperience;
        public int ExperienceToNextLevel => experienceToNextLevel;
        public event Action<int, int, int> StateChanged;

        public bool AddExperience(int amount)
        {
            if (GameSession.IsClient)
            {
                return false;
            }

            if (amount <= 0)
            {
                return false;
            }

            currentExperience = checked(currentExperience + amount);
            while (currentExperience >= experienceToNextLevel)
            {
                currentExperience -= experienceToNextLevel;
                currentLevel++;
                experienceToNextLevel = Mathf.Max(
                    experienceToNextLevel + 1,
                    Mathf.CeilToInt(
                        experienceToNextLevel * experienceGrowthMultiplier));
            }

            StateChanged?.Invoke(
                currentLevel,
                currentExperience,
                experienceToNextLevel);
            return true;
        }

        public void ApplyAuthoritativeState(
            int level,
            int experience,
            int experienceToNext)
        {
            if (level < 1 || experience < 0 || experienceToNext <= 0 ||
                experience >= experienceToNext)
            {
                throw new ArgumentOutOfRangeException(nameof(level));
            }

            currentLevel = level;
            currentExperience = experience;
            experienceToNextLevel = experienceToNext;
            StateChanged?.Invoke(
                currentLevel,
                currentExperience,
                experienceToNextLevel);
        }
    }
}
