// CameraController.cs
using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    [Header("Follow Settings")]
    public Transform target;
    public Vector3 offset = new Vector3(0, 5, -10);
    public float followSpeed = 5f;
    public float rotationSpeed = 2f;
    public float smoothTime = 0.3f;

    [Header("Boundaries")]
    public bool useBoundaries = false;
    public float minX = -50f;
    public float maxX = 50f;
    public float minY = 0f;
    public float maxY = 50f;
    public float minZ = -50f;
    public float maxZ = 50f;

    [Header("Zoom Settings")]
    public float minZoom = 2f;
    public float maxZoom = 10f;
    public float zoomSpeed = 2f;
    public float zoomSmoothTime = 0.3f;

    [Header("Collision Settings")]
    public bool useCollision = true;
    public float collisionRadius = 0.2f;
    public LayerMask collisionLayers;

    private Vector3 currentVelocity;
    private float currentZoomVelocity;
    private float currentZoom;
    private bool isShaking = false;
    private Vector3 originalOffset;
    private float shakeMagnitude;
    private PlayerInput playerInput;
    private InputAction zoomAction;
    private bool inputSystemEnabled = false;

    private void Awake()
    {
        try
        {
            // Get the PlayerInput component
            playerInput = GetComponent<PlayerInput>();
            if (playerInput == null)
            {
                Debug.LogWarning("PlayerInput component not found. Adding one...");
                playerInput = gameObject.AddComponent<PlayerInput>();
            }

            // Check if we have an Input Action Asset assigned
            if (playerInput.actions == null)
            {
                Debug.LogWarning("No Input Action Asset assigned to PlayerInput component. Camera zoom will be disabled.");
                return;
            }

            // Try to get the zoom action
            zoomAction = playerInput.actions["Zoom"];
            if (zoomAction == null)
            {
                Debug.LogWarning("Zoom action not found in Input Action Asset. Camera zoom will be disabled.");
                return;
            }

            inputSystemEnabled = true;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Error setting up Input System: {e.Message}. Camera zoom will be disabled.");
            inputSystemEnabled = false;
        }
    }

    private void Start()
    {
        if (target == null)
        {
            Debug.LogWarning("Camera target not assigned! Please assign a target in the inspector.");
            return;
        }

        originalOffset = offset;
        currentZoom = -offset.z;
        transform.position = target.position + offset;
    }

    private void LateUpdate()
    {
        if (target == null) return;

        // Handle zoom using the new Input System
        if (inputSystemEnabled && zoomAction != null)
        {
            float scrollInput = zoomAction.ReadValue<float>();
            if (scrollInput != 0)
            {
                currentZoom = Mathf.Clamp(currentZoom - scrollInput * zoomSpeed, minZoom, maxZoom);
                offset.z = -currentZoom;
            }
        }

        // Calculate desired position
        Vector3 desiredPos = target.position + offset;

        // Apply boundaries if enabled
        if (useBoundaries)
        {
            desiredPos.x = Mathf.Clamp(desiredPos.x, minX, maxX);
            desiredPos.y = Mathf.Clamp(desiredPos.y, minY, maxY);
            desiredPos.z = Mathf.Clamp(desiredPos.z, minZ, maxZ);
        }

        // Handle collision if enabled
        if (useCollision)
        {
            Vector3 directionToTarget = (target.position - transform.position).normalized;
            float distanceToTarget = Vector3.Distance(transform.position, target.position);
            
            RaycastHit hit;
            if (Physics.SphereCast(transform.position, collisionRadius, directionToTarget, out hit, distanceToTarget, collisionLayers))
            {
                desiredPos = hit.point - directionToTarget * collisionRadius;
            }
        }

        // Smooth follow with damping
        transform.position = Vector3.SmoothDamp(transform.position, desiredPos, ref currentVelocity, smoothTime);

        // Smooth rotation
        Quaternion targetRotation = Quaternion.LookRotation(target.position - transform.position);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }

    public void Shake(float duration, float magnitude)
    {
        if (!isShaking)
        {
            shakeMagnitude = magnitude;
            StartCoroutine(DoShake(duration));
        }
    }

    private IEnumerator DoShake(float duration)
    {
        isShaking = true;
        Vector3 originalPos = transform.position;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float x = Mathf.PerlinNoise(Time.time * 10f, 0f) * 2f - 1f;
            float y = Mathf.PerlinNoise(0f, Time.time * 10f) * 2f - 1f;
            
            Vector3 shakeOffset = new Vector3(x, y, 0) * shakeMagnitude * (1f - (elapsed / duration));
            transform.position = originalPos + shakeOffset;
            
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = originalPos;
        isShaking = false;
    }

    private void OnDrawGizmosSelected()
    {
        if (useBoundaries)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(new Vector3((minX + maxX) / 2, (minY + maxY) / 2, (minZ + maxZ) / 2),
                               new Vector3(maxX - minX, maxY - minY, maxZ - minZ));
        }
    }
}
