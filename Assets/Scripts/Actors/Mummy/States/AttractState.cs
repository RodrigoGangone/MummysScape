using UnityEngine;
using static PlayerEnum;

public sealed class AttractState : State, IBandageRestrictor
{
    private readonly PlayerContext _ctx;
    private BoxPushAttract _box;

    // Constantes
    private const float MIN_PULL_SPEED_FLOOR = 0.05f;
    private const float ROT_LERP = 1f;

    // Variable para controlar el delay
    private float _moveUnlockTime;

    private WrapHandler _currentWrapHandler;

    public AttractState(PlayerContext ctx) => _ctx = ctx;

    public override void OnEnter()
    {
        Debug.Log("AttractState!");
        
        _ctx.View.PlaySfx("Shoot");
        
        _ctx.View.Animator.SetBool("PrePull", true);
        
        if (!_ctx.TryGetAttractTarget(out _box))
        {
            StateMachine.ChangeState(PlayerStateId.Idle);
            return;
        }

        // 1. Iniciamos la física de la caja
        _box.SetPushAttractMode(true, true);
        _box.bank.Play3D("MoveBox", _box.transform.position);
        
        // 2. Iniciamos VISUAL de la venda (Reutilizamos la View)
        // Le mandamos el transform de la caja y su posición central
        // Esto asegura que la linea salga de la mano correcta y vaya a la caja
        _ctx.View.StartBandage(_box.transform, _box.transform.position, 0.4f);

        // 3. Calculamos tiempos
        // Esperamos ese tiempo antes de mover la caja
        _moveUnlockTime = Time.time + 0.4f;

        _currentWrapHandler = _box.GetComponent<WrapHandler>();
    }

    public override void OnExit()
    {
        _box?.StopImmediate();
        _box.bank.Stop("MoveBox");
        _box?.SetPushAttractMode(false); // Esto dispara el UnWrap
        _box = null;
        
        // Limpiamos visuales de la venda
        _ctx.View.StopBandage();
        
        _currentWrapHandler.UnWrap();
        
        _ctx.View.Animator.SetBool("Pull", false);
        _ctx.View.Animator.SetBool("PrePull", false);
    }

    public override void OnUpdate() { }

    public override void OnFixedUpdate()
    {
        // --- Chequeos de validación (Salidas) ---
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

        // --- Lógica de Dirección y Rotación ---
        Vector3 playerPos = _ctx.Tf.position;
        Vector3 boxPos    = _box.transform.position;
        Vector3 toPlayer  = new Vector3(playerPos.x - boxPos.x, 0f, playerPos.z - boxPos.z);
        Vector3 dirPull   = toPlayer.sqrMagnitude > 0.0001f ? toPlayer.normalized : Vector3.zero;

        // Siempre rotamos al player hacia la caja
        Vector3 toBox = -dirPull;
        if (toBox.sqrMagnitude > 0.0001f)
        {
            var targetRot = Quaternion.LookRotation(toBox, Vector3.up);
            var smooth    = Quaternion.Slerp(_ctx.Rb.rotation, targetRot, _ctx.TurnSpeed * ROT_LERP * Time.fixedDeltaTime);
            _ctx.Rb.MoveRotation(smooth);
        }

        // --- Lógica de Espera (Wait for Wrap Visual) ---
        if (Time.time < _moveUnlockTime)
        {
            _ctx.View.Animator.SetBool("Pull", true);
            _box.StopImmediate(); 
            return; 
        }

        // --- Lógica de Movimiento (Solo se ejecuta tras finalizar el Wrap Visual) ---
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