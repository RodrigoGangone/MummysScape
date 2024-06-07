using UnityEditor.Animations;
using UnityEngine;

public class ViewPlayer
{
    Player _player;

    private CapsuleCollider _capsuleCollider;

    private Animator[] _animatorController;
    private SkinnedMeshRenderer skinnedMesh;

    public Material hookMaterial;
    public LineRenderer bandageHook;

    public ViewPlayer(Player p)
    {
        _player = p;

        _capsuleCollider = _player.GetComponent<CapsuleCollider>();

        hookMaterial = _player._bandage.material;

        bandageHook = _player._bandage;

        skinnedMesh = _player.GetComponentInChildren<SkinnedMeshRenderer>();
    }

    public void ChangeMesh(Mesh mesh)
    {
        skinnedMesh.sharedMesh = mesh;
    }
    
    public void AdjustColliderSize()
    {
        switch (_player.CurrentPlayerSize)
        {
            case PlayerSize.Normal:
                _capsuleCollider.height = 1.8f;
                _capsuleCollider.center = new Vector3(0, 0.9f, 0);
                break;
            case PlayerSize.Small:
                _capsuleCollider.height = 1.2f;
                _capsuleCollider.center = new Vector3(0, 0.6f, 0);
                break;
            case PlayerSize.Head:
                _capsuleCollider.height = 0.5f;
                _capsuleCollider.center = new Vector3(0, 0.5f, 0);
                break;
        }
    }

    public void PLAY_PUFF()
    {
        _player._puffFX.Play();
    }

    public void PLAY_ANIM(string anim, bool value)
    {
        _player._anim.SetBool(anim, value);
    }

    public void PLAY_ANIM_TRIGGER(string anim)
    {
        _player._anim.SetTrigger(anim);
    }

    public void DrawBandageHOOK()
    {
        bandageHook.SetPosition(0, _player.target.transform.position);
        bandageHook.SetPosition(1, _player._modelPlayer.hookBeetle.transform.position);
    }
}