using TMPro;
using TopDownRoguelike.Gameplay.Bosses;
using UnityEngine;
using UnityEngine.UI;

namespace TopDownRoguelike.Gameplay.UI
{
    public class BossHealthView : MonoBehaviour
    {
        [SerializeField] private Slider healthSlider;
        [SerializeField] private TMP_Text healthText;

        private BossHealth currentBossHealth;

        private void Awake()
        {
            if (healthSlider == null ||
                healthText == null)
            {
                Debug.LogError(
                    "BossHealthView: " +
                    "Required references are missing.");

                enabled = false;
                return;
            }

            healthSlider.minValue = 0f;
            healthSlider.maxValue = 1f;
            healthSlider.value = 1f;
            healthSlider.interactable = false;

            gameObject.SetActive(false);
        }

        public void Bind(BossHealth bossHealth)
        {
            Unbind();

            if (bossHealth == null)
            {
                return;
            }

            currentBossHealth = bossHealth;

            currentBossHealth.OnHealthChanged +=
                HandleHealthChanged;

            currentBossHealth.OnDied +=
                HandleBossDied;

            gameObject.SetActive(true);

            Refresh(
                currentBossHealth.CurrentHealth,
                currentBossHealth.MaxHealth);
        }

        private void HandleHealthChanged(
            int currentHealth,
            int maxHealth)
        {
            Refresh(currentHealth, maxHealth);
        }

        private void HandleBossDied()
        {
            if (currentBossHealth != null)
            {
                Refresh(
                    0,
                    currentBossHealth.MaxHealth);
            }

            Unbind();
            gameObject.SetActive(false);
        }

        public void Hide()
        {
            Unbind();
            gameObject.SetActive(false);
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
                $"BOSS  {currentHealth} / {maxHealth}";
        }

        private void Unbind()
        {
            if (currentBossHealth == null)
            {
                return;
            }

            currentBossHealth.OnHealthChanged -=
                HandleHealthChanged;

            currentBossHealth.OnDied -=
                HandleBossDied;

            currentBossHealth = null;
        }

        private void OnDestroy()
        {
            Unbind();
        }
    }
}
