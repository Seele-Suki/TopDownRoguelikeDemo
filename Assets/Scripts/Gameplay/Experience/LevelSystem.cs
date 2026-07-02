using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

namespace TopDownRoguelike.Gameplay.Experience
{
    public class LevelSystem : MonoBehaviour
    {
        [SerializeField] private int currentLevel = 1;
        [SerializeField] private int currentExperience;
        [SerializeField] private int experienceToNextLevel = 10;
        [SerializeField] private float experienceGrowthMultiplier = 1.25f;

        public event Action<int> OnLevelChanged;
        public event Action<int, int> OnExperienceChanged;
        public event Action<int> OnLevelUp;

        public int CurrentLevel => currentLevel;
        public int CurrentExperience => currentExperience;
        public int ExperienceToNextLevel => experienceToNextLevel;

        private void Start()
        {
            OnLevelChanged?.Invoke(currentLevel);
            OnExperienceChanged?.Invoke(currentExperience, experienceToNextLevel);
        }

        public void AddExperience(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            currentExperience += amount;

            while (currentExperience >= experienceToNextLevel)
            {
                currentExperience -= experienceToNextLevel;
                LevelUp();
            }

            OnExperienceChanged?.Invoke(currentExperience, experienceToNextLevel);
        }

        private void LevelUp()
        {
            currentLevel++;
            experienceToNextLevel = Mathf.CeilToInt(experienceToNextLevel * experienceGrowthMultiplier);

            Debug.Log($"Level Up! Current Level: {currentLevel}");

            OnLevelChanged?.Invoke(currentLevel);
            OnLevelUp?.Invoke(currentLevel);
        }
    }
}