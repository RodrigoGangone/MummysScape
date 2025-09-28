using UnityEngine;

/// <summary> Requiere que el jugador esté a <= maxDistance del boss. </summary>
[CreateAssetMenu(menuName = "Boss/Conditions/Distance Max")]
public sealed class BC_PlayerMaxDistance : SkillConditionSO
{
    [SerializeField, Min(0f)] private float maxDistance = 5f;
    public override bool Evaluate(in WorldModel wm, IBossContext ctx) => wm.DistanceBP <= maxDistance;
}