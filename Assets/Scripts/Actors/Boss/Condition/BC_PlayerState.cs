using System;
using UnityEngine;

/// <summary>
/// Habilita la skill dependiendo el estado en el que se encuentre el Player
/// </summary>

[CreateAssetMenu(menuName = "Boss/Conditions/Player State")]
public class BC_PlayerState : SkillConditionSO
{
    [SerializeField] private string[] allowedStates;

    public override bool Evaluate(in WorldModel wm, IBossContext ctx) => Array.IndexOf(allowedStates, ctx.Player.StateMachine.getCurrentState()) >= 0;
}