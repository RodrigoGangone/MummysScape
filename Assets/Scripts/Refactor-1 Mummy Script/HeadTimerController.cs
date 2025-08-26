using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using static PlayerEnum;

/// <summary>
/// HeadTimerController
/// Inicia un timer al entrar en Head; cancela al salir. Actualiza UI (Image Filled).
/// Al expirar, invoca _onExpired (asigná "matar/resetear" a la momia).
/// </summary>
public sealed class HeadTimerController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private PlayerModel _model;     // asignaló desde el Controller
    [SerializeField] private TimerService _timers;   // en el mismo prefab o en escena

    [Header("UI")]
    [SerializeField] private Image _fill;            // Image Fill (radial/horizontal)
    [SerializeField] private Sprite _spriteHead;
    [SerializeField] private Sprite _spriteNormalOrSmall;

    [Header("Config")]
    [SerializeField] private float _headSeconds = 10f;

    [Header("Events")]
    [SerializeField] private UnityEvent _onExpired;

    private TimerService.Handle _handle;

    private void OnEnable()
    {
        if (_model != null) _model.OnSizeChanged += HandleSize;
        HandleSize(_model != null ? _model.Size : PlayerSize.Normal);
    }

    private void OnDisable()
    {
        if (_model != null) _model.OnSizeChanged -= HandleSize;
        _timers?.Cancel(_handle);
    }

    private void HandleSize(PlayerSize s)
    {
        // Sprite según estado
        if (_fill)
        {
            _fill.sprite = s == PlayerSize.Head ? _spriteHead : _spriteNormalOrSmall;
            _fill.fillAmount = 1f;
        }

        // Timer
        _timers?.Cancel(_handle);
        if (s == PlayerSize.Head)
        {
            _handle = _timers.StartTimer(
                _headSeconds,
                onTick: remaining => { if (_fill) _fill.fillAmount = remaining / _headSeconds; },
                onComplete: () => { if (_fill) _fill.fillAmount = 0f; _onExpired?.Invoke(); }
            );
        }
    }
}