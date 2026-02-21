using UnityEngine;

/// <summary>
/// Habilita la skill dependiendo si el player esta en su rango de vision.
/// </summary>

[CreateAssetMenu(menuName = "Boss/Conditions/Line Of Sight")]
public sealed class BC_PlayerLoS : SkillConditionSO
{
    public override bool Evaluate(in WorldModel wm, IBossContext ctx) => wm.HasLineOfSight;
}