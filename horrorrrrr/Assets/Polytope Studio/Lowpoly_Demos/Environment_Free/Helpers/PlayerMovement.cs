#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Speeds")]
    public float walkSpeed = 4f;
    public float sprintSpeed = 7f;

    [Header("Jump & Gravity")]
    public float jumpHeight = 1.5f;
    public float gravity = -9.81f;

    [Header("Smoothing")]
    [Tooltip("Higher = faster to reach target speed")]
    public float acceleration = 10f;

    [Header("References")]
    public Transform cameraTransform; // optional: align movement to camera forward

    [Header("Ground Check")]
    public Transform groundCheck;            // child at feet (optional)
    public float groundDistance = 0.2f;      // sphere radius for ground check
    public LayerMask groundMask = ~0;        // layers considered ground
    public float stickToGroundForce = 0.5f;  // small downward force while grounded

    [Header("Input")]
    public bool useOldInputFallback = true;  // use Input.GetAxis fallback

    [Header("Debug")]
    public bool debugLogs = false;

    // internals
    CharacterController controller;
    CapsuleCollider capsuleFallback;
    Vector2 input = Vector2.zero;
    bool sprinting = false;
    bool jumpRequested = false;

    Vector3 moveVelocity = Vector3.zero;   // horizontal velocity
    Vector3 velSmoothRef = Vector3.zero;
    float verticalVelocity = 0f;

    bool isGrounded = false;
    Vector3 groundNormal = Vector3.up;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        if (controller == null)
        {
            controller = gameObject.AddComponent<CharacterController>();
            controller.height = 2f;
            controller.radius = 0.5f;
            Debug.Log("PlayerMovement: Added CharacterController fallback.");
        }

        capsuleFallback = GetComponent<CapsuleCollider>();

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        if (groundCheck == null)
        {
            GameObject go = new GameObject("GroundCheck");
            go.hideFlags = HideFlags.DontSave;
            go.transform.SetParent(transform);
            float feetOffset = -(controller != null ? controller.height * 0.5f : 0.9f) + (controller != null ? controller.radius : 0.5f);
            go.transform.localPosition = new Vector3(0f, feetOffset, 0f);
            groundCheck = go.transform;
        }
    }

    void Update()
    {
        if (useOldInputFallback)
            ReadOldInputFallback();

#if ENABLE_INPUT_SYSTEM
        // If using new Input System and not relying on external InputManager, OnMove etc. will set `input`.
#endif

        // capture jump in Update to avoid missing it between physics steps
        if (Input.GetButtonDown("Jump"))
            jumpRequested = true;
    }

    void FixedUpdate()
    {
        GroundCheck();
        ApplyMovement();

        if (debugLogs)
            Debug.Log($"PlayerMovement: input={input} sprint={sprinting} grounded={isGrounded} vVel={verticalVelocity:F2} normal={groundNormal}");
    }

    void GroundCheck()
    {
        Vector3 checkPos = groundCheck ? groundCheck.position : transform.position;
        isGrounded = Physics.CheckSphere(checkPos, groundDistance, groundMask, QueryTriggerInteraction.Ignore);

        // sample normal with a short sphere cast for slope projection
        RaycastHit hit;
        if (Physics.SphereCast(checkPos + Vector3.up * 0.05f, groundDistance * 0.5f, Vector3.down, out hit, 0.2f + groundDistance, groundMask, QueryTriggerInteraction.Ignore))
            groundNormal = hit.normal;
        else
            groundNormal = Vector3.up;

        // also prefer CharacterController.isGrounded if true
        if (controller != null && controller.isGrounded)
            isGrounded = true;
    }

#if ENABLE_INPUT_SYSTEM
    // Optional: bind PlayerInput actions to these
    public void OnMove(InputAction.CallbackContext ctx) => input = ctx.ReadValue<Vector2>();
    public void OnSprint(InputAction.CallbackContext ctx) => sprinting = ctx.ReadValue<float>() > 0.5f;
    public void OnJump(InputAction.CallbackContext ctx) { if (ctx.performed) jumpRequested = true; }
#endif

    public void ProcessMove(Vector2 move) => input = move; // external caller support

    void ReadOldInputFallback()
    {
        input.x = Input.GetAxis("Horizontal");
        input.y = Input.GetAxis("Vertical");
        sprinting = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        // jump captured in Update
    }

    void ApplyMovement()
    {
        if (controller == null) return;

        // reset vertical velocity slightly when grounded
        if (isGrounded && verticalVelocity < 0f)
            verticalVelocity = -0.5f;

        // camera-relative desired direction
        Vector3 forward = cameraTransform ? cameraTransform.forward : transform.forward;
        Vector3 right = cameraTransform ? cameraTransform.right : transform.right;
        forward.y = 0f; right.y = 0f;
        forward.Normalize(); right.Normalize();

        Vector3 desiredDir = forward * input.y + right * input.x;
        if (desiredDir.sqrMagnitude > 1f) desiredDir.Normalize();

        // project onto ground plane for slope handling
        Vector3 planarDir = Vector3.ProjectOnPlane(desiredDir, groundNormal).normalized;

        float targetSpeed = sprinting ? sprintSpeed : walkSpeed;
        Vector3 targetVelocity = planarDir * targetSpeed;

        float smoothTime = Mathf.Max(0.0001f, 1f / Mathf.Max(0.0001f, acceleration));
        moveVelocity = Vector3.SmoothDamp(moveVelocity, targetVelocity, ref velSmoothRef, smoothTime);

        // jump
        if (isGrounded && jumpRequested)
        {
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            jumpRequested = false;
        }

        // small stick force to keep grounded
        if (isGrounded && moveVelocity.sqrMagnitude > 0.01f)
            verticalVelocity -= stickToGroundForce * Time.fixedDeltaTime;

        // gravity
        verticalVelocity += gravity * Time.fixedDeltaTime;

        Vector3 finalVelocity = moveVelocity + Vector3.up * verticalVelocity;
        controller.Move(finalVelocity * Time.fixedDeltaTime);
    }

    void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(groundCheck.position, groundDistance);
    }
}