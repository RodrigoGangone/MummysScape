using UnityEngine;

public class SwingState : State
{
    private readonly PlayerContext _ctx;
    private Transform _hookTf;

    private bool _attached;

    public SwingState(PlayerContext ctx) => _ctx = ctx;

    public override void OnEnter()
    {
        Debug.Log("SwingState - Enter");

        if (_ctx.TryGetSwingTarget(out var hookRb))
        {
            _hookTf = hookRb.transform;

            Vector3 worldHookPoint = hookRb.worldCenterOfMass;

            _ctx.View?.SetSwingLineActive(true, _hookTf);

            bool attached = false;

            _ctx.View?.PlayBandageDraw(onAttachMoment: () =>
            {
                if (attached) return;
                attached = true;

                _ctx.SwingHandler.Attach(_ctx.Rb, hookRb, worldHookPoint);
            });
        }
        else
        {
            StateMachine.ChangeState(PlayerEnum.PlayerStateId.Fall);
        }
    }


    public override void OnUpdate() { }

    public override void OnFixedUpdate()
    {
        var rb = _ctx.Rb;
        var joint = _ctx.SwingHandler.SpringJoint;
        if (joint == null) return;

        Vector2 mv = _ctx.Input.Move;
        bool hasInput = mv.sqrMagnitude > 0.0001f;

        Vector3 wishDir = _ctx.CameraRelativeDir(mv.x, mv.y);
        Vector3 ropeDir = _ctx.SwingHandler.GetRopeDirWorld();

        Vector3 tanDir = Vector3.ProjectOnPlane(wishDir, ropeDir).normalized;

        Vector3 currentVel = rb.velocity;
        Vector3 currentTanVel = Vector3.ProjectOnPlane(currentVel, ropeDir);
        float currentSpeed = currentTanVel.magnitude;

        if (hasInput && tanDir.sqrMagnitude > 0f)
        {
            bool isFightingGravity = tanDir.y > 0;
            float forcePower = isFightingGravity ? 4f : 20f;

            float maxSpeed = _ctx.SwingHandler.MaxTangentialSpeed;

            float alignment = currentTanVel.sqrMagnitude > 0.0001f
                ? Vector3.Dot(tanDir, currentTanVel.normalized)
                : 0f;

            if (currentSpeed < maxSpeed || alignment < 0.5f)
            {
                rb.AddForce(tanDir * forcePower, ForceMode.Acceleration);
            }
        }
        else
        {
            _ctx.SwingHandler.HandlePassiveReturn(rb, Time.fixedDeltaTime);
        }

        _ctx.SwingHandler.ClampTangentialSpeed(rb);

        Vector3 face = new Vector3(currentTanVel.x, 0f, currentTanVel.z);
        if (face.sqrMagnitude > 0.001f)
        {
            Quaternion target = Quaternion.LookRotation(face.normalized, Vector3.up);
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, target, 12f * Time.fixedDeltaTime));
        }

        float n = Mathf.Clamp01(currentSpeed / Mathf.Max(0.01f, _ctx.SwingHandler.MaxTangentialSpeed));
        _ctx.View?.SetMoveSpeedVisual(n);
    }

    public override void OnExit()
    {
        _ctx.SwingHandler.Detach();
        _ctx.View?.SetSwingLineActive(false);
        _ctx.View?.CancelBandageDraw();
        _hookTf = null;
    }

}
