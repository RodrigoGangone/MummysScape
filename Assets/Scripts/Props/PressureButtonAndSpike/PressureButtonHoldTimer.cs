using System;
using UnityEngine;

/// <summary>
/// Adapta el TimerService existente a la retención del botón y expone progreso normalizado,
/// cancelación segura y una única ejecución activa por instancia.
/// </summary>
[DisallowMultipleComponent]
public sealed class PressureButtonHoldTimer : MonoBehaviour
{
    [SerializeField] private TimerService _timerService;
    [SerializeField, Min(0.01f)] private float _duration = 10f;

    private TimerService.Handle _handle;

    public bool IsRunning { get; private set; }
    public float Progress { get; private set; }
    public float Duration => _duration;

    public event Action<float> ProgressChanged;
    public event Action Completed;

    public bool StartTimer()
    {
        if (IsRunning)
        {
            return false;
        }

        if (_timerService == null)
        {
            Debug.LogError($"{nameof(PressureButtonHoldTimer)} requiere una referencia a {nameof(TimerService)}.", this);
            return false;
        }

        IsRunning = true;
        SetProgress(0f);

        _handle = _timerService.StartTimer(
            _duration,
            onTick: HandleTick,
            onComplete: HandleCompleted);

        return true;
    }

    public void Cancel()
    {
        if (_timerService != null)
        {
            _timerService.Cancel(_handle);
        }

        IsRunning = false;
        _handle = default;
        SetProgress(0f);
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

        float normalized = _duration <= Mathf.Epsilon
            ? 1f
            : 1f - Mathf.Clamp01(remainingSeconds / _duration);

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
    }
}
