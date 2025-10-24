using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    private PlayerInput playerInput;               // generated wrapper
    private InputActionMap defaultMap;             // the map we keep enabled
    private PlayerInput.OnFootActions onFoot;
    private PlayerMovement playerMovement;

    [Header("Input")]
    public string defaultActionMapName = "OnFoot"; // name of the map to enable at runtime

    void Awake()
    {
        // instantiate generated wrapper
        playerInput = new PlayerInput();

        // disable all action maps in the asset
        if (playerInput?.asset != null)
        {
            foreach (var map in playerInput.asset.actionMaps)
                map.Disable();

            // find and enable only the default map
            defaultMap = playerInput.asset.FindActionMap(defaultActionMapName);
            if (defaultMap != null)
                defaultMap.Enable();
            else
                Debug.LogWarning($"InputManager: Action map '{defaultActionMapName}' not found in asset.");
        }

        onFoot = playerInput.OnFoot;
        playerMovement = GetComponent<PlayerMovement>();

        if (playerMovement == null)
            Debug.LogWarning("InputManager: No PlayerMovement found on the same GameObject.");
    }

    void FixedUpdate()
    {
        if (playerMovement != null && defaultMap != null)
            playerMovement.ProcessMove(onFoot.Movement.ReadValue<Vector2>());
    }

    private void OnEnable()
    {
        // enable only the default map when this GameObject enables
        defaultMap?.Enable();
    }

    private void OnDisable()
    {
        // disable only the default map when this GameObject disables
        defaultMap?.Disable();
    }

    // Optional: if you use a PlayerInput component on the same object and want to force control scheme:
    public void ForceKeyboardMouseControlScheme()
    {
        var pi = GetComponent<UnityEngine.InputSystem.PlayerInput>();
        if (pi != null && Keyboard.current != null && Mouse.current != null)
        {
            // SwitchCurrentControlScheme exists on PlayerInput component
            pi.SwitchCurrentControlScheme("Keyboard&Mouse", Keyboard.current, Mouse.current);
        }
    }
}