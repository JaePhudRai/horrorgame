#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using UnityEngine;

public class MouseMovement : MonoBehaviour
{
    public float mouseSensitivity = 100f;   // tweak this
    public float smoothTime = 0.03f;        // smaller = tighter, larger = smoother/slower
    public bool debugInput = true;          // enable to log input each frame

    float xRotation = 0f;
    float yRotation = 0f;

    // clamp (degrees)
    public float topClamp = 90f;
    public float bottomClamp = -90f;

    // smoothing helpers
    float xVel = 0f;
    float yVel = 0f;

    void Awake()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void LateUpdate()
    {
        // read input
        float mouseX = 0f;
        float mouseY = 0f;

        // Old Input Manager fallback (always available)
        float oldX = Input.GetAxis("Mouse X");
        float oldY = Input.GetAxis("Mouse Y");

#if ENABLE_INPUT_SYSTEM
        bool hasNewMouse = Mouse.current != null;
        Vector2 newDelta = hasNewMouse ? Mouse.current.delta.ReadValue() : Vector2.zero;
        if (hasNewMouse)
        {
            // scale raw pixel delta with sensitivity (do not multiply by Time.deltaTime)
            mouseX = newDelta.x * (mouseSensitivity * 0.01f);
            mouseY = newDelta.y * (mouseSensitivity * 0.01f);
        }
        else
#endif
        {
            // old Input Manager: GetAxis is frame-rate independent already in typical setups
            mouseX = oldX * mouseSensitivity * Time.deltaTime;
            mouseY = oldY * mouseSensitivity * Time.deltaTime;
        }

        if (debugInput)
        {
#if ENABLE_INPUT_SYSTEM
            Debug.Log($"MouseMovement DEBUG: oldAxis=({oldX:F3},{oldY:F3}) newDelta=({(Mouse.current!=null?newDelta.x:0f):F3},{(Mouse.current!=null?newDelta.y:0f):F3}) effective=({mouseX:F3},{mouseY:F3})");
#else
            Debug.Log($"MouseMovement DEBUG: oldAxis=({oldX:F3},{oldY:F3}) effective=({mouseX:F3},{mouseY:F3})");
#endif
        }

        // target rotations
        float targetX = xRotation - mouseY;
        float targetY = yRotation + mouseX;

        // smooth the rotations
        xRotation = Mathf.SmoothDamp(xRotation, targetX, ref xVel, smoothTime);
        yRotation = Mathf.SmoothDamp(yRotation, targetY, ref yVel, smoothTime);

        // clamp and apply
        xRotation = Mathf.Clamp(xRotation, bottomClamp, topClamp);
        transform.localRotation = Quaternion.Euler(xRotation, yRotation, 0f);
    }

    // helper to unlock cursor from inspector or other scripts
    public void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    // helper to lock cursor from inspector or other scripts
    public void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}