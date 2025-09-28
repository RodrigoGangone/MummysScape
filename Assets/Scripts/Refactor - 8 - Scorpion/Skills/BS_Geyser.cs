using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Boss/Skills/Scorpion/Geyser Skill")]
public class BS_Geyser : BossSkillSO
{
    protected override void Execute(in WorldModel wm, IBossContext ctx)
    {
        Debug.Log("Geyser Skill");
    }
}