using UnityEngine;

/// <summary> Requiere línea de visión previa (el cálculo de LOS lo setea el Boss antes de armar el WorldModelk ). </summary>
[CreateAssetMenu(menuName = "Boss/Conditions/Line Of Sight")]
public sealed class BC_PlayerLoS : SkillConditionSO
{
    public override bool Evaluate(in WorldModel wm, IBossContext ctx) => wm.HasLineOfSight;
}