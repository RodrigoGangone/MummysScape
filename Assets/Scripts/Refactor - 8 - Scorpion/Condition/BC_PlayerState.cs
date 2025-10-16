using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Boss/Conditions/Player State")]
public class BC_PlayerState : SkillConditionSO
{
    [SerializeField] private string[] allowedStates;

    public override bool Evaluate(in WorldModel wm, IBossContext ctx)
    {
        Debug.Log(ctx.Player._stateMachinePlayer.getCurrentState() + "CurrentState Mummy");
        
        return Array.IndexOf(allowedStates, ctx.Player._stateMachinePlayer.getCurrentState()) >= 0;
    }
}