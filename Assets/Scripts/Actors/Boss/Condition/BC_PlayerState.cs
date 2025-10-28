using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Boss/Conditions/Player State")]
public class BC_PlayerState : SkillConditionSO
{
    [SerializeField] private string[] allowedStates;

    public override bool Evaluate(in WorldModel wm, IBossContext ctx)
    {
        Debug.Log($"CurrentState Mummy {ctx.Player.StateMachine.getCurrentState()}");
        
        return Array.IndexOf(allowedStates, ctx.Player.StateMachine.getCurrentState()) >= 0;
    }
}