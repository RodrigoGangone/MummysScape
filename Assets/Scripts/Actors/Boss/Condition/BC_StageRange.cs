using UnityEngine;

/// <summary>
/// Habilita la skill solo dentro de un rango de stages.
/// </summary>

[CreateAssetMenu(menuName = "Boss/Conditions/Stage Range")]
public sealed class BC_StageRange : SkillConditionSO
{
    [SerializeField, Min(0)] private int minStage = 0;
    [SerializeField, Min(0)] private int maxStageInclusive = 99;

    public override bool Evaluate(in WorldModel wm, IBossContext ctx) => wm.StageIndex >= minStage && wm.StageIndex <= maxStageInclusive;
}