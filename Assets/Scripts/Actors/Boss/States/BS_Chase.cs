using UnityEngine;

/// <summary>
/// Estado de Persecución: Gestiona el movimiento del Boss hacia el jugador, ajustando su velocidad 
/// según el multiplicador de la fase actual y manteniendo la rotación hacia el objetivo.
/// </summary>

public class BS_Chase : State
{
    private readonly BossActor _actor;
    public BS_Chase(BossActor actor) { _actor = actor; }
    public override void OnEnter() => _actor.Animator?.SetBool("Run", true);
    public override void OnFixedUpdate(){}
    public override void OnExit()  => _actor.Animator?.SetBool("Run", false);
    public override void OnUpdate()
    {
        var t = _actor.Transform;
        Vector3 target = _actor.Player.Tf.position;
        target.y = t.position.y;
        var dir = (target - t.position).normalized;
        var speed = 3f * (_actor.Config.GetStage(_actor.CurrentStageIndex)?.speedMultiplier ?? 1f);
        t.position += dir * (speed * Time.deltaTime);
        t.rotation = Quaternion.Slerp(t.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 8f);
    }
}
