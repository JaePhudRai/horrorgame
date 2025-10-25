#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using UnityEngine;

public class MouseMovement : MonoBehaviour
{
    [Header("Input / Sensitivity")]
    public float mouseSensitivity = 60f;   // lowered default
    public float newInputScale = 0.005f;   // lowered default for Mouse.current.delta
    public bool debugInput = false;

    [Header("Smoothing (exponential)")]
    [Tooltip("Higher = faster response, lower = smoother/slower")]
    public float smoothSpeed = 12f;

    float xRotation = 0f;
    float yRotation = 0f;

    public float topClamp = 90f;
    public float bottomClamp = -90f;

    Vector2 smoothedDelta = Vector2.zero;

    void Awake()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void LateUpdate()
    {
        Vector2 rawDelta = Vector2.zero;

#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null)
        {
            Vector2 newDelta = Mouse.current.delta.ReadValue();
            rawDelta.x = newDelta.x * (mouseSensitivity * newInputScale);
            rawDelta.y = newDelta.y * (mouseSensitivity * newInputScale);
        }
        else
#endif
        {
            float oldX = Input.GetAxis("Mouse X");
            float oldY = Input.GetAxis("Mouse Y");
            rawDelta.x = oldX * mouseSensitivity * Time.deltaTime;
            rawDelta.y = oldY * mouseSensitivity * Time.deltaTime;
        }

        float lerpFactor = 1f - Mathf.Exp(-smoothSpeed * Time.deltaTime);
        smoothedDelta = Vector2.Lerp(smoothedDelta, rawDelta, lerpFactor);

        if (debugInput)
            Debug.Log($"MouseMovement DEBUG raw=({rawDelta.x:F3},{rawDelta.y:F3}) smoothed=({smoothedDelta.x:F3},{smoothedDelta.y:F3})");

        xRotation -= smoothedDelta.y;
        yRotation += smoothedDelta.x;

        xRotation = Mathf.Clamp(xRotation, bottomClamp, topClamp);
        transform.localRotation = Quaternion.Euler(xRotation, yRotation, 0f);
    }

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