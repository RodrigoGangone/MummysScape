using UnityEngine;
using static PlayerEnum.PlayerStateId;

/// <summary>
/// PlayerInputStateDriver
/// Agrega "gating" para entrar a Push: exige alineación al eje y mirar razonablemente al centro de la caja.
/// Evita reentradas indeseadas cuando el jugador está intentando salir girando.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(StateMachinePlayer))]
public class PlayerInputStateDriver : MonoBehaviour
{
    [Header("Tuning")]
    [SerializeField, Min(0f)] private float _moveDeadZone = 0.1f;

    [Header("Push Gating")]
    [SerializeField, Range(0f,1f)] private float _enterPushAxisDot = 0.6f; // alineación mínima al eje
    [SerializeField, Range(0f,90f)] private float _enterPushAngleDeg = 35f; // mirar aprox hacia el centro

    private StateMachinePlayer _sm;
    private PlayerContext _ctx;
    private IPlayerInput _input;

    public void Bind(PlayerContext ctx, StateMachinePlayer sm)
    {
        _ctx = ctx;
        _sm = sm;
        _input = ctx.Input;
    }

    private void Awake()
    {
        if (_sm == null) _sm = GetComponent<StateMachinePlayer>();
    }

    private void Update()
    {
        if (_ctx == null || _sm == null || _input == null) return;

        var mv = _input.Move;

        // 1) Caída
        if (!_ctx.IsGrounded())
        {
            _sm.ChangeState(Fall);
            return;
        }

        // 2) Acciones instantáneas (ejemplos)
        if (_input.ConsumeSpaceDown())
        {
            if (_sm.ChangeState(Smash)) return;
        }

        // 3) Edge: E / Q
        if (_input.ConsumeShootDown())
        {
            if (_sm.ChangeState(Shoot)) return;
        }
        if (_input.ConsumeDropDown())
        {
            if (_sm.ChangeState(DropBandage)) return;
        }

        // 4) Push con gating: requiere target válido + alineación al eje + ángulo razonable
        bool wantsMove = Mathf.Abs(mv.x) > _moveDeadZone || Mathf.Abs(mv.y) > _moveDeadZone;

        if (wantsMove && _ctx.TryGetPushTarget(out var box, out var face))
        {
            Vector3 desiredDir = _ctx.CameraRelativeDir(mv.x, mv.y);
            Vector3 axis = box.GetPushAxis(face); // eje X/Z desde la cara

            float axisDot = Vector3.Dot(desiredDir, axis);
            float axisOk = axisDot >= _enterPushAxisDot ? 1f : 0f;

            // ángulo al centro de la caja (horizontal)
            Vector3 playerPos = _ctx.Rb.position;
            Vector3 toBox = box.transform.position - playerPos;
            toBox.y = 0f;

            bool angleOk = toBox.sqrMagnitude <= 1e-6f 
                || Vector3.Angle(_ctx.Rb.rotation * Vector3.forward, toBox.normalized) <= _enterPushAngleDeg;

            if (axisOk == 1f && angleOk)
            {
                var current = _sm.CurrentId();
                if (current is PlayerEnum.PlayerStateId id && id == Push)
                {
                    // ya estamos en Push, dejá que el estado gobierne
                    return;
                }

                if (_sm.ChangeState(Push)) return;
            }
        }

        // 5) Walk / Idle
        if (wantsMove)
        {
            _sm.ChangeState(Walk);
        }
        else
        {
            _sm.ChangeState(Idle);
        }
    }
}