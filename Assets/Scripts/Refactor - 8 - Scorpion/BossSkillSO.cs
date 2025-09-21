using System;
using UnityEngine;

#region Interfaces de integración

/// <summary>
/// Interfaz mínima que debe exponer tu Boss para que las skills puedan operar sin acoplarse a una clase concreta.
/// </summary>
public interface IBossContext
{
    Transform Transform { get; }
    Animator Animator { get; }
    GameObject GameObject { get; }
    Player Player { get; }

    // Señal “simbólica” para tu FSM existente (StateMachine).
    void TriggerFSM(string intentOrEvent);

    // Lectura del stage actual (0..N-1).
    int CurrentStageIndex { get; }

    // Acceso a la configuración viva del Boss.
    BossConfigSO Config { get; }
}

/// <summary>
/// Snapshot liviano del mundo para que condiciones/skills no raycastéen de más.
/// </summary>
public readonly struct WorldModel
{
    public readonly Vector3 BossPos;
    public readonly Vector3 PlayerPos;
    public readonly float DistanceBP;
    public readonly bool HasLineOfSight;
    public readonly int StageIndex;
    public readonly BossConfigSO Config;

    public WorldModel(IBossContext ctx, bool hasLOS)
    {
        BossPos = ctx.Transform.position;
        PlayerPos = ctx.Player.transform.position;
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
    public bool IsReady(float now, BossConfigSO config, int stageIndex)
    {
        
        float cd = GetCooldownForStage(config, stageIndex);
        //Debug.Log(_lastUseTime + "BossSkillSO" + now);
        return now >= _lastUseTime + cd;
    }

    /// <summary> Consulta el cooldown específico desde StageStats (si hay mapping), o usa baseCooldown. </summary>
    protected virtual float GetCooldownForStage(BossConfigSO config, int stageIndex)
    {
        var stats = config?.GetStage(stageIndex);
        if (stats == null) return baseCooldown;

        // Por defecto, usa un multiplicador general. Si luego querés mapear por-skill, lo ampliamos.
        return Mathf.Max(0f, baseCooldown * stats.cooldownMultiplier);
    }

    /// <summary> Hook para condiciones propias de la skill que no viven en SOs reutilizables. </summary>
    protected virtual bool CanExecuteExtra(in WorldModel wm, IBossContext ctx) => true;

    /// <summary> Intenta ejecutar la skill (verifica cooldown + condiciones). </summary>
    public bool TryExecute(in WorldModel wm, IBossContext ctx, float now)
    {
        if (!IsReady(now, wm.Config, wm.StageIndex)) return false;

        // Eval de condiciones componibles
        if (conditions != null)
        {
            for (int i = 0; i < conditions.Length; i++)
                if (conditions[i] != null && !conditions[i].Evaluate(wm, ctx))
                    return false;
        }

        if (!CanExecuteExtra(wm, ctx)) return false;

        Execute(wm, ctx);
        _lastUseTime = now;
        return true;
    }

    /// <summary> Lógica específica de la habilidad. Evitar dependencias duras: usar IBossContext. </summary>
    protected abstract void Execute(in WorldModel wm, IBossContext ctx);
}

/// <summary>
/// Condición abstracta para comporner lógica de disponibilidad de una habilidad (distancia, LOS, stage, etc.)
/// </summary>
public abstract class SkillConditionSO : ScriptableObject
{
    public abstract bool Evaluate(in WorldModel wm, IBossContext ctx);
}

#region Condiciones comunes (listas para usar)

/// <summary> Requiere que el jugador esté a <= maxDistance del boss. </summary>
[CreateAssetMenu(menuName = "Boss/Conditions/Distance Max")]
public sealed class DistanceMaxConditionSO : SkillConditionSO
{
    [SerializeField, Min(0f)] private float maxDistance = 5f;
    public override bool Evaluate(in WorldModel wm, IBossContext ctx) => wm.DistanceBP <= maxDistance;
}

/// <summary> Requiere línea de visión previa (el cálculo de LOS lo setea el Boss antes de armar el WorldModel). </summary>
[CreateAssetMenu(menuName = "Boss/Conditions/Line Of Sight")]
public sealed class LineOfSightConditionSO : SkillConditionSO
{
    public override bool Evaluate(in WorldModel wm, IBossContext ctx) => wm.HasLineOfSight;
}

/// <summary> Habilita la skill solo dentro de un rango de stages. </summary>
[CreateAssetMenu(menuName = "Boss/Conditions/Stage Range")]
public sealed class StageRangeConditionSO : SkillConditionSO
{
    [SerializeField, Min(0)] private int minStage = 0;
    [SerializeField, Min(0)] private int maxStageInclusive = 99;
    public override bool Evaluate(in WorldModel wm, IBossContext ctx)
        => wm.StageIndex >= minStage && wm.StageIndex <= maxStageInclusive;
}

/// <summary> Habilita la skill solo con un Size en especifico. </summary>
[CreateAssetMenu(menuName = "Boss/Conditions/Player Size")]
public sealed class PlayerSizeConditionSO : SkillConditionSO
{
    [SerializeField] private PlayerSize[] allowedSizes;
    public override bool Evaluate(in WorldModel wm, IBossContext ctx) => Array.IndexOf(allowedSizes, ctx.Player.CurrentPlayerSize) >= 0;
}


/// <summary> Habilita la skill dependiendo donde este posicionado el Player. </summary>
[CreateAssetMenu(menuName = "Boss/Conditions/Player in ground")]
public sealed class PlayerGroundConditionSO : SkillConditionSO
{
    [SerializeField] private bool inGround;
    public override bool Evaluate(in WorldModel wm, IBossContext ctx) => inGround == ctx.Player._modelPlayer.CheckGround();
}


#endregion
