using UnityEngine;
using static PlayerEnum;

public sealed class WalkState : State
{
    private readonly PlayerContext _ctx;
    
    // Distancia extra para detectar colisión antes de tocarla
    private const float CollisionBuffer = 0.1f; 

    public WalkState(PlayerContext ctx) => _ctx = ctx;

    public override void OnEnter()
    {
        _ctx.View.Animator.SetBool("Walk", true);
        
        // Matamos la inercia física al entrar para tener control total inmediato
        _ctx.Rb.velocity = Vector3.zero; 
    }

    public override void OnExit()
    {
        _ctx.View.Animator.SetBool("Walk", false);
    }

    public override void OnUpdate()
    {
        Vector2 mv = _ctx.Input.Move;
        
        // Si no hay input, frenamos inmediatamente (sin inercia)
        if (mv.sqrMagnitude < 0.001f) return;

        Vector3 dir = _ctx.CameraRelativeDir(mv.x, mv.y);
        float moveAmount = _ctx.MoveSpeed * Time.deltaTime;

        // --- COLISION MANUAL ---
        // Lanzamos un rayo desde el centro (o pies) hacia donde queremos ir.
        // Si hay pared cerca, no movemos el transform.
        //bool canMove = !Physics.Raycast(_ctx.Tf.position + Vector3.up * 0.5f, dir, moveAmount + CollisionBuffer);
        //
        //if (canMove)
        //{
        _ctx.Tf.position += dir * moveAmount;
        //}

        // --- ROTACIÓN ---
        Quaternion targetRot = Quaternion.LookRotation(dir, Vector3.up);
        _ctx.Tf.rotation = Quaternion.Slerp(_ctx.Tf.rotation, targetRot, _ctx.TurnSpeed * Time.deltaTime);

        _ctx.View?.SetMoveSpeedVisual(1f);
    }

    public override void OnFixedUpdate()
    {
        // Chequeo de suelo: Si deja de haber suelo, pasamos a Fall
        // (Necesario porque al mover por transform, el Rigidbody no cae solo a veces)
        if (!_ctx.IsGrounded())
        {
            StateMachine.ChangeState(PlayerStateId.Fall);
        }
    }
}