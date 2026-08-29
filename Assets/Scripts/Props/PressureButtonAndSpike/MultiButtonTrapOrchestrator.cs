using UnityEngine;

/// <summary>
/// Escucha múltiples botones y resuelve el estado de la trampa evaluando 
/// el estado predominante entre todos los emisores.
/// </summary>
[DisallowMultipleComponent]
public sealed class MultiButtonTrapOrchestrator : MonoBehaviour
{
    [Header("Inputs")]
    [SerializeField] private PressureButtonStateResolver[] _buttonResolvers;

    [Header("Output")]
    [SerializeField] private MonoBehaviour _spikeTrapTarget;
    
    private ISpikeTrapController _trapController;

    private void Awake()
    {
        if (_spikeTrapTarget != null)
        {
            _trapController = _spikeTrapTarget as ISpikeTrapController;
        }
    }

    private void OnEnable()
    {
        if (_buttonResolvers == null) return;

        for (int i = 0; i < _buttonResolvers.Length; i++)
        {
            if (_buttonResolvers[i] != null)
            {
                _buttonResolvers[i].EffectiveStateChanged += EvaluateCombinedState;
            }
        }
    }

    private void Start()
    {
        // Forzar una evaluación inicial para establecer el estado de la trampa
        EvaluateCombinedState(PressureButtonState.Released);
    }

    private void OnDisable()
    {
        if (_buttonResolvers == null) return;

        for (int i = 0; i < _buttonResolvers.Length; i++)
        {
            if (_buttonResolvers[i] != null)
            {
                _buttonResolvers[i].EffectiveStateChanged -= EvaluateCombinedState;
            }
        }
    }

    private void EvaluateCombinedState(PressureButtonState _)
    {
        if (_trapController == null) return;

        PressureButtonState highestState = PressureButtonState.Released;

        for (int i = 0; i < _buttonResolvers.Length; i++)
        {
            PressureButtonState state = _buttonResolvers[i].EffectiveState;
            
            if (state == PressureButtonState.FullyPressed)
            {
                highestState = PressureButtonState.FullyPressed;
                break; // Optimización: Si encontramos un FullyPressed, no necesitamos seguir iterando
            }
            
            if (state == PressureButtonState.HalfPressed)
            {
                highestState = PressureButtonState.HalfPressed;
            }
        }

        _trapController.SetState(MapTrapState(highestState));
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
}