using static Utils;

/// <summary> Estado de entrada (intro). </summary>
public sealed class BS_Entry : State
{
    private readonly BossActor _actor;
    public BS_Entry(BossActor actor) { _actor = actor; }

    public override void OnEnter()
    {
        _actor.focus.Activate();
    }

    public override void OnUpdate()
    {
        //var t = _actor.Transform;
        //var p = _actor.Player.Tf.position;
        //p.y = t.position.y;
        //t.LookAt(p);
    }
    public override void OnFixedUpdate() { }
    public override void OnExit() { }
}