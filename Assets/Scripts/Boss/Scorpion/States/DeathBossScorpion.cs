using Unity.VisualScripting;
using static Utils;

public class DeathBossScorpion : State
{
    private Scorpion _scorpion;

    public DeathBossScorpion(Scorpion scorpion)
    {
        _scorpion = scorpion;
    }

    public override void OnEnter()
    {
        _scorpion._anim.SetTrigger(DEATH_ANIM_SCORPION);
        _scorpion.viewScorpion.SetActive(false);
    }

    public override void OnUpdate()
    {
    }

    public override void OnFixedUpdate()
    {
    }

    public override void OnExit()
    {
    }
}