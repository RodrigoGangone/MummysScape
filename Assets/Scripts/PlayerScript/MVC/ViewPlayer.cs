using UnityEngine;
using UnityEngine.Animations.Rigging;

public class ViewPlayer
{
    Player _player;

    private CapsuleCollider _capsuleCollider;
    private Animator[] _animatorController;
    private SkinnedMeshRenderer bodySkinnedMesh;
    private SkinnedMeshRenderer headSkinnedMesh;

    private Material _bodyMat;
    private Material _headMat;
    private Material _fireMat;
    public Material hookMaterial;
    public LineRenderer bandageHook;

    public RigBuilder rigBuilder;
    public TwoBoneIKConstraint rightHand;

    public bool drawPull;

    public ViewPlayer(Player p, SkinnedMeshRenderer body, SkinnedMeshRenderer head, MeshRenderer fire)
    {
        _player = p;

        rigBuilder = _player.rigBuilder;

        rightHand = _player.rightHand;

        _capsuleCollider = _player.GetComponent<CapsuleCollider>();

        hookMaterial = _player._bandage.material;

        bandageHook = _player._bandage;

        bodySkinnedMesh = body;
        headSkinnedMesh = head;
        _fireMat = fire.material;

        _bodyMat = bodySkinnedMesh.material;
        _headMat = headSkinnedMesh.material;
    }

    public void SetValueMaterial(float value)
    {
        _bodyMat.SetFloat("_Value", value);
        _headMat.SetFloat("_Value", value);
        _fireMat.SetFloat("_Dissolve_Distortion", value);
    }

    public void ChangeMesh(Mesh mesh)
    {
        bodySkinnedMesh.sharedMesh = mesh;
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

    public void DrawBandage(Vector3 finishPos)
    {
        bandageHook.SetPosition(0, _player.handTarget.transform.position);
        bandageHook.SetPosition(1, finishPos);
    }

    public void StateRigBuilder(bool act)
    {
        rigBuilder.enabled = act;
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
}