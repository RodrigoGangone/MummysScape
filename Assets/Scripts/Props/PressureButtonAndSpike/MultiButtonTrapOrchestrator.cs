using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Conserva las conexiones antiguas. Spears suma estos botones junto a los conectados directamente.
/// Otros ISpikeTrapController conservan la resolución por estado predominante.
/// </summary>
[DisallowMultipleComponent]
public sealed class MultiButtonTrapOrchestrator : MonoBehaviour
{
    [Header("Botones conectados (compatibilidad)")]
    [SerializeField] private PressureButtonStateResolver[] _buttonResolvers;
    [Header("Lanza de destino")]
    [SerializeField] private MonoBehaviour _spikeTrapTarget;

    private readonly HashSet<PressureButtonStateResolver> _registeredButtons = new HashSet<PressureButtonStateResolver>();
    private SpikeTrapController _registeredTrap;
    private bool _connectionsChanged;
    private bool _legacyDirty;

    private void OnEnable() => RegisterConnections();

    private void Update()
    {
        if (!_connectionsChanged) return;
        _connectionsChanged = false;
        UnregisterConnections();
        RegisterConnections();
    }

    private void RegisterConnections()
    {
        _registeredTrap = _spikeTrapTarget as SpikeTrapController;
        if (_buttonResolvers != null)
        {
            foreach (PressureButtonStateResolver button in _buttonResolvers)
            {
                if (button == null || !_registeredButtons.Add(button)) continue;
                if (_registeredTrap != null) _registeredTrap.RegisterButton(button, this);
                else button.TrapStateChanged += MarkLegacyDirty;
            }
        }
        _legacyDirty = true;
    }

    private void MarkLegacyDirty(PressureButtonState state) => _legacyDirty = true;

    private void LateUpdate()
    {
        if (!_legacyDirty || _spikeTrapTarget == null || _spikeTrapTarget is SpikeTrapController ||
            _spikeTrapTarget is not ISpikeTrapController controller) return;
        _legacyDirty = false;
        PressureButtonState highest = PressureButtonState.Released;
        foreach (PressureButtonStateResolver button in _registeredButtons)
            if (button != null && button.isActiveAndEnabled && button.TrapState > highest)
                highest = button.TrapState;
        controller.SetState(highest == PressureButtonState.FullyPressed ? SpikeTrapState.Lowered
            : highest == PressureButtonState.HalfPressed ? SpikeTrapState.HalfRaised : SpikeTrapState.Raised);
    }

    private void OnDisable() => UnregisterConnections();

    private void UnregisterConnections()
    {
        if (_registeredTrap != null) _registeredTrap.UnregisterButtons(this);
        foreach (PressureButtonStateResolver button in _registeredButtons)
            if (button != null) button.TrapStateChanged -= MarkLegacyDirty;
        _registeredButtons.Clear();
        _registeredTrap = null;
    }

    private void OnValidate() => _connectionsChanged = true;
}
