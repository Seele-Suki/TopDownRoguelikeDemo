using TopDownRoguelike.Gameplay.Characters;
using TopDownRoguelike.Gameplay.Networking;
using TopDownRoguelike.Networking.Protocol;
using UnityEngine;
using UnityEngine.UI;

namespace TopDownRoguelike.Gameplay.UI
{
    public class ShotgunCooldownView
        : MonoBehaviour
    {
        [SerializeField]
        private ShotgunSkill shotgunSkill;

        [SerializeField]
        private Image cooldownMask;

        private NetworkGameBootstrap
            networkGameBootstrap;

        private float
            authoritativeCooldownRemaining;

        private float
            authoritativeCooldownDuration;

        private bool
            hasAuthoritativeCooldown;

        private void Awake()
        {
            if (shotgunSkill == null ||
                cooldownMask == null)
            {
                Debug.LogError(
                    "ShotgunCooldownView: References " +
                    "are missing.");

                enabled = false;
                return;
            }

            cooldownMask.fillAmount =
                0f;

            cooldownMask.enabled =
                false;

            authoritativeCooldownRemaining =
                0f;

            authoritativeCooldownDuration =
                0f;

            hasAuthoritativeCooldown =
                false;
        }

        private void Start()
        {
            networkGameBootstrap =
                FindFirstObjectByType<
                    NetworkGameBootstrap>();

            if (networkGameBootstrap == null)
            {
                return;
            }

            networkGameBootstrap
                .PlayerShotgunEventReceived +=
                HandlePlayerShotgunEvent;
        }

        private void Update()
        {
            float cooldownProgress;

            if (hasAuthoritativeCooldown)
            {
                cooldownProgress =
                    authoritativeCooldownDuration > 0f
                        ? Mathf.Clamp01(
                            authoritativeCooldownRemaining /
                            authoritativeCooldownDuration)
                        : 0f;

                authoritativeCooldownRemaining =
                    Mathf.Max(
                        0f,
                        authoritativeCooldownRemaining -
                        Time.deltaTime);

                if (authoritativeCooldownRemaining <= 0f)
                {
                    hasAuthoritativeCooldown =
                        false;
                }
            }
            else
            {
                cooldownProgress =
                    shotgunSkill.CooldownNormalized;
            }

            cooldownMask.fillAmount =
                cooldownProgress;

            cooldownMask.enabled =
                cooldownProgress > 0.001f;
        }

        public void ApplyAuthoritativeCooldown(
            float effectiveCooldown)
        {
            if (float.IsNaN(effectiveCooldown) ||
                float.IsInfinity(effectiveCooldown) ||
                effectiveCooldown < 0f)
            {
                return;
            }

            authoritativeCooldownDuration =
                effectiveCooldown;

            authoritativeCooldownRemaining =
                effectiveCooldown;

            hasAuthoritativeCooldown =
                true;
        }

        private void HandlePlayerShotgunEvent(
            uint playerId,
            PlayerShotgunEvent shotgunEvent)
        {
            if (networkGameBootstrap == null ||
                shotgunSkill == null ||
                shotgunEvent == null)
            {
                return;
            }

            if (shotgunEvent.PlayerId != playerId)
            {
                return;
            }

            if (networkGameBootstrap.Registry == null ||
                !networkGameBootstrap.Registry.TryGetPlayer(
                    playerId,
                    out GameObject player) ||
                player == null)
            {
                return;
            }

            if (player != shotgunSkill.gameObject)
            {
                return;
            }

            ApplyAuthoritativeCooldown(
                shotgunEvent.EffectiveCooldown);
        }

        private void OnDestroy()
        {
            if (networkGameBootstrap != null)
            {
                networkGameBootstrap
                    .PlayerShotgunEventReceived -=
                    HandlePlayerShotgunEvent;

                networkGameBootstrap =
                    null;
            }
        }
    }
}