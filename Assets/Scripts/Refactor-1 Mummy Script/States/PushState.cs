using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// PushState
/// Empuja una caja en X/Z con snap lateral.
/// Permanece mientras el input proyectado sobre el eje de empuje tenga el mismo signo.
/// Sale si el input cambia de signo o cae a deadzone.
/// </summary>
public class PushState : State
{
    private readonly PlayerContext _ctx;
    private readonly float _dead = 0.15f;

    private IPushable _target;
    private PushInfo _info;
    
    public PushState(PlayerContext ctx) => _ctx = ctx;

    public override void OnEnter()
    {
        Debug.Log("PushState!");
        
        // Resolver target (debe existir por el Driver)
        if (!_ctx.TryGetPushTarget(out _target, out _info)) return;

        // Lock exclusivo
        if (!_target.TryAcquire(_ctx.Tf))
        {
            _target = null;
            return;
        }

        // Snap del player a la cara
        var tf = _ctx.Tf;
        Vector3 pos = tf.position;
        pos.y = tf.position.y; // no cambiamos altura
        // colocá el player a una pequeña distancia de la cara
        tf.position = new Vector3(_info.SnapPoint.x, pos.y, _info.SnapPoint.z);

        // Orientar al player mirando hacia la caja
        tf.forward = -_info.FaceNormal;

        _target.OnPushStart(_info);
    }

    public override void OnUpdate()
    {
    }

    public override void OnFixedUpdate()
    {
        if (_target == null) return;

        // input del jugador en espacio de cámara, proyectado al eje permitido
        Vector2 mv = _ctx.Input.Move;
        Vector3 wishDir = _ctx.CameraRelativeDir(mv.x, mv.y);
        var along = Vector3.Dot(wishDir, _info.Axis); // [-1..1] sentido del empuje

        var stop = Mathf.Abs(along) <= _dead;                 // dejó de empujar
        var opposite = along < -_dead;                        // va en sentido contrario
        var grounded = _ctx.IsGrounded();                     // si se cae, cancelamos

        if (!grounded || stop || opposite)
        {
            _ctx.Rb.velocity = new Vector3(0f, _ctx.Rb.velocity.y, 0f);
            _ctx.View?.SetMoveSpeedVisual(0f);
            _target.OnPushEnd();
            _target.Release(_ctx.Tf);
            _target = null;

            // Volver a Idle/Walk (el Guard permite)
            StateMachine.ChangeState(PlayerEnum.PlayerStateId.Idle);
            return;
        }

        // Actualizar empuje
        _target.OnPushUpdate(_info, Mathf.Clamp(along, -1f, 1f), _ctx.MoveSpeed);
        _ctx.View?.SetMoveSpeedVisual(Mathf.Abs(along));
    }

    public override void OnExit()
    {
        if (_target != null)
        {
            _target.OnPushEnd();
            _target.Release(_ctx.Tf);
            _target = null;
        }
        _ctx.View?.SetMoveSpeedVisual(0f);
    }
}
