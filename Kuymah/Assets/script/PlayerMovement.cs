using UnityEngine;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("References")]
    [SerializeField] Transform cameraTransform;
    [SerializeField] Transform playerModel; // assign the visual model (child) here

    [Header("Movement")]
    [SerializeField] float speed = 6f;
    [SerializeField] float acceleration = 20f;
    [SerializeField] float deceleration = 25f;
    [SerializeField] float rotationSmoothTime = 0.08f;

    [Header("Sprint / Crouch")]
    [SerializeField] float sprintMultiplier = 1.7f;
    [SerializeField] KeyCode sprintKey = KeyCode.LeftShift;
    [SerializeField] KeyCode crouchKey = KeyCode.C;
    [SerializeField] float crouchHeight = 1.0f;
    [SerializeField] float crouchSpeedMultiplier = 0.5f;
    [SerializeField] float crouchTransitionSpeed = 8f;

    [Header("Jump & Gravity")]
    [SerializeField] float jumpHeight = 1.6f;
    [SerializeField] float gravity = -20f;
    [SerializeField] float terminalVelocity = -50f;

    [Header("Ground / Slope")]
    [SerializeField] Transform groundCheck;
    [SerializeField] float groundDistance = 0.2f;
    [SerializeField] LayerMask groundMask;
    [SerializeField] float slopeForceDown = 5f; // helps stick to slopes

    [Header("CharacterController Settings")]
    [SerializeField] float controllerHeight = 2f;
    [SerializeField] Vector3 controllerCenter = new Vector3(0, 1f, 0);
    [SerializeField] float controllerSkinWidth = 0.08f;
    [SerializeField] float controllerStepOffset = 0.3f;

    [Header("Debug")]
    [SerializeField] bool debugMode = false;

#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
    // Optional Input System actions (assign in Inspector or via code)
    public InputActionProperty moveAction;
    public InputActionProperty jumpAction;
#else
    // Old Input System uses default axes and keys
