/// <summary>
/// InputSchemeManager
/// - Escucha el PlayerInput global para saber qué control scheme está activo.
/// - Expone el esquema actual (KeyboardMouse / Joystick).
/// - Emite un evento cuando cambia, para que UI y otros sistemas reaccionen.
/// Vive entre escenas (DontDestroyOnLoad).
/// Requiere que el PlayerInput tenga Behavior = "Invoke C# Events".
/// </summary>
using System;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[DisallowMultipleComponent]
#if ENABLE_INPUT_SYSTEM
[RequireComponent(typeof(PlayerInput))]
#endif
public sealed class InputSchemeManager : MonoBehaviour
{
    // Nombres EXACTOS tal como los ve el PlayerInput.currentControlScheme
    public const string KeyboardMouseSchemeName = "Keyboard&Mouse";
    public const string JoystickSchemeName      = "Joystick";
    public const string GamepadSchemeName       = "Gamepad";

    public static InputSchemeManager Instance { get; private set; }

    public InputScheme CurrentScheme { get; private set; } = InputScheme.KeyboardMouse;

    /// <summary>Evento disparado cuando cambia el esquema de entrada.</summary>
    public event Action<InputScheme> SchemeChanged;

#if ENABLE_INPUT_SYSTEM
    private PlayerInput _playerInput;
#endif

    [SerializeField] private bool _debugLogs = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

#if ENABLE_INPUT_SYSTEM
        _playerInput = GetComponent<PlayerInput>();

        if (_playerInput != null &&
            _playerInput.notificationBehavior != PlayerNotifications.InvokeCSharpEvents &&
            _debugLogs)
        {
            Debug.LogWarning(
                "[InputSchemeManager] PlayerInput.Behavior debería ser 'Invoke C# Events' para que onControlsChanged funcione.");
        }
#endif
    }

    private void OnEnable()
    {
#if ENABLE_INPUT_SYSTEM
        if (_playerInput != null)
        {
            _playerInput.onControlsChanged += HandleControlsChanged;
            UpdateSchemeFromPlayerInput(_playerInput); // estado inicial
        }
#endif
    }

    private void OnDisable()
    {
#if ENABLE_INPUT_SYSTEM
        if (_playerInput != null)
        {
            _playerInput.onControlsChanged -= HandleControlsChanged;
        }
#endif
    }

#if ENABLE_INPUT_SYSTEM
    private void HandleControlsChanged(PlayerInput playerInput)
    {
        UpdateSchemeFromPlayerInput(playerInput);
    }

    private void UpdateSchemeFromPlayerInput(PlayerInput playerInput)
    {
        if (playerInput == null) return;

        string schemeName = playerInput.currentControlScheme;
        if (_debugLogs) Debug.Log($"[InputSchemeManager] currentControlScheme = {schemeName}");

        InputScheme newScheme = ResolveScheme(schemeName);

        if (newScheme == CurrentScheme)
        {
            if (_debugLogs) Debug.Log($"[InputSchemeManager] Scheme unchanged ({newScheme})");
            return;
        }

        CurrentScheme = newScheme;
        if (_debugLogs) Debug.Log($"[InputSchemeManager] SchemeChanged => {newScheme}");
        SchemeChanged?.Invoke(newScheme);
    }

    private static InputScheme ResolveScheme(string schemeName)
    {
        if (string.IsNullOrEmpty(schemeName) || schemeName == KeyboardMouseSchemeName)
            return InputScheme.KeyboardMouse;

        if (schemeName == JoystickSchemeName || schemeName == GamepadSchemeName)
            return InputScheme.Joystick;

        // fallback, por si en el futuro agregas otro scheme raro
        return InputScheme.KeyboardMouse;
    }
#endif
}