using System;
using UnityEngine;

/// <summary>
/// Adapta el TimerService existente a la retención del botón y expone progreso normalizado,
/// cancelación segura y una única ejecución activa por instancia.
/// </summary>
[DisallowMultipleComponent]
public sealed class PressureButtonHoldTimer : MonoBehaviour
{
    [Header("Referencias internas")]
    [SerializeField] private TimerService _timerService;
    [Header("Retención del botón")]
    [Tooltip("Segundos que mantiene la pulsación completa después de perder peso.")]
    [SerializeField, Min(0.01f)] private float _duration = 10f;

    private TimerService.Handle _handle;
    private float _runningDuration;

    public bool IsRunning { get; private set; }
    public float Progress { get; private set; }
    public float Duration => _duration;

    public event Action<float> ProgressChanged;
    public event Action Completed;
    public event Action Cancelled;

    private void Awake()
    {
        if (_timerService == null) _timerService = GetComponent<TimerService>();
    }

    private void Update()
    {
        if (IsRunning && (_timerService == null || !_timerService.isActiveAndEnabled ||
            _handle == null || !_handle.IsActive)) Cancel();
    }

    public bool StartTimer()
    {
        if (IsRunning || !isActiveAndEnabled)
        {
            return false;
        }

        if (_timerService == null || !_timerService.isActiveAndEnabled)
        {
            Debug.LogError($"{nameof(PressureButtonHoldTimer)} requiere una referencia a {nameof(TimerService)}.", this);
            return false;
        }

        IsRunning = true;
        _runningDuration = _duration;
        SetProgress(0f);

        _handle = _timerService.StartTimer(
            _runningDuration,
            onTick: HandleTick,
            onComplete: HandleCompleted);

        return true;
    }

    public void Cancel()
    {
        bool wasRunning = IsRunning;
        if (_timerService != null)
        {
            _timerService.Cancel(_handle);
        }

        IsRunning = false;
        _handle = default;
        SetProgress(0f);
        if (wasRunning) Cancelled?.Invoke();
    }

    public void ResetProgress()
    {
        SetProgress(0f);
    }

    private void HandleTick(float remainingSeconds)
    {
        if (!IsRunning)
        {
            return;
        }

        float normalized = _runningDuration <= Mathf.Epsilon
            ? 1f
            : 1f - Mathf.Clamp01(remainingSeconds / _runningDuration);

        SetProgress(normalized);
    }

    private void HandleCompleted()
    {
        if (!IsRunning)
        {
            return;
        }

        IsRunning = false;
        _handle = default;
        SetProgress(1f);
        Completed?.Invoke();
    }

    private void SetProgress(float value)
    {
        value = Mathf.Clamp01(value);
        if (Mathf.Approximately(Progress, value))
        {
            return;
        }

        Progress = value;
        ProgressChanged?.Invoke(Progress);
    }

    private void OnDisable()
    {
        Cancel();
    }

    private void OnDestroy()
    {
        Cancel();
    }

    private void OnValidate()
    {
        _duration = Mathf.Max(0.01f, _duration);
        if (!Application.isPlaying && _timerService == null) _timerService = GetComponent<TimerService>();
    }
}
