using UnityEngine;

public class AimState : State
{
    private readonly PlayerContext _ctx;
    private readonly GameObject _decal;
    public AimState(PlayerContext ctx)
    {
        _ctx = ctx;
        _decal = _ctx.View.Decal;
    }
    
    public override void OnEnter() => SimpleShootData.Path = null;

    public override void OnUpdate()
    {
        if (_ctx.TryGetAim(out var pos))
        {
            SetDecalVisible(true);
            SetDecal(pos); 
        }
        else
            SetDecalVisible(false);
    }

    
    public override void OnFixedUpdate()
    {
    }
    public override void OnExit() => SetDecalVisible(false);
    
    private void SetDecalVisible(bool visible)
    {
        if (_decal && _decal.activeSelf != visible) _decal.SetActive(visible);
    }

    private void SetDecal(Vector3 pos) => _decal.transform.position = pos;
}