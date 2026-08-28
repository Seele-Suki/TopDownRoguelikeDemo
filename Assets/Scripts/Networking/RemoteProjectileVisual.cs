using System;
using UnityEngine;

namespace TopDownRoguelike.Gameplay.Networking
{
    public sealed class RemoteProjectileVisual
        : MonoBehaviour
    {
        [SerializeField]
        private float speed = 12f;

        [SerializeField]
        private float lifeTime = 2f;

        private Vector2 direction;
        private float remainingLifeTime;
        private bool initialized;

        public Vector2 Direction =>
            direction;

        public bool IsInitialized =>
            initialized;

        public void Initialize(
            Vector2 origin,
            Vector2 newDirection)
        {
            if (newDirection.sqrMagnitude <
                0.0001f)
            {
                throw new ArgumentException(
                    "Remote projectile direction " +
                    "cannot be zero.",
                    nameof(newDirection));
            }

            if (float.IsNaN(origin.x) ||
                float.IsInfinity(origin.x) ||
                float.IsNaN(origin.y) ||
                float.IsInfinity(origin.y))
            {
                throw new ArgumentException(
                    "Remote projectile origin " +
                    "must be finite.",
                    nameof(origin));
            }

            if (float.IsNaN(newDirection.x) ||
                float.IsInfinity(newDirection.x) ||
                float.IsNaN(newDirection.y) ||
                float.IsInfinity(newDirection.y))
            {
                throw new ArgumentException(
                    "Remote projectile direction " +
                    "must be finite.",
                    nameof(newDirection));
            }

            if (speed <= 0f)
            {
                throw new InvalidOperationException(
                    "Remote projectile speed must be positive.");
            }

            if (lifeTime <= 0f)
            {
                throw new InvalidOperationException(
                    "Remote projectile lifetime must be positive.");
            }

            transform.position =
                new Vector3(
                    origin.x,
                    origin.y,
                    transform.position.z);

            direction =
                newDirection.normalized;

            remainingLifeTime =
                lifeTime;

            initialized =
                true;

            gameObject.SetActive(
                true);
        }

        public void Tick(
            float deltaTime)
        {
            if (!initialized ||
                deltaTime <= 0f)
            {
                return;
            }

            transform.position +=
                (Vector3)(
                    direction *
                    speed *
                    deltaTime);

            remainingLifeTime -=
                deltaTime;

            if (remainingLifeTime <= 0f)
            {
                initialized =
                    false;

                gameObject.SetActive(
                    false);
            }
        }

        private void Update()
        {
            Tick(
                Time.deltaTime);
        }

        private void OnDisable()
        {
            initialized =
                false;

            direction =
                Vector2.zero;

            remainingLifeTime =
                0f;
        }
    }
}