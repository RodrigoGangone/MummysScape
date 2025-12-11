using UnityEngine;
using static PlayerEnum;

public sealed class ShootState : State
{
    private readonly PlayerContext _ctx;
    private readonly GameObject _projectile;

    public ShootState(PlayerContext ctx)
    {
        _ctx = ctx;
        _projectile = _ctx.ProjectilePrefab;
    }

    public override void OnEnter()
    {
        var path = SimpleShootData.Path;
        
        // --- CAMBIO: Determinar el punto de inicio ---
        Vector3 startPos;

        if (_ctx.ShootOrigin != null)
        {
            // Prioridad 1: El transform asignado (Mano/Arma)
            startPos = _ctx.ShootOrigin.position;
        }
        else if (path != null && path.Count > 0)
        {
            // Prioridad 2: El inicio del path calculado
            startPos = path[0];
        }
        else
        {
            // Fallback
            startPos = _ctx.Tf.position;
        }

        // Calculamos rotación (mirando al siguiente punto del path si existe)
        var rot = (path != null && path.Count > 1) 
            ? Quaternion.LookRotation((path[1] - startPos).normalized) 
            : Quaternion.identity;

        // Instanciamos
        var go = Object.Instantiate(_projectile, startPos, rot);
        // ---------------------------------------------
        
        _ctx.View.Animator.SetBool("Shoot", true);
        
        if (path != null) // Pequeña seguridad extra por si path es null
            go.GetComponent<BandageProjectile>().Play(path, 30);
    }

    public override void OnUpdate()
    {
    }

    public override void OnFixedUpdate()
    {
    }

    public override void OnExit()
    {
        _ctx.View.Animator.SetBool("Shoot", false);
    }
}
