using UnityEngine;

[CreateAssetMenu(menuName = "Boss/Skills/Scorpion/Charge Projectile Skill")]
public class BS_ChargeProjectile : BossSkillSO
{
    protected override void Execute(in WorldModel wm, IBossContext ctx)
    {
        var projectile = ctx.Transform.GetComponentInChildren<ChargeableProjectile>();
        
        if (projectile != null)
        {
            projectile.Launch();
            
            projectile.transform.SetParent(null, true);
        }
        else
            Debug.LogWarning("[BS_ChargeProjectile] No se encontró un ChargeableProjectile para lanzar.");
    }
}