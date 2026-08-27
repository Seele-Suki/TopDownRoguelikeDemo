using TMPro;
using TopDownRoguelike.Gameplay.Characters;
using UnityEngine;
using UnityEngine.UI;

namespace TopDownRoguelike.Gameplay.UI
{
    public sealed class HealthBarView : MonoBehaviour
    {
        [SerializeField]
        private Slider healthSlider;

        [SerializeField]
        private TMP_Text healthText;

        private PlayerHealth boundPlayerHealth;

        private void Awake()
        {
            if (healthSlider == null ||
                healthText == null)
            {
                Debug.LogError(
                    "HealthBarView: " +
                    "Required references are missing.");

                enabled = false;
                return;
            }

            healthSlider.minValue = 0f;
            healthSlider.maxValue = 1f;
            healthSlider.value = 1f;
            healthSlider.interactable = false;
        }

        public void Bind(
            PlayerHealth playerHealth)
        {
            Unbind();

            if (playerHealth == null)
            {
                return;
            }

            boundPlayerHealth = playerHealth;

            boundPlayerHealth.OnHealthChanged +=
                HandleHealthChanged;

            Refresh(
                boundPlayerHealth.CurrentHealth,
                boundPlayerHealth.MaxHealth);
        }

        public void Unbind()
        {
            if (boundPlayerHealth == null)
            {
                return;
            }

            boundPlayerHealth.OnHealthChanged -=
                HandleHealthChanged;

            boundPlayerHealth = null;
        }

        private void HandleHealthChanged(
            int currentHealth,
            int maxHealth)
        {
            Refresh(
                currentHealth,
                maxHealth);
        }

        private void Refresh(
            int currentHealth,
            int maxHealth)
        {
            healthSlider.value =
                maxHealth > 0
                    ? (float)currentHealth / maxHealth
                    : 0f;

            healthText.text =
                $"{currentHealth} / {maxHealth}";
        }

        private void OnDestroy()
        {
            Unbind();
        }
    }
}