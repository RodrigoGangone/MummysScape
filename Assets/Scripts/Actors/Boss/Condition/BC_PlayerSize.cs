using System;
using UnityEngine;
using static PlayerEnum;

/// <summary>
/// Habilita la skill solo con un Size en especifico.
/// </summary>

    [CreateAssetMenu(menuName = "Boss/Conditions/Player Size")]
    public sealed class BC_PlayerSize : SkillConditionSO
    {
        [SerializeField] private PlayerSize[] allowedSizes;
        
        public override bool Evaluate(in WorldModel wm, IBossContext ctx) => Array.IndexOf(allowedSizes, ctx.Player.Model.Size) >= 0;
    }
