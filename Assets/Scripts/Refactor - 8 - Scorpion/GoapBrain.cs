using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// Planner GOAP mínimo: devuelve una intención simbólica que el BossActor mapea a la FSM.
/// </summary>
public sealed class GoapBrain
{
    public string DecideNextIntent(in WorldModel wm, IBossContext ctx)
    {
        if (wm.Config == null) return "Idle";

        bool inSight = wm.DistanceBP <= wm.Config.sightRange && (wm.HasLineOfSight || wm.DistanceBP <= 1.25f);
        if (!inSight) return "Idle"; // o "Search" si luego agregás ese estado

        bool inAttack = wm.DistanceBP <= wm.Config.attackRange;

        bool aReady = wm.Config.SkillA && wm.Config.SkillA.IsReady(Time.time, wm.Config, wm.StageIndex);
        bool bReady = wm.Config.SkillB && wm.Config.SkillB.IsReady(Time.time, wm.Config, wm.StageIndex);

        // Política simple: B cuando está un poco más lejos, A cuando está bien en rango
        if (bReady && wm.DistanceBP <= wm.Config.attackRange * 1.2f) return "SkillB";
        if (aReady && inAttack) return "SkillA";

        if (wm.DistanceBP > wm.Config.attackRange) return "Chase";
        return "Idle";
    }
}
