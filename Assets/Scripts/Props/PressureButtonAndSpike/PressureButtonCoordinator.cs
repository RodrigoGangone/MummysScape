using System.Collections.Generic;
using UnityEngine;

/// <summary>Conecta este botón a sus lanzas y actualiza la placa visual.</summary>
[DisallowMultipleComponent]
public sealed class PressureButtonCoordinator : MonoBehaviour
{
    [Header("Referencias internas")]
    [SerializeField] private PressureButtonStateResolver _stateResolver;
    [SerializeField] private PressureButtonPlateMover _plateMover;
    [Header("Lanzas conectadas")]
    [Tooltip("Arrastrá el SpikeTrapController de cada instancia de Spears. Cada botón se suma una sola vez por lanza.")]
    [SerializeField] private MonoBehaviour[] _spikeTrapTargets;

    private readonly HashSet<SpikeTrapController> _registeredTraps = new HashSet<SpikeTrapController>();
    private bool _connectionsChanged;

    private void Awake() => ResolveReferences();

    private void OnEnable()
    {
        ResolveReferences();
        if (_stateResolver != null) _stateResolver.EffectiveStateChanged += ApplyState;
        RegisterConnections();
        if (_stateResolver != null) ApplyState(_stateResolver.EffectiveState);
    }

    private void Start()
    {
        if (_stateResolver != null) ApplyState(_stateResolver.EffectiveState);
    }

    private void Update()
    {
        if (!_connectionsChanged) return;
        _connectionsChanged = false;
        UnregisterConnections();
        RegisterConnections();
        if (_stateResolver != null) ApplyState(_stateResolver.EffectiveState);
    }

    private void OnDisable()
    {
        if (_stateResolver != null) _stateResolver.EffectiveStateChanged -= ApplyState;
        UnregisterConnections();
    }

    private void RegisterConnections()
    {
        if (_stateResolver == null || _spikeTrapTargets == null) return;
        foreach (MonoBehaviour target in _spikeTrapTargets)
        {
            if (target is SpikeTrapController trap && _registeredTraps.Add(trap))
                trap.RegisterButton(_stateResolver, this);
        }
    }

    private void UnregisterConnections()
    {
        foreach (SpikeTrapController trap in _registeredTraps)
            if (trap != null) trap.UnregisterButtons(this);
        _registeredTraps.Clear();
    }

    private void ApplyState(PressureButtonState state)
    {
        if (_plateMover != null) _plateMover.SetState(state);
        if (_spikeTrapTargets == null) return;
        SpikeTrapState trapState = state == PressureButtonState.FullyPressed ? SpikeTrapState.Lowered
            : state == PressureButtonState.HalfPressed ? SpikeTrapState.HalfRaised : SpikeTrapState.Raised;
        foreach (MonoBehaviour target in _spikeTrapTargets)
        {
            // Compatibilidad con otros controladores que sólo implementan el contrato de estados.
            if (target != null && target is not SpikeTrapController && target is ISpikeTrapController controller)
                controller.SetState(trapState);
        }
    }

    private void ResolveReferences()
    {
        if (_stateResolver == null) _stateResolver = GetComponent<PressureButtonStateResolver>();
        if (_plateMover == null) _plateMover = GetComponent<PressureButtonPlateMover>();
    }

    private void OnValidate()
    {
        if (!Application.isPlaying) ResolveReferences();
        _connectionsChanged = true;
    }
}
