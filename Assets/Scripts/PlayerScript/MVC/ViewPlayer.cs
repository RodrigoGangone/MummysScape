using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class ViewPlayer
{
    Player _player;

    private CapsuleCollider _capsuleCollider;
    private Animator[] _animatorController;

    private SkinnedMeshRenderer _bodySkinnedMesh;
    private SkinnedMeshRenderer _headSkinnedMesh;

    private Material _bodyMat;
    private Material _headMat;
    private Material _fireMat;

    private ParticleSystem _fireParticle;
    private ParticleSystem _smokeParticle;
    
    private const string _ACTIVEFLAME = "_isActive";
    private const string _COLORFLAME = "_Color";

    public Material hookMaterial;
    public LineRenderer bandageLineRenderer;

    private RigBuilder rigBuilder;
    public TwoBoneIKConstraint rightHand;

    //Vars para disappear
    private const float _materialTransitionDuration = 1.5f; // Duracion en segundos para cambiar el material

    private bool _materialChanged;
    private float _elapsedTime;

    public ViewPlayer(Player p, SkinnedMeshRenderer body, SkinnedMeshRenderer head, MeshRenderer fire)
    {
        _player = p;

        rigBuilder = _player.rigBuilder;

        rightHand = _player.rightHand;

        _capsuleCollider = _player.GetComponent<CapsuleCollider>();

        hookMaterial = _player._bandage.material;

        bandageLineRenderer = _player._bandage;

        _bodySkinnedMesh = body;
        _headSkinnedMesh = head;

        _fireMat = fire.material;

        _bodyMat = _bodySkinnedMesh.material;
        _headMat = _headSkinnedMesh.material;

        //_fireParticle = _fireMat.GameObject().GetComponentInChildren<ParticleSystem>();
        //_smokeParticle = _fireMat.GameObject().GetComponentInChildren<ParticleSystem>();
    }

    public void SetValueMaterial(float valueBody, float valueHead)
    {
        _bodyMat.SetFloat("_CutoffHeight", valueBody);
        _headMat.SetFloat("_CutoffHeight", valueHead);
        _fireMat.SetFloat("_Dissolve_Distortion", valueHead);
    }

    public void ChangeMesh(Mesh mesh)
    {
        _bodySkinnedMesh.sharedMesh = mesh;
    }

    public void AdjustViewProperty()
    {
        if (_capsuleCollider == null) return;

        float height, radius, centerY;

        _fireMat.SetFloat(_ACTIVEFLAME, _player.CurrentPlayerSize == PlayerSize.Head ? 0 : 1);

        _player._fire.SetActive(_player.CurrentPlayerSize != PlayerSize.Head);

        switch (_player.CurrentPlayerSize)
        {
            case PlayerSize.Normal:
                height = 1.8f;
                radius = 0.5f;
                centerY = 0.9f;

                _fireMat.SetColor(_COLORFLAME, _player._fireColorNormal);
                break;
            case PlayerSize.Small:
                height = 1.2f;
                radius = 0.5f;
                centerY = 0.6f;

                _fireMat.SetColor(_COLORFLAME, _player._fireColorSmall);
                break;
            case PlayerSize.Head:
                height = 0.71f;
                radius = 0.35f;
                centerY = 0.33f;
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
        bandageLineRenderer.SetPosition(0, _player.handTarget.transform.position);
        bandageLineRenderer.SetPosition(1, finishPos);
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
        if (act)
            _player._walkFX.Play();
        else
            _player._walkFX.Stop();
    }

    public void PLAY_WALK_SAND(bool act)
    {
        if (act)
            _player._walkSandFX.Play();
        else
        {
            _player._walkSandFX.Stop();
            //_player._walkSandFX.Clear();
        }
    }

    public void PLAY_ANIM(string anim, bool value)
    {
        _player._anim.SetBool(anim, value);
    }

    public void PLAY_ANIM_TRIGGER(string anim)
    {
        _player._anim.SetTrigger(anim);
    }

    public RaycastHit? GetHitFromPull()
    {
        var rayOrigin = new Vector3(
            _player.transform.position.x,
            _player.ShootTargetTransform.position.y,
            _player.transform.position.z
        );

        var movableBoxLayer = LayerMask.NameToLayer("MovableBox");
        var layerMaskBox = 1 << movableBoxLayer;

        Vector3 forwardDirection = _player.transform.forward;
        Vector3 rightDirection = Quaternion.Euler(0, 5, 0) * _player.transform.forward;
        Vector3 leftDirection = Quaternion.Euler(0, -5, 0) * _player.transform.forward;

        Vector3[] directions = { forwardDirection, rightDirection, leftDirection };
        foreach (var direction in directions)
        {
            if (Physics.Raycast(rayOrigin, direction, out var hit, _player.RayCheckPullDistance, layerMaskBox))
            {
                return hit;
            }
        }

        return null;
    }

    public RaycastHit? GetHitFromPush()
    {
        var rayOrigin = new Vector3(
            _player.transform.position.x,
            _player.ShootTargetTransform.position.y,
            _player.transform.position.z
        );

        var movableBoxLayer = LayerMask.NameToLayer("MovableBox");
        var layerMaskBox = 1 << movableBoxLayer;

        if (Physics.Raycast(rayOrigin, _player.transform.forward, out var hit, _player.RayCheckPushDistance,
                layerMaskBox))
        {
            return hit;
        }

        return null;
    }

    internal IEnumerator MaterialTransitionCoroutine(bool fadeOut)
    {
        _elapsedTime = 0f;
        _materialChanged = false;

        // Fase 1: Desvanecer el cuerpo

        var bodyStartValue = fadeOut ? 1f : -0.5f;
        var bodyEndValue = fadeOut ? -0.5f : 1f;

        var headStartValue = fadeOut ? 1f : -0.5f;
        var headEndValue = fadeOut ? -0.5f : 1f;

        _player._viewPlayer.SetValueMaterial(bodyStartValue, headStartValue);

        while (_elapsedTime < _materialTransitionDuration)
        {
            _elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(_elapsedTime / _materialTransitionDuration);

            // Interpolar sólo el cuerpo
            float bodyValue = Mathf.Lerp(bodyStartValue, bodyEndValue, t);
            _player._viewPlayer.SetValueMaterial(bodyValue,
                headStartValue); // Mantener la cabeza visible (1f) durante esta fase

            yield return null;
        }

        _player._viewPlayer.SetValueMaterial(bodyEndValue, headStartValue);

        _elapsedTime = 0f;

        // Fase 2: Desvanecer la cabeza

        while (_elapsedTime < _materialTransitionDuration)
        {
            _elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(_elapsedTime / _materialTransitionDuration);

            // Interpolar sólo la cabeza
            float headValue = Mathf.Lerp(headStartValue, headEndValue, t);
            _player._viewPlayer.SetValueMaterial(bodyEndValue, headValue); // Mantener el cuerpo en su valor final

            yield return null;
        }

        _player._viewPlayer.SetValueMaterial(bodyEndValue, headEndValue);
        _materialChanged = true;
    }
}