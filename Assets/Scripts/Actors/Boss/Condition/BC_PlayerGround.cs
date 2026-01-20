using UnityEngine;


/// <summary> Habilita la skill dependiendo donde este posicionado el Player. </summary>
[CreateAssetMenu(menuName = "Boss/Conditions/Player in ground")]
public sealed class BC_PlayerGround : SkillConditionSO
{
    [SerializeField] private bool inGround;

    public override bool Evaluate(in WorldModel wm, IBossContext ctx) =>
        inGround == ctx.Player.IsGrounded();
}