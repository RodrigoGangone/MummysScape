using UnityEngine;
using static PlayerEnum;
/// <summary>
/// WalkState
/// Movimiento con Rigidbody relativo a la cámara. Rotación suave hacia dirección de marcha.
/// </summary>
public sealed class WalkState : State
{
    private readonly PlayerContext _ctx;
    public WalkState(PlayerContext ctx) => _ctx = ctx;

    public override void OnEnter() { }
    public override void OnExit()  { }

    public override void OnUpdate()
    {
        // Q -> DropBandage (si permitido)
        
        if (_ctx.Input.ConsumeDropDown() && SizeRules.Can(_ctx.Model.Size, PlayerActionId.DropBandage))
        { StateMachine.ChangeState(PlayerStateId.Shoot /*placeholder*/); } // <- reemplazar por PlayerStateId.Drop si lo definiste
        // ^^^ ver nota debajo

        // Space -> primero Smash (si estás en Head), sino Attract (si target al frente)
        if (SizeRules.Can(_ctx.Model.Size, PlayerActionId.Smash) && _ctx.Input.ConsumeSmashDown())
        { StateMachine.ChangeState(PlayerStateId.Smash); return; }

        if (SizeRules.Can(_ctx.Model.Size, PlayerActionId.Attract) && _ctx.Input.ConsumeAttractDown())
        {
            // opcional: verificá target antes de entrar
            if (_ctx.TryGetAttractTarget(out _)) { StateMachine.ChangeState(PlayerStateId.Attract /*placeholder*/); } // <- reemplazar por PlayerStateId.Attract
        }

        // E -> Shoot
        if (_ctx.Input.ConsumeShootDown() && SizeRules.Can(_ctx.Model.Size, PlayerActionId.Shoot))
        { StateMachine.ChangeState(PlayerStateId.Shoot); }
    }

    public override void OnFixedUpdate()
    {
        Vector2 mv = _ctx.Input.Move;
        Vector3 dir = _ctx.CameraRelativeDir(mv.x, mv.y);

        if (dir.sqrMagnitude > 0.0001f)
        {
            Vector3 targetPos = _ctx.Rb.position + dir * (_ctx.MoveSpeed * Time.fixedDeltaTime);
            _ctx.Rb.MovePosition(targetPos);

            Quaternion targetRot = Quaternion.LookRotation(dir, Vector3.up);
            Quaternion smoothRot = Quaternion.Slerp(_ctx.Rb.rotation, targetRot, _ctx.TurnSpeed * Time.fixedDeltaTime);
            _ctx.Rb.MoveRotation(smoothRot);

            _ctx.View?.SetMoveSpeedVisual(1f);
        }
        else
        {
            _ctx.View?.SetMoveSpeedVisual(0f);
        }
    }
}