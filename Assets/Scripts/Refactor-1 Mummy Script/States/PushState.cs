using UnityEngine;

/// <summary>
/// PushState
/// Empuja una caja en X/Z con snap suave hacia el plano de la cara.
/// Permanece mientras el input siga empujando; sale si soltás, invertís o girás demasiado.
/// </summary>
public sealed class PushState : State
{
    private readonly PlayerContext _ctx;

    [SerializeField] private float _dead = 0.15f;          // deadzone de input
    [SerializeField] private float _snapStrength = 12f;    // fuerza de snap al plano
    [SerializeField] private float _maxYawExitDeg = 45f;   // giro para salir

    private IPushable _target;
    private PushInfo _info;
    private Vector3 _snapPlanePoint;

    public PushState(PlayerContext ctx) => _ctx = ctx;

    public override void OnEnter()
    {
        Debug.Log("PushState!");

        if (!_ctx.TryGetPushTarget(out _target, out _info)) return;
        if (!_target.TryAcquire(_ctx.Tf)) { _target = null; return; }

        // Guardamos el punto del plano de contacto (cara de la caja)
        _snapPlanePoint = _info.SnapPoint;

        // NO hard-snap. Sólo orientamos al player mirando a la caja.
        _ctx.Tf.forward = -_info.FaceNormal;

        _target.OnPushStart(_info);
    }

    public override void OnUpdate()
    {
        
    }

    public override void OnFixedUpdate()
    {
        if (_target == null) return;

        // --- Soft-snap al plano de la cara ---
        // d = componente del vector (player - plano) sobre la normal de la cara
        Vector3 toPlane = Vector3.Project(_ctx.Rb.position - _snapPlanePoint, _info.FaceNormal);
        Vector3 correction = -toPlane * Mathf.Min(_snapStrength * Time.fixedDeltaTime, 1f);
        _ctx.Rb.MovePosition(_ctx.Rb.position + correction);

        // --- Input proyectado sobre el eje de empuje ---
        Vector2 mv = _ctx.Input.Move;
        Vector3 wishDir = _ctx.CameraRelativeDir(mv.x, mv.y);
        float along = Vector3.Dot(wishDir, _info.Axis); // [-1..1]

        bool stop      = Mathf.Abs(along) <= _dead;
        bool opposite  = along < -_dead;
        bool grounded  = _ctx.IsGrounded();

        // También salir si giraste "lejos" de la cara
        float facing = Vector3.Dot(_ctx.Tf.forward, -_info.FaceNormal); // 1 = mirando a la caja
        bool turnedAway = Mathf.Acos(Mathf.Clamp(facing, -1f, 1f)) * Mathf.Rad2Deg > _maxYawExitDeg;

        if (!grounded || stop || opposite || turnedAway)
        {
            _ctx.Rb.velocity = new Vector3(0f, _ctx.Rb.velocity.y, 0f);
            _ctx.View?.SetMoveSpeedVisual(0f);
            _target.OnPushEnd(); _target.Release(_ctx.Tf); _target = null;
            StateMachine.ChangeState(PlayerEnum.PlayerStateId.Idle);
            return;
        }

        // Nota: al empujar "hacia la caja" queremos que la caja se mueva "lejos del player".
        // Como Axis = FaceNormal, invertimos el signo para que empujar hacia adelante (wishDir ~ -FaceNormal)
        // produzca velocidad +FaceNormal.
        float signed = -Mathf.Clamp(along, -1f, 1f);

        _target.OnPushUpdate(_info, signed, _ctx.MoveSpeed);
        _ctx.View?.SetMoveSpeedVisual(Mathf.Abs(signed));
    }

    public override void OnExit()
    {
        if (_target != null) { _target.OnPushEnd(); _target.Release(_ctx.Tf); _target = null; }
        _ctx.View?.SetMoveSpeedVisual(0f);
    }
}