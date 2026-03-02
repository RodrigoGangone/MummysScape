/// <summary>
/// CursorByInputScheme
/// Ajusta visibilidad y lock del cursor según el esquema de entrada actual,
/// suscribiéndose a InputSchemeManager. Pensado para vivir en InputSchemeRoot
/// y aplicarse en todas las escenas.
/// Usa una pequeña coroutine para reforzar el estado del cursor durante
/// unos pocos frames después del cambio, evitando flicker sin usar Update().
/// </summary>
using System.Collections;
using UnityEngine;

public sealed class CursorByInputScheme : MonoBehaviour
{
    [Header("Keyboard & Mouse")]
    [SerializeField] private bool _visibleWithMouse = true;
    [SerializeField] private CursorLockMode _lockWithMouse = CursorLockMode.None;

    [Header("Joystick")]
    [SerializeField] private bool _visibleWithJoystick = false;
    [SerializeField] private CursorLockMode _lockWithJoystick = CursorLockMode.Locked;

    [Header("Debug")]
    [SerializeField] private bool _debugLogs = false;

    [Header("Stabilización")]
    [Tooltip("Cuántos frames refuerza el estado del cursor después de un cambio de esquema.")]
    [SerializeField] private int _enforceFramesOnChange = 60;

    private bool _subscribed;
    private Coroutine _enforceRoutine;

    private void Awake()
    {
        TrySubscribe();
    }

    private void OnEnable()
    {
        TrySubscribe();
    }

    private void Start()
    {
        TrySubscribe();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void OnDestroy()
    {
        Unsubscribe();
    }

    private void TrySubscribe()
    {
        if (_subscribed) return;

        var manager = InputSchemeManager.Instance;
        if (manager == null)
        {
            if (_debugLogs)
                Debug.LogWarning("[CursorByInputScheme] InputSchemeManager.Instance es null en TrySubscribe.");
            return;
        }

        manager.SchemeChanged -= OnSchemeChanged;
        manager.SchemeChanged += OnSchemeChanged;
        _subscribed = true;

        OnSchemeChanged(manager.CurrentScheme);

        if (_debugLogs)
            Debug.Log($"[CursorByInputScheme] Suscrito. Scheme inicial = {manager.CurrentScheme}");
    }

    private void Unsubscribe()
    {
        if (!_subscribed) return;

        var manager = InputSchemeManager.Instance;
        if (manager != null)
        {
            manager.SchemeChanged -= OnSchemeChanged;
        }

        _subscribed = false;
    }

    private void OnSchemeChanged(InputScheme scheme)
    {
        if (_debugLogs)
            Debug.Log($"[CursorByInputScheme] OnSchemeChanged => {scheme}");

        ApplyCursorState(scheme);

        if (_enforceRoutine != null)
            StopCoroutine(_enforceRoutine);

        _enforceRoutine = StartCoroutine(EnforceCursorStateForFrames(scheme, _enforceFramesOnChange));
    }

    private void ApplyCursorState(InputScheme scheme)
    {
        bool targetVisible;
        CursorLockMode targetLock;

        switch (scheme)
        {
            case InputScheme.KeyboardMouse:
                targetVisible = _visibleWithMouse;
                targetLock    = _lockWithMouse;
                break;

            case InputScheme.Joystick:
            default:
                targetVisible = _visibleWithJoystick;
                targetLock    = _lockWithJoystick;
                break;
        }

        Cursor.visible   = targetVisible;
        Cursor.lockState = targetLock;

        if (_debugLogs)
        {
            Debug.Log($"[CursorByInputScheme] Cursor => visible={Cursor.visible}, lock={Cursor.lockState}");
        }
    }

    private IEnumerator EnforceCursorStateForFrames(InputScheme scheme, int frames)
    {
        for (int i = 0; i < frames; i++)
        {
            yield return new WaitForEndOfFrame();
            ApplyCursorState(scheme);
        }

        _enforceRoutine = null;
    }
}