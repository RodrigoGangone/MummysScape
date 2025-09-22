using UnityEngine;

[CreateAssetMenu(menuName = "Boss/Skills/Scorpion/Stone Skill")]
public class StoneSkill : BossSkillSO
{
    protected override void Execute(in WorldModel wm, IBossContext ctx)
    {
        Debug.Log("Stone Skill");
    }
}

[CreateAssetMenu(menuName = "Boss/Skills/Scorpion/Geyser Skill")]
public class GeyserSkill : BossSkillSO
{
    protected override void Execute(in WorldModel wm, IBossContext ctx)
    {
        Debug.Log("Geyser Skill");
    }
}

[CreateAssetMenu(menuName = "Boss/Skills/Other/Tornado Skill")]
public class TornadoSkill : BossSkillSO
{
    protected override void Execute(in WorldModel wm, IBossContext ctx)
    {
        Debug.Log("Tornado Skill");
    }
}

[CreateAssetMenu(menuName = "Boss/Skills/Other/Assault Skill")]
public class AssaultSkill : BossSkillSO
{
    protected override void Execute(in WorldModel wm, IBossContext ctx)
    {
        Debug.Log("Assault Skill");
    }
}
