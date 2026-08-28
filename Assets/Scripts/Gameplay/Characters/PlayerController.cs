using System;
using TopDownRoguelike.Gameplay.Characters;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField]
    private float moveSpeed = 5f;

    private Rigidbody2D rb;
    private Vector2 moveInput;
    private Vector2 aimDirection;
    private PlayerHealth playerHealth;
    private IPlayerInputSource inputSource;

    public Vector2 AimDirection
    {
        get
        {
            return aimDirection;
        }
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        playerHealth =
            GetComponent<PlayerHealth>();

        inputSource =
            GetComponent<IPlayerInputSource>();

        if (inputSource == null)
        {
            if (rb != null)
            {
                rb.velocity =
                    Vector2.zero;
            }

            Debug.LogError(
                "PlayerController requires an " +
                "IPlayerInputSource component.",
                this);

            enabled =
                false;
        }
    }

    private void Update()
    {
        if (playerHealth != null &&
            playerHealth.IsDead)
        {
            return;
        }

        moveInput =
            inputSource.MoveDirection;

        aimDirection =
            inputSource.AimDirection;
    }

    private void FixedUpdate()
    {
        if (playerHealth != null &&
            playerHealth.IsDead)
        {
            return;
        }

        Move();
        RotateToAimDirection();
    }

    public void SetInputSource(
        IPlayerInputSource newInputSource)
    {
        if (newInputSource == null)
        {
            throw new ArgumentNullException(
                nameof(newInputSource));
        }

        inputSource =
            newInputSource;

        enabled =
            true;
    }

    public void AddMoveSpeed(float amount)
    {
        moveSpeed += amount;

        moveSpeed =
            Mathf.Max(
                0f,
                moveSpeed);
    }

    private void Move()
    {
        rb.velocity =
            moveInput * moveSpeed;
    }

    private void RotateToAimDirection()
    {
        if (aimDirection.sqrMagnitude <
            0.0001f)
        {
            return;
        }

        float angle =
            Mathf.Atan2(
                aimDirection.y,
                aimDirection.x) *
            Mathf.Rad2Deg;

        rb.rotation =
            angle;
    }
}