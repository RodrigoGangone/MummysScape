using UnityEngine;

/// <summary>Retiene el primer aporte del botón hasta la llegada de su único foco.</summary>
[DisallowMultipleComponent]
public sealed class PressureButtonOneShotFocusTrigger : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PressureButtonStateResolver _stateResolver;
    [SerializeField] private FocusOnActivation _focusOnActivation;

    // Se conservan los campos serializados: el primer estado habilitado consume el único foco.
    [Header("Enabled States")]
    [SerializeField] private bool _activateOnHalfPressed = true;
    [SerializeField] private bool _activateOnFullyPressed = true;

    private bool _triggered;
    private FocusManager.ActivationHandle _activation;
    public bool IsWaitingForFocus { get; private set; }

    private void Awake() => ResolveReferences();

    private void OnEnable()
    {
        ResolveReferences();
        _stateResolver?.RefreshTrapContribution();
    }

    // El resolver llama antes de publicar el aporte; no depende del orden de suscriptores.
    public bool DelayFirstContribution(PressureButtonState state)
    {
        if (!isActiveAndEnabled) return false;
        if (_triggered) return IsWaitingForFocus;
        bool eligible = state == PressureButtonState.HalfPressed && _activateOnHalfPressed ||
                        state == PressureButtonState.FullyPressed && _activateOnFullyPressed;
        if (!eligible) return false;

        ResolveReferences();
        _triggered = true;
        if (_focusOnActivation == null) return false;
        IsWaitingForFocus = true;
        _activation = _focusOnActivation.ActivateWhenFocused(this, _ => ReleaseContribution(), ReleaseContribution,
            () => _stateResolver != null && _stateResolver.IsPreparingTraps());
        return IsWaitingForFocus;
    }

    private void ReleaseContribution()
    {
        IsWaitingForFocus = false;
        if (_stateResolver != null && _stateResolver.isActiveAndEnabled)
            _stateResolver.RefreshTrapContribution();
    }

    public void CancelPending()
    {
        _activation?.Dispose();
        _activation = null;
        IsWaitingForFocus = false;
    }

    private void OnDisable()
    {
        CancelPending();
        ReleaseContribution();
    }

    public void ResetOneShotTriggers()
    {
        CancelPending();
        _triggered = false;
    }

    private void ResolveReferences()
    {
        if (_stateResolver == null) _stateResolver = GetComponent<PressureButtonStateResolver>();
        if (_focusOnActivation == null) _focusOnActivation = GetComponent<FocusOnActivation>();
    }

#if UNITY_EDITOR
    private void OnValidate() => ResolveReferences();
#endif
}
