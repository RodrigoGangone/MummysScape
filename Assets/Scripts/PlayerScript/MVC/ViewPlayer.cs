using System;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class ViewPlayer
{
    Player _player;

    private CapsuleCollider _capsuleCollider;
    private Animator[] _animatorController;
    private SkinnedMeshRenderer skinnedMesh;

    public Material playerMat;
    public Material hookMaterial;
    public LineRenderer bandageHook;

    public Rig rigBuilder;

    public ViewPlayer(Player p)
    {
        _player = p;

        rigBuilder = _player.GetComponentInChildren<Rig>();

        _capsuleCollider = _player.GetComponent<CapsuleCollider>();

        hookMaterial = _player._bandage.material;

        bandageHook = _player._bandage;

        skinnedMesh = _player.GetComponentInChildren<SkinnedMeshRenderer>();

        playerMat = skinnedMesh.material;
    }

    public void ChangeMesh(Mesh mesh)
    {
        skinnedMesh.sharedMesh = mesh;
    }

    public void AdjustColliderSize()
    {
        if (_capsuleCollider == null) return;

        float height, radius, centerY;

        switch (_player.CurrentPlayerSize)
        {
            case PlayerSize.Normal:
                height = 1.8f;
                radius = 0.5f;
                centerY = 0.9f;
                break;
            case PlayerSize.Small:
                height = 1.2f;
                radius = 0.5f;
                centerY = 0.6f;
                break;
            case PlayerSize.Head:
                height = 0.5f;
                radius = 0.4f;
                centerY = 0.3f;
                break;
            default:
                Debug.LogWarning("Unknown player size.");
                return;
        }

        _capsuleCollider.height = height;
        _capsuleCollider.radius = radius;
        _capsuleCollider.center = new Vector3(0, centerY, 0);
    }

    public void PLAY_PUFF()
    {
        _player._puffFX.Play();
    }

    public void PLAY_WALK(bool act)
    {
        _player._walkFX.Play();
        _player._walkFX.loop = act;
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
        //rigBuilder.weight = 1;

        _player.rightHand.data.target = _player._modelPlayer.hookBeetle.transform;

        bandageHook.SetPosition(0, _player.target.transform.position);
        bandageHook.SetPosition(1, _player._modelPlayer.hookBeetle.transform.position);
    }
}