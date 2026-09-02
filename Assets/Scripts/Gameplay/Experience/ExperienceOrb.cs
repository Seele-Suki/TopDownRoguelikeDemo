using UnityEngine;
using TopDownRoguelike.Infrastructure;

namespace TopDownRoguelike.Gameplay.Experience
{
    public class ExperienceOrb : MonoBehaviour
    {
        [SerializeField] private int experienceAmount = 1;

        private ExperienceOrbPool pool;

        private bool isCollected;

        public int ExperienceAmount => experienceAmount;

        public bool IsCollected => isCollected;

        public void SetPool(ExperienceOrbPool experienceOrbPool)
        {
            pool = experienceOrbPool;
        }

        public void Initialize(int amount)
        {
            experienceAmount = amount;
            isCollected = false;
        }

        public bool TryCollect()
        {
            if (GameSession.IsClient)
            {
                return false;
            }

            if (isCollected)
            {
                return false;
            }

            isCollected = true;

            if (pool != null)
            {
                pool.ReleaseOrb(this);
            }
            else
            {
                if (Application.isPlaying)
                {
                    Destroy(gameObject);
                }
                else
                {
                    DestroyImmediate(gameObject);
                }
            }

            return true;
        }

        public void Collect()
        {
            TryCollect();
        }
    }
}
