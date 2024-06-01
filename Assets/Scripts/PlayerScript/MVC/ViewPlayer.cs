using UnityEditor.Animations;
using UnityEngine;

public class ViewPlayer
{
    Player _player;
    private Animator[] _animatorController;
    private SkinnedMeshRenderer skinnedMesh;
    public Material hookMaterial;

    public ViewPlayer(Player p)
    {
        _player = p;
        hookMaterial = _player._bandage.GetComponent<LineRenderer>().material;
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
}