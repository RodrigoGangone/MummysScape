using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Planner GOAP mínimo: devuelve una intención simbólica que el BossActor mapea a la FSM.
/// </summary>
// GoapBrain.cs
public sealed class GoapBrain
{
    public string DecideNextIntent(in WorldModel wm, IBossContext ctx, BossSkillSO runtimePrimary, BossSkillSO runtimeSecondary)
    {
        bool aValid = runtimePrimary != null && runtimePrimary.CanExecute(wm, ctx, Time.time);
        bool bValid = runtimeSecondary != null && runtimeSecondary.CanExecute(wm, ctx, Time.time);

        if (aValid && bValid) 
            return "Primary"; 

        if (aValid)
            return "Primary";

        if (bValid)
            return "Secondary";

        Debug.Log($"[GOAP] A y B no disponibles → Idle");
        return "Idle";
    }

}
