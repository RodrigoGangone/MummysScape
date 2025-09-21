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
        bool aHas = wm.Config.PrimarySkill;
        bool bHas = wm.Config.SecondarySkill;
        
        bool aReady = runtimePrimary != null && runtimePrimary.IsReady(Time.time, wm.Config, wm.StageIndex);
        bool bReady = runtimeSecondary != null && runtimeSecondary.IsReady(Time.time, wm.Config, wm.StageIndex);

        //f (wm.HasLineOfSight)
        //
            if (aReady) return "Primary";
            if (bReady) return "Secondary";

            // Tenés LoS pero no hay skills listas:
            Debug.Log($"[GOAP] LoS=TRUE, Aready={aReady}, Bready={bReady} → Idle (cooldowns/condiciones)");
            return "Idle";
        

        // No hay LoS: explícitalo
        Debug.Log("[GOAP] LoS=FALSE → Idle");
        return "Idle";
    }
}
