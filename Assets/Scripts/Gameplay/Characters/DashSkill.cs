using System;
using System.Collections;
using TopDownRoguelike.Gameplay.Characters;
using TopDownRoguelike.Gameplay.Skills;
using UnityEngine;

public class DashSkill : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private DashData dashData;

    [Header("Runtime Debug")]
    [SerializeField] private float cooldownRemaining;

    private float dashSpeed;
    private float dashDuration;
    private float dashCooldown;
    private PlayerHealth playerHealth;
    private IPlayerInputSource inputSource;
    private uint lastProcessedDashRequestSequence;

    public bool IsDashing { get; private set; }
    public bool IsReady => !IsDashing && cooldownRemaining <= 0f;
    public float CooldownRemaining => cooldownRemaining;

    public float CooldownNormalized =>
        dashCooldown > 0f ? cooldownRemaining / dashCooldown : 0f;

    private Rigidbody2D rb;
    private PlayerController playerController;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        playerController = GetComponent<PlayerController>();
        playerHealth = GetComponent<PlayerHealth>();
        inputSource = GetComponent<IPlayerInputSource>();

        if (inputSource == null)
        {
            Debug.LogError(
                "DashSkill requires an " +
                "IPlayerInputSource component.",
                this);

            enabled = false;
            return;
        }

        lastProcessedDashRequestSequence =
            inputSource.DashRequestSequence;

        if (playerHealth == null)
        {
            Debug.LogError("DashSkill: Player is missing PlayerHealth.");
            enabled = false;
            return;
        }

        if (rb == null)
        {
            Debug.LogError("DashSkill: 玩家对象缺少 Rigidbody2D。");
        }

        if (playerController == null)
        {
            Debug.LogError("DashSkill: 玩家对象缺少 PlayerController。");
        }

        if (dashData == null)
        {
            Debug.LogError("DashSkill: DashData is not assigned.");
            enabled = false;
            return;
        }

        dashSpeed = dashData.DashSpeed;
        dashDuration = dashData.DashDuration;
        dashCooldown = dashData.Cooldown;
    }

    private void Update()
    {
        bool hasDashRequest =
            TryConsumeDashRequest();

        if (Time.timeScale <= 0f ||
            (playerHealth != null && playerHealth.IsDead))
        {
            return;
        }

        if (cooldownRemaining > 0f)
        {
            cooldownRemaining =
                Mathf.Max(
                    0f,
                    cooldownRemaining - Time.deltaTime);
        }

        if (hasDashRequest && IsReady)
        {
            StartCoroutine(
                DashCoroutine());
        }
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

        lastProcessedDashRequestSequence =
            inputSource.DashRequestSequence;

        enabled =
            true;
    }

    private bool TryConsumeDashRequest()
    {
        if (inputSource == null)
        {
            return false;
        }

        uint currentSequence =
            inputSource.DashRequestSequence;

        uint difference =
            unchecked(
                currentSequence -
                lastProcessedDashRequestSequence);

        if (difference == 0u ||
            difference >= 0x80000000u)
        {
            return false;
        }

        lastProcessedDashRequestSequence =
            currentSequence;

        return true;
    }

    private IEnumerator DashCoroutine()
    {
        Vector2 dashDirection = GetDashDirection();

        if (dashDirection.sqrMagnitude < 0.01f)
        {
            yield break;
        }

        IsDashing = true;
        cooldownRemaining = dashCooldown;
        playerHealth.SetInvulnerable(true);

        // 暂时关闭普通移动，避免移动脚本覆盖冲刺速度。
        if (playerController != null)
        {
            playerController.enabled = false;
        }

        float elapsedTime = 0f;

        while (elapsedTime < dashDuration)
        {
            rb.velocity = dashDirection * dashSpeed;
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        EndDash();
    }

    private void EndDash()
    {
        rb.velocity = Vector2.zero;

        if (playerController != null)
        {
            playerController.enabled = true;
        }

        if (playerHealth != null)
        {
            playerHealth.SetInvulnerable(false);
        }

        IsDashing = false;
    }

    private void OnDisable()
    {
        if (IsDashing)
        {
            EndDash();
        }
    }

    public void ReduceCooldown(float amount)
    {
        if (amount <= 0f)
        {
            return;
        }

        dashCooldown = Mathf.Max(0.3f, dashCooldown - amount);
        cooldownRemaining = Mathf.Min(cooldownRemaining, dashCooldown);

        Debug.Log($"Dash cooldown: {dashCooldown:F2}s");
    }

    public void AddDashDuration(float amount)
    {
        if (amount <= 0f)
        {
            return;
        }

        dashDuration = Mathf.Min(0.35f, dashDuration + amount);

        Debug.Log($"Dash duration: {dashDuration:F2}s");
    }

    private Vector2 GetDashDirection()
    {
        if (inputSource == null)
        {
            return Vector2.zero;
        }

        Vector2 movement =
            inputSource.MoveDirection;

        if (movement.sqrMagnitude > 0.01f)
        {
            return movement.normalized;
        }

        Vector2 aim =
            inputSource.AimDirection;

        if (aim.sqrMagnitude > 0.01f)
        {
            return aim.normalized;
        }

        return Vector2.zero;
    }
}