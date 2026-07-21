using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Orquesta el encendido de varios FirePillar por pasos. Cada paso puede encender uno o varios pilares a la vez,
/// y luego esperar un intervalo configurable antes de continuar con el siguiente grupo. Se dispara al entrar el Player en su trigger.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public sealed class FirePillarHandler : MonoBehaviour
{
    [Serializable]
    private sealed class FirePillarStep
    {
        [SerializeField] private string _name = "Step";
        [SerializeField] private List<FirePillar> _pillars = new();
        [SerializeField, Min(0f)] private float _delayAfterStep = 1f;

        public string Name => _name;
        public IReadOnlyList<FirePillar> Pillars => _pillars;
        public float DelayAfterStep => _delayAfterStep;
    }

    [Header("Trigger")]
    [SerializeField] private string _playerLayerName = "Player";
    [SerializeField] private bool _triggerOnce = true;
    [SerializeField] private bool _restartSequenceIfTriggeredAgain;

    [Header("Sequence")]
    [SerializeField] private List<FirePillarStep> _steps = new();

    [Header("Debug")]
    [SerializeField] private bool _logSequence;

    private Coroutine _sequenceRoutine;
    private Collider _triggerCollider;
    private int _playerLayer;
    private bool _wasTriggered;

    private void Awake()
    {
        _triggerCollider = GetComponent<Collider>();
        _triggerCollider.isTrigger = true;

        _playerLayer = LayerMask.NameToLayer(_playerLayerName);
        if (_playerLayer < 0)
            Debug.LogWarning($"{nameof(FirePillarHandler)}: no existe la layer '{_playerLayerName}'. Revisá Project Settings > Tags and Layers.", this);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsPlayerLayer(other))
            return;

        if (_triggerOnce && _wasTriggered)
            return;

        PlaySequence();
    }

    /// <summary>
    /// Ejecuta la secuencia configurada desde el Inspector. Puede llamarse desde eventos, botones o Timeline.
    /// </summary>
    [ContextMenu("Play Sequence")]
    public void PlaySequence()
    {
        if (_sequenceRoutine != null)
        {
            if (!_restartSequenceIfTriggeredAgain)
                return;

            StopCoroutine(_sequenceRoutine);
        }

        _sequenceRoutine = StartCoroutine(PlaySequenceRoutine());
    }

    /// <summary>
    /// Detiene la secuencia actual sin apagar los pilares ya encendidos.
    /// </summary>
    [ContextMenu("Stop Sequence")]
    public void StopSequence()
    {
        if (_sequenceRoutine == null)
            return;

        StopCoroutine(_sequenceRoutine);
        _sequenceRoutine = null;
    }

    /// <summary>
    /// Apaga todos los pilares referenciados y permite volver a disparar el trigger.
    /// </summary>
    [ContextMenu("Reset All Pillars")]
    public void ResetAllPillars()
    {
        StopSequence();

        foreach (FirePillarStep step in _steps)
        {
            foreach (FirePillar pillar in step.Pillars)
                pillar?.Extinguish();
        }

        _wasTriggered = false;
    }

    private IEnumerator PlaySequenceRoutine()
    {
        _wasTriggered = true;

        for (int i = 0; i < _steps.Count; i++)
        {
            FirePillarStep step = _steps[i];

            if (_logSequence)
                Debug.Log($"{nameof(FirePillarHandler)}: encendiendo {step.Name} ({i + 1}/{_steps.Count}).", this);

            foreach (FirePillar pillar in step.Pillars)
                pillar?.Ignite();

            if (step.DelayAfterStep > 0f && i < _steps.Count - 1)
                yield return new WaitForSeconds(step.DelayAfterStep);
        }

        _sequenceRoutine = null;
    }

    private bool IsPlayerLayer(Collider other)
    {
        if (_playerLayer < 0 || other == null)
            return false;

        return other.gameObject.layer == _playerLayer || other.transform.root.gameObject.layer == _playerLayer;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        Collider trigger = GetComponent<Collider>();
        if (trigger != null)
            trigger.isTrigger = true;
    }
#endif
}
