#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using UnityEngine;

namespace HorrorGame
{
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
        public Transform groundCheck;            // assign an empty child near feet (optional)
        public float groundDistance = 0.2f;      // radius of sphere to check for ground
        public LayerMask groundMask = ~0;        // what layers count as ground (default = everything)
        public float stickToGroundForce = 0.5f;  // small downward force to keep on ground when walking

        [Header("Input")]
        public bool useOldInputFallback = true;  // allow WASD via old Input.GetAxis even when new Input System present

        // internals
        private CharacterController controller;
        private Vector2 input = Vector2.zero;    // movement input (x = strafe, y = forward)
        private bool sprinting = false;
        private bool jumpPressed = false;

        private Vector3 moveVelocity = Vector3.zero;   // horizontal velocity (m/s)
        private Vector3 velSmoothRef = Vector3.zero;   // ref for SmoothDamp
        private float verticalVelocity = 0f;

        // ground state
        private bool isGrounded = false;

        void Awake()
        {
            controller = GetComponent<CharacterController>();
            if (controller == null)
                Debug.LogError("PlayerMovement requires a CharacterController on the same GameObject.");

            // auto-assign main camera if not set
            if (cameraTransform == null && Camera.main != null)
                cameraTransform = Camera.main.transform;

            // If no groundCheck provided, create a fallback position at the transform's feet
            if (groundCheck == null && controller != null)
            {
                GameObject go = new GameObject("GroundCheck");
                go.hideFlags = HideFlags.DontSave;
                go.transform.SetParent(transform);
                float offset = (controller.height * 0.5f) - controller.radius;
                go.transform.localPosition = new Vector3(0f, -offset, 0f);
                groundCheck = go.transform;
            }
        }

        void FixedUpdate()
        {
            if (useOldInputFallback)
                ReadOldInputFallback();

            GroundCheck();
            ApplyMovement();
        }

        void GroundCheck()
        {
            Vector3 checkPos = groundCheck ? groundCheck.position : transform.position;
            isGrounded = Physics.CheckSphere(checkPos, groundDistance, groundMask, QueryTriggerInteraction.Ignore);
        }

#if ENABLE_INPUT_SYSTEM
        public void OnMove(InputAction.CallbackContext ctx) => input = ctx.ReadValue<Vector2>();
        public void OnSprint(InputAction.CallbackContext ctx) => sprinting = ctx.ReadValue<float>() > 0.5f;
        public void OnJump(InputAction.CallbackContext ctx) { if (ctx.performed) jumpPressed = true; }
#endif

        public void ProcessMove(Vector2 move) => input = move;

        void ReadOldInputFallback()
        {
#if UNITY_EDITOR || UNITY_STANDALONE || UNITY_WEBPLAYER || UNITY_ANDROID || UNITY_IOS
            input.x = Input.GetAxis("Horizontal");
            input.y = Input.GetAxis("Vertical");
            sprinting = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            if (Input.GetButtonDown("Jump"))
                jumpPressed = true;
#endif
        }

        void ApplyMovement()
        {
            if (controller == null) return;

            if (isGrounded && verticalVelocity < 0f)
                verticalVelocity = -2f;

            Vector3 forward = cameraTransform ? cameraTransform.forward : transform.forward;
            Vector3 right = cameraTransform ? cameraTransform.right : transform.right;
            forward.y = 0f; right.y = 0f;
            forward.Normalize(); right.Normalize();

            Vector3 desiredDir = forward * input.y + right * input.x;
            if (desiredDir.sqrMagnitude > 1f) desiredDir.Normalize();

            float targetSpeed = sprinting ? sprintSpeed : walkSpeed;
            Vector3 targetVelocity = desiredDir * targetSpeed;

            float smoothTime = Mathf.Max(0.0001f, 1f / Mathf.Max(0.0001f, acceleration));
            moveVelocity = Vector3.SmoothDamp(moveVelocity, targetVelocity, ref velSmoothRef, smoothTime);

            if (isGrounded && jumpPressed)
            {
                verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
                jumpPressed = false;
            }

            if (isGrounded && moveVelocity.sqrMagnitude > 0.01f)
                verticalVelocity -= stickToGroundForce * Time.deltaTime;

            verticalVelocity += gravity * Time.deltaTime;

            Vector3 finalVelocity = moveVelocity + Vector3.up * verticalVelocity;
            controller.Move(finalVelocity * Time.deltaTime);
        }

        void OnDrawGizmosSelected()
        {
            if (groundCheck == null) return;
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(groundCheck.position, groundDistance);
        }
    }
}