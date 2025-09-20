using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Boss/Skills/Scorpion/Stone Attack (A)")]
public class Scorpion_SkillA : BossSkillSO
{
    protected override void Execute(in WorldModel wm, IBossContext ctx)
    {
        Debug.Log("Scorpion - Skill A");
    }
}
