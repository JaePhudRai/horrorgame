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

    // Assign the player's root (capsule / body) here in the Inspector.
    // If assigned, rotation will be applied to this transform; otherwise to this GameObject.
    public Transform playerBody;

    [Header("Roll (Z) settings")]
    public bool allowRoll = true;
    public float rollSpeed = 90f;            // degrees per second when pressing roll keys
    public float rollSmoothSpeed = 8f;       // smoothing for roll changes
    public KeyCode rollLeftKey = KeyCode.Q;
    public KeyCode rollRightKey = KeyCode.E;

    float xRotation = 0f;
    float yRotation = 0f;
    float zRotation = 0f;           // current (smoothed) roll
    float targetZRotation = 0f;     // target roll

    public float topClamp = 90f;
    public float bottomClamp = -90f;

    Vector2 smoothedDelta = Vector2.zero;

    void Awake()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (playerBody == null && transform.parent != null)
            playerBody = transform.parent;
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

        // Roll input (Q/E by default). Adjust targetZRotation.
        if (allowRoll)
        {
            float rollInput = 0f;
            if (Input.GetKey(rollLeftKey)) rollInput += 1f;
            if (Input.GetKey(rollRightKey)) rollInput -= 1f;

            targetZRotation += rollInput * rollSpeed * Time.deltaTime;
            // Optional: keep targetZRotation within -180..180 to avoid overflow
            if (targetZRotation > 180f) targetZRotation -= 360f;
            if (targetZRotation < -180f) targetZRotation += 360f;

            // Smooth the roll
            float rollLerp = 1f - Mathf.Exp(-rollSmoothSpeed * Time.deltaTime);
            zRotation = Mathf.LerpAngle(zRotation, targetZRotation, rollLerp);
        }
        else
        {
            targetZRotation = 0f;
            zRotation = Mathf.LerpAngle(zRotation, 0f, 1f - Mathf.Exp(-rollSmoothSpeed * Time.deltaTime));
        }

        // Apply full 3-axis rotation to the assigned target (playerBody if set, else this transform)
        Quaternion targetRotation = Quaternion.Euler(xRotation, yRotation, zRotation);
        if (playerBody != null)
            playerBody.localRotation = targetRotation;
        else
            transform.localRotation = targetRotation;
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