using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Boss/Skills/Scorpion/Geyser Attack (B)")]
public class Scorpion_SkillB : BossSkillSO
{
    protected override void Execute(in WorldModel wm, IBossContext ctx)
    {
        Debug.Log("Scorpion - Skill B");
    }
}
