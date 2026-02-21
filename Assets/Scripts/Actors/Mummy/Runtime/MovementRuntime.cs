using UnityEngine;
using static PlayerEnum;

/// <summary> 
/// Adaptador de Movimiento: Ajusta dinámicamente la velocidad de desplazamiento y rotación del jugador 
/// en función de su tamaño actual (PlayerSize). Se sincroniza con el PlayerModel 
/// y reacciona en tiempo real a cambios de escala mediante el sistema de eventos globales.
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
    
    private void OnEnable() => GameEventManager.Instance.playerEvents.OnSizeChanged.Register<PlayerSize>(Recompute);

    private void OnDisable() => GameEventManager.Instance.playerEvents.OnSizeChanged.Unregister<PlayerSize>(Recompute);
    
}