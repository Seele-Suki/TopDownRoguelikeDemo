using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TopDownRoguelike.Gameplay.UI
{
    public class SkillCooldownView : MonoBehaviour
    {
        [SerializeField] private DashSkill dashSkill;
        [SerializeField] private Image cooldownMask;

        private void Awake()
        {
            if (dashSkill == null || cooldownMask == null)
            {
                Debug.LogError(
                    "SkillCooldownView: References are not assigned.");

                enabled = false;
                return;
            }

            cooldownMask.fillAmount = 0f;
            cooldownMask.enabled = false;
        }

        private void Update()
        {
            float cooldownProgress = dashSkill.CooldownNormalized;

            cooldownMask.fillAmount = cooldownProgress;
            cooldownMask.enabled = cooldownProgress > 0.001f;
        }
    }
}