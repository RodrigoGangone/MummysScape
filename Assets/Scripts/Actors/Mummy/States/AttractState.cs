using UnityEngine;
using static PlayerEnum;

/// <summary> 
/// Estado de Atracción: Ejecuta la mecánica de "tirar" de una caja mediante vendas, coordinando la 
/// animación de envolver (Wrap), el desplazamiento físico del objeto y la rotación del jugador. 
/// </summary>

public sealed class AttractState : State, IBandageRestrictor
{
    private readonly PlayerContext _ctx;
    private BoxPushAttract _box;

    private const float MIN_PULL_SPEED_FLOOR = 0.05f;
    private const float ROT_LERP = 1f;

    private float _moveUnlockTime;

    private WrapHandler _currentWrapHandler;

    public AttractState(PlayerContext ctx) => _ctx = ctx;

    public override void OnEnter()
    {
        _ctx.View.PlaySfx("Shoot");
        
        _ctx.View.Animator.SetBool("PrePull", true);
        
        if (!_ctx.TryGetAttractTarget(out _box))
        {
            StateMachine.ChangeState(PlayerStateId.Idle);
            return;
        }

        _box.SetPushAttractMode(true, true);
        _box.bank.Play3D("MoveBox", _box.transform.position);
        
        _ctx.View.StartBandage(_box.transform, _box.transform.position, 0.4f);

        _moveUnlockTime = Time.time + 0.4f;

        _currentWrapHandler = _box.GetComponent<WrapHandler>();
    }

    public override void OnExit()
    {
        _box?.StopImmediate();
        _box.bank.Stop("MoveBox");
        _box?.SetPushAttractMode(false);
        _box = null;
        
        _ctx.View.StopBandage();
        
        _currentWrapHandler.UnWrap();
        
        _ctx.View.Animator.SetBool("Pull", false);
        _ctx.View.Animator.SetBool("PrePull", false);
    }

    public override void OnUpdate() { }

    public override void OnFixedUpdate()
    {
        if (!_ctx.IsGrounded())
        {
            StateMachine.ChangeState(PlayerStateId.Fall);
            return;
        }
        if (!_ctx.Input.IsSpaceHeld())
        {
            StateMachine.ChangeState(PlayerStateId.Idle);
            return;
        }
        if (_box == null || !_box.IsGroundedForPushAttract())
        {
            StateMachine.ChangeState(PlayerStateId.Idle);
            return;
        }

        Vector3 playerPos = _ctx.Tf.position;
        Vector3 boxPos    = _box.transform.position;
        Vector3 toPlayer  = new Vector3(playerPos.x - boxPos.x, 0f, playerPos.z - boxPos.z);
        Vector3 dirPull   = toPlayer.sqrMagnitude > 0.0001f ? toPlayer.normalized : Vector3.zero;

        Vector3 toBox = -dirPull;
        if (toBox.sqrMagnitude > 0.0001f)
        {
            var targetRot = Quaternion.LookRotation(toBox, Vector3.up);
            var smooth    = Quaternion.Slerp(_ctx.Rb.rotation, targetRot, _ctx.TurnSpeed * ROT_LERP * Time.fixedDeltaTime);
            _ctx.Rb.MoveRotation(smooth);
        }

        if (Time.time < _moveUnlockTime)
        {
            _ctx.View.Animator.SetBool("Pull", true);
            _box.StopImmediate(); 
            return; 
        }

        float min    = _ctx.AttractMinDistance;
        float max    = _ctx.AttractMaxDistance;
        float distXZ = _box.HorizontalDistanceTo(playerPos);

        if (distXZ <= min)
        {
            StateMachine.ChangeState(PlayerStateId.Idle);
            return;
        }

        float t01 = Mathf.InverseLerp(min, max, Mathf.Clamp(distXZ, min, max));
        float curveMul = Mathf.Max(0f, _ctx.AttractSpeedCurve.Evaluate(t01));
        float pullSpeed = Mathf.Max(MIN_PULL_SPEED_FLOOR, _ctx.MoveSpeed * _ctx.AttractSpeedBase * curveMul);

        float maxStep = pullSpeed * Time.fixedDeltaTime;
        float allowed = Mathf.Max(0f, distXZ - min);
        float step    = Mathf.Min(maxStep, allowed);

        if (step > 0f) _box.MoveBy(dirPull * step);
    }
}