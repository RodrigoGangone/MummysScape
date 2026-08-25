/// <summary>
/// Estado de Entrada: Controla la secuencia de introducción del Boss, activando el foco 
/// cinematográfico de cámara al inicio del encuentro.
/// </summary>

public sealed class BS_Entry : State
{
    private readonly BossActor _actor;
    public BS_Entry(BossActor actor) { _actor = actor; }

    public override void OnEnter() {}
    public override void OnUpdate() { }
    public override void OnFixedUpdate() { }
    public override void OnExit() { }
}