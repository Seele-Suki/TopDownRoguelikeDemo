using TopDownRoguelike.Gameplay.Characters;
using UnityEngine;
using UnityEngine.UI;

namespace TopDownRoguelike.Gameplay.UI
{
    public class ShotgunCooldownView : MonoBehaviour
    {
        [SerializeField] private ShotgunSkill shotgunSkill;
        [SerializeField] private Image cooldownMask;

        private void Awake()
        {
            if (shotgunSkill == null || cooldownMask == null)
            {
                Debug.LogError(
                    "ShotgunCooldownView: References are missing.");

                enabled = false;
                return;
            }

            cooldownMask.fillAmount = 0f;
            cooldownMask.enabled = false;
        }

        private void Update()
        {
            float cooldownProgress =
                shotgunSkill.CooldownNormalized;

            cooldownMask.fillAmount = cooldownProgress;

            cooldownMask.enabled =
                cooldownProgress > 0.001f;
        }
    }
}