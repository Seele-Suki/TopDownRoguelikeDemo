using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private Camera mainCamera;

    private Rigidbody2D rb;
    private Vector2 moveInput;
    private Vector2 mouseWorldPosition;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
    }

    private void Update()
    {
        ReadMovementInput();
        ReadMousePosition();
    }

    private void FixedUpdate()
    {
        Move();
        RotateToMouse();
    }

    private void ReadMovementInput()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        moveInput = new Vector2(horizontal, vertical).normalized;
    }

    private void ReadMousePosition()
    {
        Vector3 mouseScreenPosition = Input.mousePosition;
        Vector3 worldPosition = mainCamera.ScreenToWorldPoint(mouseScreenPosition);

        mouseWorldPosition = worldPosition;
    }

    private void Move()
    {
        rb.velocity = moveInput * moveSpeed;
    }

    private void RotateToMouse()
    {
        Vector2 aimDirection = mouseWorldPosition - rb.position;
        float angle = Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg;

        rb.rotation = angle;
    }
}