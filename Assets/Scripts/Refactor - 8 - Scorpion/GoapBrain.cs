using UnityEngine;
using static BossCommonState;

/// <summary>
/// Planner GOAP mínimo: devuelve una intención simbólica que el BossActor mapea a la FSM.
/// </summary>
public sealed class GoapBrain
{
    // GoapBrain.DecideNextIntent
    public BossCommonState DecideNextIntent(in WorldModel wm, IBossContext ctx, BossSkillSO runtimePrimary, BossSkillSO runtimeSecondary)
    {
        // Si el contexto no es un BossActor, no hay decisión posible.
        if (ctx is not BossActor boss) return None;

        //Priorizamos la muerte del Boss por sobre todas las cosas
        if (boss.IsDie)          return Die;

        // Estados que no deben cambiar intención (early-outs).
        if (boss.IsEntry)            return None;
        if (boss.IsExecutingSkill)   return None;

        // Priorizamos daño si aplica (si querés que no interrumpa skills, ponelo antes de IsExecutingSkill).
        //if (boss.IsDamaged)          return Intent.Damaged;

        // Cacheamos Time.time para evitar leerlo dos veces.
        float now = Time.time;

        bool aValid = runtimePrimary   != null && runtimePrimary.CanExecute(wm, ctx, now);
        bool bValid = runtimeSecondary != null && runtimeSecondary.CanExecute(wm, ctx, now);

        // Lógica de selección mínima: primaria sobre secundaria; si ninguna, Idle.
        if (aValid) return Primary;
        if (bValid) return Secondary;

        // Solo vuelve a Idle si no hay nada pendiente
        return Idle;
    }
}
