#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using UnityEngine;

public class MouseMovement : MonoBehaviour
{
    [Header("Input / Sensitivity")]
    public float mouseSensitivity = 100f;   // tweak this (used with newInputScale)
    public float newInputScale = 0.01f;     // multipler for Mouse.current.delta (adjust to reduce speed)
    public bool debugInput = false;         // set false in normal play

    [Header("Smoothing (exponential)")]
    [Tooltip("Higher = faster response, lower = smoother/slower")]
    public float smoothSpeed = 12f;         // recommended: 8 - 16

    float xRotation = 0f;
    float yRotation = 0f;

    // clamp (degrees)
    public float topClamp = 90f;
    public float bottomClamp = -90f;

    // smoothing state
    Vector2 smoothedDelta = Vector2.zero;

    void Awake()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void LateUpdate()
    {
        // Read raw mouse delta in a consistent unit
        Vector2 rawDelta = Vector2.zero;

#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null)
        {
            // Mouse.current.delta is pixels since last frame (do NOT multiply by Time.deltaTime)
            Vector2 newDelta = Mouse.current.delta.ReadValue();
            rawDelta.x = newDelta.x * (mouseSensitivity * newInputScale);
            rawDelta.y = newDelta.y * (mouseSensitivity * newInputScale);
        }
        else
#endif
        {
            // Old Input Manager: GetAxis returns a delta-like value, multiply by Time.deltaTime for consistency
            float oldX = Input.GetAxis("Mouse X");
            float oldY = Input.GetAxis("Mouse Y");
            rawDelta.x = oldX * mouseSensitivity * Time.deltaTime;
            rawDelta.y = oldY * mouseSensitivity * Time.deltaTime;
        }

        // Exponential smoothing (frame-rate independent)
        float lerpFactor = 1f - Mathf.Exp(-smoothSpeed * Time.deltaTime);
        smoothedDelta = Vector2.Lerp(smoothedDelta, rawDelta, lerpFactor);

        if (debugInput)
            Debug.Log($"MouseMovement DEBUG raw=({rawDelta.x:F3},{rawDelta.y:F3}) smoothed=({smoothedDelta.x:F3},{smoothedDelta.y:F3})");

        // Apply rotation
        xRotation -= smoothedDelta.y;
        yRotation += smoothedDelta.x;

        xRotation = Mathf.Clamp(xRotation, bottomClamp, topClamp);
        transform.localRotation = Quaternion.Euler(xRotation, yRotation, 0f);
    }

    // helpers
    public void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}