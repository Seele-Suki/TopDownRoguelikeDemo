using System.Collections;
using TopDownRoguelike.Gameplay.Core;
using TopDownRoguelike.Gameplay.Enemies;
using TopDownRoguelike.Gameplay.UI;
using TopDownRoguelike.Networking.Gameplay;
using TopDownRoguelike.Networking.Protocol;
using TopDownRoguelike.Infrastructure;
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

        private const uint BossEntityId =
            0x20000001u;

        [Header("Runtime Debug")]
        [SerializeField] private bool encounterStarted;
        [SerializeField] private GameObject currentBoss;

        private BossHealth currentBossHealth;

        public event System.Action<GameObject> BossSpawned;

        public GameObject CurrentBoss =>
            currentBoss;

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
            if (!GameSession.IsHost)
            {
                return;
            }

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

            NetworkEntityId identifier =
                currentBoss.GetComponent<NetworkEntityId>();

            if (identifier == null)
            {
                identifier =
                    currentBoss.AddComponent<NetworkEntityId>();
            }

            if (!identifier.IsAssigned &&
                !identifier.TryAssign(
                    BossEntityId,
                    NetworkEntityType.Boss))
            {
                Destroy(currentBoss);
                currentBoss = null;
                yield break;
            }

            if (identifier.IsAssigned &&
                (identifier.EntityId != BossEntityId ||
                 identifier.EntityType != NetworkEntityType.Boss))
            {
                Destroy(currentBoss);
                currentBoss = null;
                yield break;
            }

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

            BossSpawned?.Invoke(currentBoss);

            bossHealthView.Bind(currentBossHealth);

            gameManager.StartBossBattle();

            Debug.Log("Boss battle started.");
        }

        public GameObject CreateClientBoss(
            WorldEntityRecord record)
        {
            if (!GameSession.IsClient ||
                record == null ||
                record.EntityType != NetworkEntityType.Boss ||
                record.EntityId == 0u ||
                bossPrefab == null)
            {
                return null;
            }

            GameObject clientBoss =
                Instantiate(
                    bossPrefab,
                    new Vector3(
                        record.PositionX,
                        record.PositionY,
                        0f),
                    Quaternion.Euler(
                        0f,
                        0f,
                        record.RotationDegrees));

            NetworkEntityId identifier =
                clientBoss.GetComponent<NetworkEntityId>();

            if (identifier == null)
            {
                identifier =
                    clientBoss.AddComponent<NetworkEntityId>();
            }

            if (!identifier.IsAssigned &&
                !identifier.TryAssign(
                    record.EntityId,
                    NetworkEntityType.Boss))
            {
                Destroy(clientBoss);
                return null;
            }

            if (identifier.EntityId != record.EntityId ||
                identifier.EntityType != NetworkEntityType.Boss)
            {
                Destroy(clientBoss);
                return null;
            }

            if (clientBoss.TryGetComponent(
                    out BossController controller))
            {
                controller.enabled = false;
            }

            currentBoss = clientBoss;

            Debug.Log(
                $"BossEncounterController: created client Boss " +
                $"entity={record.EntityId} position=({record.PositionX:F2}," +
                $"{record.PositionY:F2})");

            if (clientBoss.TryGetComponent(
                    out BossHealth health))
            {
                currentBossHealth = health;
                bossHealthView?.Bind(health);
            }

            return clientBoss;
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

            Debug.Log(
                "Boss defeated. Scheduling victory after network " +
                "death event dispatch.");
            StartCoroutine(NotifyVictoryNextFrame());
        }

        private IEnumerator NotifyVictoryNextFrame()
        {
            yield return null;
            gameManager.NotifyVictory();
            Debug.Log("Victory requested after Boss death dispatch.");
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
