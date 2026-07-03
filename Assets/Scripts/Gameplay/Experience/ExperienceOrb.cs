using UnityEngine;

namespace TopDownRoguelike.Gameplay.Experience
{
    public class ExperienceOrb : MonoBehaviour
    {
        [SerializeField] private int experienceAmount = 1;

        private ExperienceOrbPool pool;

        public int ExperienceAmount => experienceAmount;

        public void SetPool(ExperienceOrbPool experienceOrbPool)
        {
            pool = experienceOrbPool;
        }

        public void Initialize(int amount)
        {
            experienceAmount = amount;
        }

        public void Collect()
        {
            if (pool != null)
            {
                pool.ReleaseOrb(this);
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
}