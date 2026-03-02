using UnityEngine;

/// <summary> 
/// Skill de Boss: Localiza un proyectil "ChargeableProjectile" en la jerarquía del Boss, 
/// activa su lanzamiento y lo libera de su contenedor (parent) para permitir su vuelo independiente.
/// </summary>

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