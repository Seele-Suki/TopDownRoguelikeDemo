using UnityEngine;

namespace TopDownRoguelike.Gameplay.Characters
{
    public sealed class LocalPlayerInputSource
        : MonoBehaviour,
          IPlayerInputSource
    {
        [SerializeField]
        private Camera mainCamera;

        [SerializeField]
        private KeyCode dashKey = KeyCode.Space;

        public Vector2 MoveDirection
        {
            get
            {
                float horizontal =
                    Input.GetAxisRaw("Horizontal");

                float vertical =
                    Input.GetAxisRaw("Vertical");

                return new Vector2(
                    horizontal,
                    vertical).normalized;
            }
        }

        public Vector2 AimDirection
        {
            get
            {
                Camera inputCamera =
                    mainCamera != null
                        ? mainCamera
                        : Camera.main;

                if (inputCamera == null)
                {
                    return Vector2.zero;
                }

                Vector3 mouseWorldPosition =
                    inputCamera.ScreenToWorldPoint(
                        Input.mousePosition);

                Vector2 direction =
                    (Vector2)mouseWorldPosition -
                    (Vector2)transform.position;

                if (direction.sqrMagnitude < 0.0001f)
                {
                    return Vector2.zero;
                }

                return direction.normalized;
            }
        }

        public bool IsFireHeld
        {
            get
            {
                return Input.GetMouseButton(0);
            }
        }

        public uint DashRequestSequence
        {
            get;
            private set;
        }

        private void Update()
        {
            if (Input.GetKeyDown(dashKey))
            {
                RegisterDashRequest();
            }
        }

        private void RegisterDashRequest()
        {
            unchecked
            {
                DashRequestSequence++;
            }
        }

        private void Awake()
        {
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }
        }
    }
}