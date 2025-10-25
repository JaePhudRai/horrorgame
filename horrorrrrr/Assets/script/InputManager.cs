using UnityEngine;
using UnityEngine.InputSystem;

namespace HorrorGame
{
    [RequireComponent(typeof(PlayerMovement))]
    public class InputManager : MonoBehaviour
    {
        [Header("Action Map / Action Names")]
        public string defaultActionMapName = "OnFoot";   // action map to enable
        public string movementActionName = "Movement";   // action name that returns Vector2

        [Header("References")]
        public PlayerMovement playerMovement;           // assign in inspector or auto-find

        UnityEngine.InputSystem.PlayerInput piComponent;
        InputAction movementAction;
        InputActionMap enabledMap;

        void Awake()
        {
            if (playerMovement == null)
                playerMovement = GetComponent<PlayerMovement>();

            piComponent = GetComponent<UnityEngine.InputSystem.PlayerInput>();

            if (piComponent == null)
            {
                Debug.LogWarning("InputManager: No PlayerInput component found. Ensure either add PlayerInput or use PlayerMovement.useOldInputFallback = true.");
                return;
            }

            var actions = piComponent.actions;
            if (actions == null)
            {
                Debug.LogError("InputManager: PlayerInput.actions is null. Check your Input Actions asset.");
                return;
            }

            foreach (var map in actions.actionMaps)
                map.Disable();

            enabledMap = actions.FindActionMap(defaultActionMapName, true);
            if (enabledMap == null)
            {
                Debug.LogWarning($"InputManager: Action map '{defaultActionMapName}' not found in asset.");
                return;
            }

            enabledMap.Enable();
            movementAction = enabledMap.FindAction(movementActionName, true);
            if (movementAction == null)
                Debug.LogWarning($"InputManager: Movement action '{movementActionName}' not found in map '{defaultActionMapName}'.");
        }

        void FixedUpdate()
        {
            if (playerMovement == null) return;

            if (movementAction != null && movementAction.enabled)
            {
                Vector2 v = movementAction.ReadValue<Vector2>();
                playerMovement.ProcessMove(v);
            }
        }

        void OnEnable() => enabledMap?.Enable();
        void OnDisable() => enabledMap?.Disable();
    }
}