using UnityEngine;

/// <summary>
/// Cerebro Lógico: Evalúa el "WorldModel" y la disponibilidad de habilidades para devolver la intención 
/// simbólica que el BossActor utilizará para cambiar sus estados en la FSM.
/// </summary>

public sealed class GoapBrain
{
    private static class Intent
    {
        public const string None      = "None";
        public const string Die   = "Die";
        public const string PreDie   = "PreDie";
        public const string Angry   = "Angry";
        public const string Primary   = "Primary";
        public const string Secondary = "Secondary";
        public const string Idle      = "Idle";
    }
    
    internal bool Paused;
    internal bool Locked;

    public string DecideNextIntent(in WorldModel wm, IBossContext ctx, BossSkillSO runtimePrimary, BossSkillSO runtimeSecondary)
    {
        // Si el contexto no es un BossActor, no hay decisión posible.
        if (ctx is not BossActor boss || Paused || Locked) return Intent.None;

        //Priorizamos la muerte del Boss por sobre todas las cosas
        if (boss.IsDie)              return Intent.Die;
        if (boss.IsPreDie)           return Intent.PreDie;
        // Estados que no deben cambiar intención (early-outs).
        if (boss.IsEntry)            return Intent.None;
        if (boss.IsExecutingSkill)   return Intent.None;

        // Priorizamos daño si aplica.
        //if (boss.IsAngry)            return Intent.Angry;

        // Cacheamos Time.time para evitar leerlo dos veces.
        float now = Time.time;

        bool aValid = runtimePrimary   != null && runtimePrimary.CanExecute(wm, ctx, now);
        bool bValid = runtimeSecondary != null && runtimeSecondary.CanExecute(wm, ctx, now);

        // Lógica de selección mínima: primaria sobre secundaria; si ninguna, Idle.
        if (aValid) return Intent.Primary;
        if (bValid) return Intent.Secondary;

        // Solo vuelve a Idle si no hay nada pendiente
        return Intent.Idle;
    }
}
