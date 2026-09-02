using System.Collections.Generic;
using System;
using TopDownRoguelike.Infrastructure;
using TopDownRoguelike.Networking.Gameplay;
using TopDownRoguelike.Networking.Protocol;
using UnityEngine;

namespace TopDownRoguelike.Gameplay.Experience
{
    public class ExperienceOrbPool : MonoBehaviour
    {
        public static ExperienceOrbPool Instance { get; private set; }

        [SerializeField] private ExperienceOrb experienceOrbPrefab;
        [SerializeField] private int initialSize = 30;

        private readonly Queue<ExperienceOrb> availableOrbs = new Queue<ExperienceOrb>();

        private uint nextNetworkEntityId = 0x40000000u;

        public event Action<ExperienceOrb> OrbSpawned;
        public event Action<ExperienceOrb> OrbCollected;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("There is more than one ExperienceOrbPool in the scene.");
                return;
            }

            Instance = this;

            for (int i = 0; i < initialSize; i++)
            {
                availableOrbs.Enqueue(CreateOrb());
            }
        }

        public ExperienceOrb GetOrb(Vector3 position, int experienceAmount)
        {
            if (GameSession.IsClient)
            {
                return null;
            }

            ExperienceOrb orb = availableOrbs.Count > 0
                ? availableOrbs.Dequeue()
                : CreateOrb();

            if (!EnsureNetworkEntityId(orb))
            {
                availableOrbs.Enqueue(orb);
                return null;
            }

            orb.transform.position = position;
            orb.Initialize(experienceAmount);
            orb.gameObject.SetActive(true);

            OrbSpawned?.Invoke(orb);

            return orb;
        }

        public void ReleaseOrb(ExperienceOrb orb)
        {
            if (orb == null || !orb.IsCollected)
            {
                return;
            }

            OrbCollected?.Invoke(orb);
            orb.gameObject.SetActive(false);
            availableOrbs.Enqueue(orb);
        }

        public IReadOnlyList<ExperienceOrb> EnumerateActiveOrbs()
        {
            var activeOrbs = new List<ExperienceOrb>();
            foreach (Transform child in transform)
            {
                if (child.gameObject.activeInHierarchy &&
                    child.TryGetComponent(out ExperienceOrb orb))
                {
                    activeOrbs.Add(orb);
                }
            }

            return activeOrbs;
        }

        public GameObject CreateClientOrb(WorldEntityRecord record)
        {
            if (!GameSession.IsClient || record == null ||
                record.EntityType != NetworkEntityType.ExperienceOrb)
            {
                return null;
            }

            ExperienceOrb orb = Instantiate(experienceOrbPrefab, transform);
            orb.Initialize(record.ExperienceAmount);
            orb.gameObject.SetActive(true);
            return orb.gameObject;
        }

        private ExperienceOrb CreateOrb()
        {
            ExperienceOrb orb = Instantiate(experienceOrbPrefab, transform);
            orb.SetPool(this);
            orb.gameObject.SetActive(false);

            return orb;
        }

        private bool EnsureNetworkEntityId(ExperienceOrb orb)
        {
            if (orb == null)
            {
                return false;
            }

            NetworkEntityId identifier =
                orb.GetComponent<NetworkEntityId>();

            if (identifier == null)
            {
                identifier =
                    orb.gameObject.AddComponent<NetworkEntityId>();
            }

            if (identifier.IsAssigned)
            {
                return identifier.EntityType ==
                    NetworkEntityType.ExperienceOrb;
            }

            return identifier.TryAssign(
                nextNetworkEntityId++,
                NetworkEntityType.ExperienceOrb);
        }
    }
}
