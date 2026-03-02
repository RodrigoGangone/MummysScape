using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using static PlayerEnum;

/// <summary> 
/// Controlador de Tiempo: Gestiona el temporizador y la barra de carga visual cuando el jugador 
/// está en estado "Head", disparando eventos al agotarse el tiempo permitido. 
/// </summary>

public sealed class HeadTimerController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private TimerService _timers;

    [Header("UI")]
    [SerializeField] private Image _fill;
    [SerializeField] private Sprite _spriteHead;
    [SerializeField] private Sprite _spriteNormalOrSmall;

    [Header("Config")]
    [SerializeField] private float _headSeconds = 10f;

    [Header("Events")]
    [SerializeField] private UnityEvent _onExpired;

    private TimerService.Handle _handle;

    public void Bind(PlayerModel model)
    {
        HandleSize(model.Size);
    }

    private void HandleSize(PlayerSize s)
    {
        if (_fill)
        {
            _fill.sprite = s == PlayerSize.Head ? _spriteHead : _spriteNormalOrSmall;
            _fill.fillAmount = 1f;
        }

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
    
    private void OnEnable() => GameEventManager.Instance.playerEvents.OnSizeChanged.Register<PlayerSize>(HandleSize);

    private void OnDisable()
    {
        GameEventManager.Instance.playerEvents.OnSizeChanged.Unregister<PlayerSize>(HandleSize);
        
        _timers?.Cancel(_handle);
    }
}