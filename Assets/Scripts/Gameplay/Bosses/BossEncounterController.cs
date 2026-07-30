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

        [SerializeField] private SpriteRenderer mapBounds;

        [SerializeField, Min(0f)]
        private float spawnPadding = 2f;

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
                bossHealthView == null ||
                mapBounds == null)
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

            Vector3 spawnPosition = GetBossSpawnPosition();

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

        private Vector3 GetBossSpawnPosition()
        {
            Bounds bounds = mapBounds.bounds;

            float minX = bounds.min.x + spawnPadding;
            float maxX = bounds.max.x - spawnPadding;
            float minY = bounds.min.y + spawnPadding;
            float maxY = bounds.max.y - spawnPadding;

            Vector3 preferredPosition =
                player.position +
                Vector3.up * spawnDistance;

            preferredPosition.z = 0f;

            if (IsInsideSpawnBounds(
                    preferredPosition,
                    minX,
                    maxX,
                    minY,
                    maxY))
            {
                return preferredPosition;
            }

            Vector2 directionToCenter =
                (Vector2)bounds.center -
                (Vector2)player.position;

            if (directionToCenter.sqrMagnitude < 0.0001f)
            {
                directionToCenter = Vector2.up;
            }

            Vector3 fallbackPosition =
                player.position +
                (Vector3)(
                    directionToCenter.normalized *
                    spawnDistance);

            fallbackPosition.x =
                Mathf.Clamp(fallbackPosition.x, minX, maxX);

            fallbackPosition.y =
                Mathf.Clamp(fallbackPosition.y, minY, maxY);

            fallbackPosition.z = 0f;

            return fallbackPosition;
        }

        private static bool IsInsideSpawnBounds(
            Vector3 position,
            float minX,
            float maxX,
            float minY,
            float maxY)
        {
            return position.x >= minX &&
                   position.x <= maxX &&
                   position.y >= minY &&
                   position.y <= maxY;
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