#endif

    CharacterController controller;
    Vector3 currentVelocity;        // horizontal smoothed velocity
    float verticalVelocity = 0f;    // vertical velocity
    bool isGrounded;
    bool isCrouching;
    float currentSpeed;
    float rotationVelocity;

    void Reset()
    {
        // sensible defaults
        controllerHeight = 2f;
        controllerCenter = new Vector3(0, 1f, 0);
        controllerStepOffset = 0.3f;
    }

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        ApplyControllerSettings();
    }

    void Start()
    {
        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        if (groundCheck == null)
        {
            // create a fallback groundCheck at feet
            GameObject go = new GameObject("GroundCheck");
            go.transform.SetParent(transform);
            go.transform.localPosition = Vector3.zero;
            groundCheck = go.transform;
            if (debugMode) Debug.Log("PlayerMovement: created fallback groundCheck.");
        }

        currentSpeed = speed;
    }

    void ApplyControllerSettings()
    {
        controller.height = controllerHeight;
        controller.center = controllerCenter;
        controller.skinWidth = controllerSkinWidth;
        controller.stepOffset = controllerStepOffset;
    }

    void Update()
    {
        // --- INPUT READ ---
        Vector2 input = Vector2.zero;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        if (moveAction != null && moveAction.action != null)
            input = moveAction.action.ReadValue<Vector2>();
        bool jumpPressed = jumpAction != null && jumpAction.action != null && jumpAction.action.WasPressedThisFrame();
#else
        input.x = Input.GetAxisRaw("Horizontal");
        input.y = Input.GetAxisRaw("Vertical");
        bool jumpPressed = Input.GetButtonDown("Jump");
#endif

        bool sprinting = Input.GetKey(sprintKey);
        if (sprinting) currentSpeed = speed * sprintMultiplier;
        else currentSpeed = speed;

        // Crouch toggle
        if (Input.GetKeyDown(crouchKey))
            isCrouching = !isCrouching;

        float targetSpeedMultiplier = isCrouching ? crouchSpeedMultiplier : 1f;
        currentSpeed *= targetSpeedMultiplier;

        // --- GROUND CHECK ---
        int effectiveMask = groundMask.value == 0 ? ~0 : groundMask.value;
        if (groundCheck != null)
            isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, effectiveMask);
        else
            isGrounded = controller.isGrounded;

        if (isGrounded && verticalVelocity < 0f)
            verticalVelocity = -2f; // small negative to keep character grounded

        // --- MOVEMENT DIRECTION (camera relative) ---
        Vector3 moveDir = Vector3.zero;
        if (cameraTransform != null)
        {
            Vector3 forward = cameraTransform.forward;
            Vector3 right = cameraTransform.right;
            forward.y = 0;
            right.y = 0;
            forward.Normalize();
            right.Normalize();
            moveDir = (right * input.x + forward * input.y);
        }
        else
        {
            moveDir = (transform.right * input.x + transform.forward * input.y);
        }

        if (moveDir.sqrMagnitude > 1f)
            moveDir.Normalize();

        // --- HORIZONTAL SMOOTHING (acceleration / deceleration) ---
        Vector3 targetHorizontalVelocity = moveDir * currentSpeed;
        float accel = (targetHorizontalVelocity.sqrMagnitude > currentVelocity.sqrMagnitude) ? acceleration : deceleration;
        currentVelocity = Vector3.MoveTowards(currentVelocity, targetHorizontalVelocity, accel * Time.deltaTime);

        // --- ROTATION (face movement) ---
        if (moveDir.sqrMagnitude > 0.01f)
        {
            float targetAngle = Mathf.Atan2(moveDir.x, moveDir.z) * Mathf.Rad2Deg;
            float smoothAngle = Mathf.SmoothDampAngle((playerModel != null ? playerModel.eulerAngles.y : transform.eulerAngles.y), targetAngle, ref rotationVelocity, rotationSmoothTime);

            // Important: rotate only the playerModel (visual) — do NOT rotate the root transform (capsule)
            if (playerModel != null)
                playerModel.rotation = Quaternion.Euler(0f, smoothAngle, 0f);
            else
                transform.rotation = Quaternion.Euler(0f, smoothAngle, 0f); // fallback if no model assigned
        }

        // --- JUMP ---
        if (jumpPressed && isGrounded)
        {
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            if (debugMode) Debug.Log($"Jump: verticalVelocity={verticalVelocity:F2}");
        }

        // --- GRAVITY & SLOPE STICKING ---
        if (!isGrounded)
        {
            verticalVelocity += gravity * Time.deltaTime;
            if (verticalVelocity < terminalVelocity) verticalVelocity = terminalVelocity;
        }
        else
        {
            // apply small downward force when moving on slopes to keep grounded
            if (moveDir.sqrMagnitude > 0.01f)
                verticalVelocity -= slopeForceDown * Time.deltaTime;
        }

        // --- CROUCH HEIGHT SMOOTH TRANSITION ---
        float targetHeight = isCrouching ? crouchHeight : controllerHeight;
        if (Mathf.Abs(controller.height - targetHeight) > 0.01f)
        {
            float newHeight = Mathf.Lerp(controller.height, targetHeight, Time.deltaTime * crouchTransitionSpeed);
            float heightDiff = controller.height - newHeight;
            controller.height = newHeight;
            // adjust center so character doesn't sink into ground
            controller.center = new Vector3(controller.center.x, controller.center.y - heightDiff * 0.5f, controller.center.z);
        }

        // --- FINAL MOVE ---
        Vector3 finalVelocity = currentVelocity + Vector3.up * verticalVelocity;
        controller.Move(finalVelocity * Time.deltaTime);

        if (debugMode)
        {
            Debug.Log($"Move: input={input}, currentVel={currentVelocity.magnitude:F2}, grounded={isGrounded}, vertical={verticalVelocity:F2}, crouch={isCrouching}");
        }
    }

    void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(groundCheck.position, groundDistance);
        }

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(transform.position + controllerCenter, new Vector3(0.5f, controllerHeight, 0.5f));
    }
}