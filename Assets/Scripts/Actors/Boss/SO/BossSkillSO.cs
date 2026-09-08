using UnityEngine;

/// <summary>
/// Base de Habilidades: Define la lógica estructural para ejecutar ataques, gestionando 
/// automáticamente los tiempos de recarga (cooldown) y la validación de condiciones de uso.
/// </summary>

#region Interfaces de integración

/// <summary>
/// Interfaz mínima que debe exponer tu Boss para que las skills puedan operar sin acoplarse a una clase concreta.
/// </summary>

public interface IBossContext
{
    Transform Transform { get; }
    PlayerContext Player { get; }
    int CurrentStageIndex { get; }
    BossConfigSO Config { get; }
}

/// <summary>
/// Snapshot liviano del mundo para que condiciones/skills no raycastéen de más.
/// </summary>

public readonly struct WorldModel
{
    private readonly Vector3 BossPos;
    private readonly Vector3 PlayerPos;
    public readonly float DistanceBP;
    public readonly bool HasLineOfSight;
    public readonly int StageIndex;
    public readonly BossConfigSO Config;

    public WorldModel(IBossContext ctx, bool hasLOS)
    {
        BossPos = ctx.Transform.position;
        PlayerPos = ctx.Player.Tf.position;
        DistanceBP = Vector3.Distance(BossPos, PlayerPos);
        HasLineOfSight = hasLOS;
        StageIndex = ctx.CurrentStageIndex;
        Config = ctx.Config;
    }
}

#endregion

/// <summary>
/// Base abstracta para cualquier habilidad de jefe. Maneja cooldown y ejecución segura.
/// Cada skill concreta sobreescribe Execute() y, si lo necesita, CanExecuteExtra().
/// </summary>

public abstract class BossSkillSO : ScriptableObject
{
    [Header("Datos")]
    [SerializeField, Min(0f)] private float baseCooldown = 3f;

    [Header("Condiciones (todas deben cumplirse)")]
    [SerializeField] private SkillConditionSO[] conditions;

    private float _lastUseTime = -999;

    /// <summary> Devuelve true si el cooldown (ajustado por Stage) ya se cumplió. </summary>
    private bool IsReady(float now, BossConfigSO config, int stageIndex)
    {
        float cd = GetCooldownForStage(config, stageIndex);
        return now >= _lastUseTime + cd;
    }

    /// <summary>
    /// Evalúa si esta skill puede ejecutarse (cooldown + condiciones + lógica adicional).
    /// NO ejecuta, NO consume cooldown.
    /// </summary>
    public bool CanExecute(in WorldModel wm, IBossContext ctx, float now)
    {
        if (!IsReady(now, wm.Config, wm.StageIndex))
            return false;

        if (conditions != null)
        {
            foreach (var c in conditions)
                if (c != null && !c.Evaluate(wm, ctx))
                    return false;
        }

        return true;
    }
    
    /// <summary> Consulta el cooldown específico desde StageStats (si hay mapping), o usa baseCooldown. </summary>
    private float GetCooldownForStage(BossConfigSO config, int stageIndex)
    {
        var stats = config?.GetStage(stageIndex);
        if (stats == null) return baseCooldown;

        return Mathf.Max(0f, baseCooldown * stats.cooldownMultiplier);
    }

    /// <summary> Intenta ejecutar la skill (verifica cooldown + condiciones). </summary>
    /// <summary>
    /// Intenta ejecutar la skill. Si pasa todos los chequeos, la ejecuta y entra en cooldown.
    /// </summary>
    public bool TryExecute(in WorldModel wm, IBossContext ctx, float now)
    {
        if (!CanExecute(wm, ctx, now))
            return false;

        Execute(wm, ctx);
        _lastUseTime = now;
        return true;
    }
    
    /// <summary> Lógica específica de la habilidad. Evitar dependencias duras: usar IBossContext. </summary>
    protected abstract void Execute(in WorldModel wm, IBossContext ctx);
    
    /// <summary>
    /// Limpia el estado interno de la habilidad si es interrumpida forzosamente (ej: cinemáticas).
    /// Al ser virtual, las habilidades hijas pueden sobrescribirlo para limpiar sus propias variables.
    /// </summary>
    public virtual void ResetSkill()
    {
        // Forzamos el tiempo de último uso a un valor muy antiguo para saltarnos el cooldown
        _lastUseTime = -999f; 
    }
}