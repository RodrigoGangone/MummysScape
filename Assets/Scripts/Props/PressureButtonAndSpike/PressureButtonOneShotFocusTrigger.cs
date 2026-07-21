using UnityEngine;

/// <summary>
/// Ejecuta el foco una única vez al entrar por primera vez en HalfPressed y una única vez
/// al entrar por primera vez en FullyPressed. La unicidad se controla por estado y se reinicia
/// solamente al recargar la escena o al invocar ResetOneShotTriggers.
/// </summary>
[DisallowMultipleComponent]
public sealed class PressureButtonOneShotFocusTrigger : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PressureButtonStateResolver _stateResolver;
    [SerializeField] private FocusOnActivation _focusOnActivation;

    [Header("Enabled States")]
    [SerializeField] private bool _activateOnHalfPressed = true;
    [SerializeField] private bool _activateOnFullyPressed = true;

    private bool _halfPressedTriggered;
    private bool _fullyPressedTriggered;

    private void OnEnable()
    {
        if (_stateResolver == null || _focusOnActivation == null)
        {
            Debug.Log(
                $"{nameof(PressureButtonOneShotFocusTrigger)} tiene referencias sin asignar.",
                this);

            return;
        }

        _stateResolver.EffectiveStateChanged += HandleStateChanged;
    }

    private void OnDisable()
    {
        if (_stateResolver != null)
        {
            _stateResolver.EffectiveStateChanged -= HandleStateChanged;
        }
    }

    private void HandleStateChanged(PressureButtonState state)
    {
        switch (state)
        {
            case PressureButtonState.HalfPressed:
                ActivateHalfPressedOnce();
                break;

            case PressureButtonState.FullyPressed:
                ActivateFullyPressedOnce();
                break;
        }
    }

    private void ActivateHalfPressedOnce()
    {
        if (!_activateOnHalfPressed || _halfPressedTriggered)
        {
            return;
        }

        _halfPressedTriggered = true;
        _focusOnActivation.Activate();
    }

    private void ActivateFullyPressedOnce()
    {
        if (!_activateOnFullyPressed || _fullyPressedTriggered)
        {
            return;
        }

        _fullyPressedTriggered = true;
        _focusOnActivation.Activate();
    }

    /// <summary>
    /// Permite volver a habilitar ambos disparos, por ejemplo al reiniciar un puzzle
    /// sin recargar la escena.
    /// </summary>
    public void ResetOneShotTriggers()
    {
        _halfPressedTriggered = false;
        _fullyPressedTriggered = false;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (_stateResolver == null)
        {
            _stateResolver = GetComponent<PressureButtonStateResolver>();
        }

        if (_focusOnActivation == null)
        {
            _focusOnActivation = GetComponent<FocusOnActivation>();
        }
    }
#endif
}
