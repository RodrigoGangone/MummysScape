using UnityEngine;

/// <summary>
/// Propaga cada estado efectivo del botón a su placa visual y a una o varias trampas mediante
/// contratos desacoplados, iniciando todos los cambios dentro de la misma actualización lógica.
/// </summary>
[DisallowMultipleComponent]
public sealed class PressureButtonCoordinator : MonoBehaviour
{
    [SerializeField] private PressureButtonStateResolver _stateResolver;
    [SerializeField] private PressureButtonPlateMover _plateMover;
    [SerializeField] private MonoBehaviour[] _spikeTrapTargets;

    private void OnEnable()
    {
        if (_stateResolver != null)
        {
            _stateResolver.EffectiveStateChanged += ApplyState;
        }
    }

    private void Start()
    {
        if (_stateResolver != null)
        {
            ApplyState(_stateResolver.EffectiveState);
        }
    }

    private void OnDisable()
    {
        if (_stateResolver != null)
        {
            _stateResolver.EffectiveStateChanged -= ApplyState;
        }
    }

    private void ApplyState(PressureButtonState state)
    {
        if (_plateMover != null)
        {
            _plateMover.SetState(state);
        }

        SpikeTrapState trapState = MapTrapState(state);

        if (_spikeTrapTargets == null)
        {
            return;
        }

        for (int i = 0; i < _spikeTrapTargets.Length; i++)
        {
            MonoBehaviour target = _spikeTrapTargets[i];
            if (target == null)
            {
                continue;
            }

            if (target is ISpikeTrapController spikeTrapController)
            {
                spikeTrapController.SetState(trapState);
            }
        }
    }

    private static SpikeTrapState MapTrapState(PressureButtonState state)
    {
        return state switch
        {
            PressureButtonState.HalfPressed => SpikeTrapState.HalfRaised,
            PressureButtonState.FullyPressed => SpikeTrapState.Lowered,
            _ => SpikeTrapState.Raised
        };
    }

    private void OnValidate()
    {
        if (_stateResolver == null)
        {
            _stateResolver = GetComponent<PressureButtonStateResolver>();
        }

        if (_plateMover == null)
        {
            _plateMover = GetComponent<PressureButtonPlateMover>();
        }

        if (_spikeTrapTargets == null)
        {
            return;
        }

        for (int i = 0; i < _spikeTrapTargets.Length; i++)
        {
            MonoBehaviour target = _spikeTrapTargets[i];
            if (target != null && target is not ISpikeTrapController)
            {
                Debug.LogWarning(
                    $"'{target.name}' no implementa {nameof(ISpikeTrapController)} y será ignorado.",
                    target);
            }
        }
    }
}
