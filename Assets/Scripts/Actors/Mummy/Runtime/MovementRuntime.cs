using UnityEngine;
using static PlayerEnum;

/// <summary>
/// MovementRuntime
/// Ajusta MoveSpeed/TurnSpeed según Size. Se inicializa con el Size del Model,
/// y luego escucha GameEvent OnSizeChanged para cambios en runtime.
/// </summary>
public sealed class MovementRuntime : MonoBehaviour
{
    [SerializeField] private MovementBySizeConfig _config;

    public float MoveSpeed { get; private set; }
    public float TurnSpeed { get; private set; }

    private PlayerModel _model;

    public void Bind(PlayerModel model)
    {
        _model = model;
        Recompute(_model.Size);
    }

    private void Recompute(PlayerSize size)
    {
        _config.Get(size, out var mv, out var turn);
        MoveSpeed = mv;
        TurnSpeed = turn;
    }
    
    private void OnEnable()
    {
        GameEventManager.Instance.playerEvents.OnSizeChanged
            .Register<PlayerSize>(Recompute);
    }

    private void OnDisable()
    {
        GameEventManager.Instance.playerEvents.OnSizeChanged
            .Unregister<PlayerSize>(Recompute);
    }
}