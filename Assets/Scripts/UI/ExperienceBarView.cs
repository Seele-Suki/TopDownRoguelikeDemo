using System.Collections;
using System.Collections.Generic;
using TMPro;
using TopDownRoguelike.Gameplay.Experience;
using UnityEngine;
using UnityEngine.UI;

namespace TopDownRoguelike.Gameplay.UI
{
    public class ExperienceBarView : MonoBehaviour
    {
        [SerializeField] private LevelSystem levelSystem;
        [SerializeField] private Slider experienceSlider;
        [SerializeField] private TMP_Text levelText;

        private void Awake()
        {
            if (experienceSlider == null)
            {
                experienceSlider = GetComponent<Slider>();
            }
        }

        private void Start()
        {
            UpdateExperienceBar(levelSystem.CurrentExperience, levelSystem.ExperienceToNextLevel);
            UpdateLevelText(levelSystem.CurrentLevel);
        }

        private void OnEnable()
        {
            if (levelSystem == null)
            {
                return;
            }

            levelSystem.OnExperienceChanged += UpdateExperienceBar;
            levelSystem.OnLevelChanged += UpdateLevelText;
            levelSystem.OnLevelUp += UpdateLevelText;
        }

        private void OnDisable()
        {
            if (levelSystem == null)
            {
                return;
            }

            levelSystem.OnExperienceChanged -= UpdateExperienceBar;
            levelSystem.OnLevelChanged -= UpdateLevelText;
            levelSystem.OnLevelUp -= UpdateLevelText;
        }

        private void UpdateExperienceBar(int currentExperience, int experienceToNextLevel)
        {
            if (experienceSlider == null || experienceToNextLevel <= 0)
            {
                return;
            }

            experienceSlider.value = (float)currentExperience / experienceToNextLevel;
        }

        private void UpdateLevelText(int level)
        {
            if (levelText == null)
            {
                return;
            }

            levelText.text = $"Lv.{level}";
        }
    }
}