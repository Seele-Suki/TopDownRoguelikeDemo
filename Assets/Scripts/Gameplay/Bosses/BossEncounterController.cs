using System.Collections;
using TopDownRoguelike.Gameplay.Core;
using TopDownRoguelike.Gameplay.Enemies;
using TopDownRoguelike.Gameplay.UI;
using UnityEngine;

namespace TopDownRoguelike.Gameplay.Bosses
{
    public class BossEncounterController : MonoBehaviour
    {
        [Header("Scene References")]
        [SerializeField] private GameManager gameManager;
        [SerializeField] private EnemySpawner enemySpawner;
        [SerializeField] private Transform player;
        [SerializeField] private BossHealthView bossHealthView;

        [Header("Boss")]
        [SerializeField] private GameObject bossPrefab;
        [SerializeField, Min(0f)]
        private float transitionDelay = 2f;

        [SerializeField, Min(1f)]
        private float spawnDistance = 6f;

        [Header("Runtime Debug")]
        [SerializeField] private bool encounterStarted;
        [SerializeField] private GameObject currentBoss;

        private BossHealth currentBossHealth;

        private void Awake()
        {
            if (gameManager == null ||
                enemySpawner == null ||
                player == null ||
                bossPrefab == null ||
                bossHealthView == null)
            {
                Debug.LogError(
                    "BossEncounterController: " +
                    "Required references are missing.");

                enabled = false;
            }
        }

        private void OnEnable()
        {
            if (gameManager != null)
            {
                gameManager.OnBossTransitionRequested +=
                    HandleBossTransitionRequested;
            }
        }

        private void OnDisable()
        {
            if (gameManager != null)
            {
                gameManager.OnBossTransitionRequested -=
                    HandleBossTransitionRequested;
            }

            UnsubscribeFromBoss();
        }

        private void HandleBossTransitionRequested()
        {
            if (encounterStarted)
            {
                return;
            }

            encounterStarted = true;

            StartCoroutine(
                BeginBossEncounterCoroutine());
        }

        private IEnumerator BeginBossEncounterCoroutine()
        {
            Debug.Log("Boss transition started.");

            enemySpawner.ClearSpawnedEnemies();

            yield return new WaitForSeconds(
                transitionDelay);

            if (gameManager.CurrentState !=
                GameState.BossTransition)
            {
                yield break;
            }

            Vector3 spawnPosition =
                player.position +
                Vector3.up * spawnDistance;

            spawnPosition.z = 0f;

            currentBoss = Instantiate(
                bossPrefab,
                spawnPosition,
                Quaternion.identity);

            if (!currentBoss.TryGetComponent(
                    out currentBossHealth))
            {
                Debug.LogError(
                    "Spawned Boss is missing BossHealth.");

                Destroy(currentBoss);
                currentBoss = null;
                yield break;
            }

            currentBossHealth.OnDied += HandleBossDied;

            bossHealthView.Bind(currentBossHealth);

            gameManager.StartBossBattle();

            Debug.Log("Boss battle started.");
        }

        private void HandleBossDied()
        {
            UnsubscribeFromBoss();

            currentBoss = null;

            gameManager.NotifyVictory();

            Debug.Log(
                "Boss defeated. Victory requested.");
        }

        private void UnsubscribeFromBoss()
        {
            if (currentBossHealth != null)
            {
                currentBossHealth.OnDied -=
                    HandleBossDied;

                currentBossHealth = null;
            }
        }
    }
}