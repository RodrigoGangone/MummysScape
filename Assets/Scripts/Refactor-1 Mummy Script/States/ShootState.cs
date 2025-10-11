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
        if (path == null || path.Count == 0) return;

        var start = (_ctx.Tf) ? _ctx.Tf.position : path[0];
        var rot = (path.Count > 1) ? Quaternion.LookRotation((path[1] - start).normalized) : Quaternion.identity;

        var go = Object.Instantiate(_projectile, start, rot);

        go.GetComponent<BandageProjectile>().Play(path, 10);
    }


    public override void OnUpdate()
    {
    }

    public override void OnFixedUpdate()
    {
    }

    public override void OnExit()
    {
    }
}