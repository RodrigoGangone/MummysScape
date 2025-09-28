using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Boss/Skills/Scorpion/Stone Skill")]
public class BS_Stone : BossSkillSO
{
    protected override void Execute(in WorldModel wm, IBossContext ctx)
    {
        Debug.Log("Stone Skill");
    }
}