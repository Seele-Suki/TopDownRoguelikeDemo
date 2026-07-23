using System.Collections;
using UnityEngine;

public class DashSkill : MonoBehaviour
{
    [Header("冲刺参数")]
    [SerializeField] private float dashSpeed = 12f;
    [SerializeField] private float dashDuration = 0.15f;
    [SerializeField] private KeyCode dashKey = KeyCode.Space;

    private Rigidbody2D rb;
    private PlayerController playerController;

    public bool IsDashing { get; private set; }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        playerController = GetComponent<PlayerController>();

        if (rb == null)
        {
            Debug.LogError("DashSkill: 玩家对象缺少 Rigidbody2D。");
        }

        if (playerController == null)
        {
            Debug.LogError("DashSkill: 玩家对象缺少 PlayerController。");
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(dashKey) && !IsDashing)
        {
            StartCoroutine(DashCoroutine());
        }
    }

    private IEnumerator DashCoroutine()
    {
        Vector2 dashDirection = GetDashDirection();

        if (dashDirection.sqrMagnitude < 0.01f)
        {
            yield break;
        }

        IsDashing = true;

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

        rb.velocity = Vector2.zero;

        if (playerController != null)
        {
            playerController.enabled = true;
        }

        IsDashing = false;
    }

    private Vector2 GetDashDirection()
    {
        Vector2 inputDirection = new Vector2(
            Input.GetAxisRaw("Horizontal"),
            Input.GetAxisRaw("Vertical")
        );

        // 优先使用键盘移动方向。
        if (inputDirection.sqrMagnitude > 0.01f)
        {
            return inputDirection.normalized;
        }

        // 没有移动输入时，朝鼠标方向冲刺。
        if (Camera.main != null)
        {
            Vector3 mouseWorldPosition =
                Camera.main.ScreenToWorldPoint(Input.mousePosition);

            Vector2 mouseDirection =
                (Vector2)mouseWorldPosition - rb.position;

            if (mouseDirection.sqrMagnitude > 0.01f)
            {
                return mouseDirection.normalized;
            }
        }

        return Vector2.zero;
    }
}