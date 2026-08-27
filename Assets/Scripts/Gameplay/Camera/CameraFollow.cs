using System;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraFollow : MonoBehaviour
{
    [SerializeField]
    private Transform target;

    [SerializeField]
    private float followSpeed = 8f;

    [SerializeField]
    private SpriteRenderer mapBounds;

    private Camera cameraComponent;
    private Vector3 offset;
    private bool hasOffset;

    public Transform Target =>
        target;

    private void Awake()
    {
        cameraComponent =
            GetComponent<Camera>();

        if (target != null)
        {
            CaptureOffset();
        }
    }

    private void Start()
    {
        if (target == null)
        {
            Debug.LogError(
                "CameraFollow target is not assigned.");

            enabled = false;
            return;
        }

        if (!hasOffset)
        {
            CaptureOffset();
        }

        if (mapBounds == null)
        {
            Debug.LogWarning(
                "CameraFollow map bounds is not assigned. " +
                "Camera clamping will be disabled.");
        }
    }

    public void SetTarget(
        Transform newTarget)
    {
        if (newTarget == null)
        {
            throw new ArgumentNullException(
                nameof(newTarget));
        }

        target = newTarget;

        if (!hasOffset)
        {
            CaptureOffset();
        }

        enabled = true;
    }

    private void CaptureOffset()
    {
        offset =
            transform.position - target.position;

        hasOffset = true;
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        Vector3 targetPosition =
            target.position + offset;

        if (mapBounds != null &&
            cameraComponent.orthographic)
        {
            ClampToMap(
                ref targetPosition);
        }

        transform.position =
            Vector3.Lerp(
                transform.position,
                targetPosition,
                followSpeed * Time.deltaTime);
    }

    private void ClampToMap(
        ref Vector3 targetPosition)
    {
        Bounds bounds =
            mapBounds.bounds;

        float halfHeight =
            cameraComponent.orthographicSize;

        float halfWidth =
            halfHeight * cameraComponent.aspect;

        targetPosition.x =
            bounds.size.x > halfWidth * 2f
                ? Mathf.Clamp(
                    targetPosition.x,
                    bounds.min.x + halfWidth,
                    bounds.max.x - halfWidth)
                : bounds.center.x;

        targetPosition.y =
            bounds.size.y > halfHeight * 2f
                ? Mathf.Clamp(
                    targetPosition.y,
                    bounds.min.y + halfHeight,
                    bounds.max.y - halfHeight)
                : bounds.center.y;
    }
}