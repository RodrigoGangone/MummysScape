using UnityEngine;
using static PlayerEnum;

/// <summary>
/// MovementRuntime
/// Mantiene MoveSpeed/TurnSpeed actuales según Size del PlayerModel.
/// Llamar a Bind(model) desde el PlayerController.
/// </summary>
public sealed class MovementRuntime : MonoBehaviour
{
    [SerializeField] private MovementBySizeConfig _config;

    public float MoveSpeed { get; private set; }
    public float TurnSpeed { get; private set; }

    private PlayerModel _model;

    public void Bind(PlayerModel model)
    {
        if (_model != null) _model.OnSizeChanged -= Recompute;
        _model = model;
        Recompute(_model.Size);
        _model.OnSizeChanged += Recompute;
    }

    private void OnDestroy()
    {
        if (_model != null) _model.OnSizeChanged -= Recompute;
    }

    private void Recompute(PlayerSize size)
    {
        _config.Get(size, out var mv, out var turn);
        MoveSpeed = mv;
        TurnSpeed = turn;
    }
}