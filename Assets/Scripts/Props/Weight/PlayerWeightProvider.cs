using UnityEngine;
using static PlayerEnum;

/// <summary>
/// Traduce el tamaño actual del PlayerModel al peso del jugador y escucha el evento existente
/// de cambio de tamaño, evitando polling y una segunda fuente de verdad.
/// </summary>
[DisallowMultipleComponent]
public sealed class PlayerWeightProvider : WeightProviderBehaviour
{
    [SerializeField] private PlayerController _playerController;

    private int _weight;
    private bool _subscribed;

    public override int Weight => _weight;

    private void Awake()
    {
        if (_playerController == null)
        {
            _playerController = GetComponentInParent<PlayerController>();
        }
    }

    private void Start()
    {
        Subscribe();
        RefreshFromModel();
    }

    protected override void OnEnable()
    {
        Subscribe();
        RefreshFromModel();
        base.OnEnable();
    }

    protected override void OnDisable()
    {
        Unsubscribe();
        base.OnDisable();
    }

    private void Subscribe()
    {
        if (_subscribed || GameEventManager.Instance == null)
        {
            return;
        }

        GameEventManager.Instance.playerEvents.OnSizeChanged.Register<PlayerSize>(HandleSizeChanged);
        _subscribed = true;
    }

    private void Unsubscribe()
    {
        if (!_subscribed || GameEventManager.Instance == null)
        {
            return;
        }

        GameEventManager.Instance.playerEvents.OnSizeChanged.Unregister<PlayerSize>(HandleSizeChanged);
        _subscribed = false;
    }

    private void RefreshFromModel()
    {
        if (_playerController == null || _playerController.Ctx == null || _playerController.Ctx.Model == null)
        {
            return;
        }

        ApplyWeight(MapWeight(_playerController.Ctx.Model.Size));
    }

    private void HandleSizeChanged(PlayerSize size)
    {
        ApplyWeight(MapWeight(size));
    }

    private void ApplyWeight(int newWeight)
    {
        newWeight = Mathf.Max(0, newWeight);
        if (_weight == newWeight)
        {
            return;
        }

        _weight = newWeight;
        NotifyWeightChanged();
    }

    private static int MapWeight(PlayerSize size)
    {
        return size switch
        {
            PlayerSize.Normal => 2,
            PlayerSize.Small => 1,
            _ => 0
        };
    }

#if UNITY_EDITOR
    private void Reset()
    {
        _playerController = GetComponentInParent<PlayerController>();
    }
#endif
}
