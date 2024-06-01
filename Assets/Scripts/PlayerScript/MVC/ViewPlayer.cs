using UnityEditor.Animations;
using UnityEngine;

public class ViewPlayer
{
    Player _player;
    
    private Animator[] _animatorController;
    private SkinnedMeshRenderer skinnedMesh;

    public Material hookMaterial;
    public LineRenderer bandageHook;

    public ViewPlayer(Player p)
    {
        _player = p;

        hookMaterial = _player._bandage.material;

        bandageHook = _player._bandage;

        skinnedMesh = _player.GetComponentInChildren<SkinnedMeshRenderer>();
    }

    public void ChangeMesh(Mesh mesh)
    {
        skinnedMesh.sharedMesh = mesh;
    }

    public void PLAY_PUFF()
    {
        _player._puffFX.Play();
    }

    public void PLAY_ANIM(string anim, bool value)
    {
        _player._anim.SetBool(anim, value);
    }

    public void DrawBandageHOOK()
    {
        bandageHook.SetPosition(0,_player.target.transform.position);
        bandageHook.SetPosition(1,_player._modelPlayer.hookBeetle.transform.position);
    }
}