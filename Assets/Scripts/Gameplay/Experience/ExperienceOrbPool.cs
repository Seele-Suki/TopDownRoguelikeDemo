using System.Collections.Generic;
using UnityEngine;

namespace TopDownRoguelike.Gameplay.Experience
{
    public class ExperienceOrbPool : MonoBehaviour
    {
        public static ExperienceOrbPool Instance { get; private set; }

        [SerializeField] private ExperienceOrb experienceOrbPrefab;
        [SerializeField] private int initialSize = 30;

        private readonly Queue<ExperienceOrb> availableOrbs = new Queue<ExperienceOrb>();

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
            ExperienceOrb orb = availableOrbs.Count > 0
                ? availableOrbs.Dequeue()
                : CreateOrb();

            orb.transform.position = position;
            orb.Initialize(experienceAmount);
            orb.gameObject.SetActive(true);

            return orb;
        }

        public void ReleaseOrb(ExperienceOrb orb)
        {
            orb.gameObject.SetActive(false);
            availableOrbs.Enqueue(orb);
        }

        private ExperienceOrb CreateOrb()
        {
            ExperienceOrb orb = Instantiate(experienceOrbPrefab, transform);
            orb.SetPool(this);
            orb.gameObject.SetActive(false);

            return orb;
        }
    }
}