using UnityEngine;
using System.Collections;
using UnityEngine.Rendering.Universal;

public class AimState : State
{
    private readonly PlayerContext _ctx;
    private readonly GameObject _decal;
    private readonly DecalProjector _rangeIndicator;
    private readonly LineRenderer _arcRenderer;
    
    private Coroutine _scaleCoroutine;
    private Vector3 _targetScale;

    private const float ANIM_DURATION = 0.2f;
    private const float DECAL_BOX_DEPTH = 50f;

    public AimState(PlayerContext ctx)
    {
        _ctx = ctx;
        _decal = _ctx.View.Decal;
        _rangeIndicator = _ctx.View.RangeIndicator;
        _arcRenderer = _ctx.View.ArcRenderer;
    }

    public override void OnEnter()
    {
        SimpleShootData.Path = null;
        if (_rangeIndicator == null) return;

        if (_arcRenderer != null)
            _arcRenderer.enabled = false;
        
        if (_scaleCoroutine != null)
            _ctx.View.StopCoroutine(_scaleCoroutine);
        
        float diameter = _ctx.AimMaxDistance * 2f;

        _targetScale = new Vector3(diameter, diameter, DECAL_BOX_DEPTH);

        _rangeIndicator.gameObject.SetActive(true);

        _scaleCoroutine = _ctx.View.StartCoroutine(AnimateScale(_rangeIndicator.transform,
                                                                      _targetScale,
                                                                      ANIM_DURATION));
    }

    public override void OnUpdate()
    {
        if (_rangeIndicator != null && _rangeIndicator.gameObject.activeSelf)
        {
            Vector3 projectorPos = _ctx.Tf.position + Vector3.up * _ctx.AimMaxHeight;
            _rangeIndicator.transform.position = projectorPos;
        }

        bool hasValidTarget = _ctx.TryGetAim(out var pos, out var normal);
        

        
        if (hasValidTarget)
            SetDecal(pos, normal);
        
        SetArc(hasValidTarget);
        SetDecalVisible(hasValidTarget);
    }

    public override void OnFixedUpdate() { }

    public override void OnExit()
    {
        SetDecalVisible(false);
        
        if (_rangeIndicator == null) return;

        if (_arcRenderer != null)
            _arcRenderer.enabled = false;
        
        if (_scaleCoroutine != null)
            _ctx.View.StopCoroutine(_scaleCoroutine);

        Vector3 exitScale = new Vector3(0, 0, DECAL_BOX_DEPTH);

        _scaleCoroutine = _ctx.View.StartCoroutine(AnimateScale(_rangeIndicator.transform,
                                                                        exitScale,
                                                                        ANIM_DURATION,
                                                                        true));
    }

    private void SetArc(bool hasValidTarget)
    {
        if (_arcRenderer != null)
        {
            if (hasValidTarget && SimpleShootData.Path != null && SimpleShootData.Path.Count > 0)
            {
                // Si es válido: DIBUJA
                _arcRenderer.enabled = true;
                _arcRenderer.positionCount = SimpleShootData.Path.Count;
                _arcRenderer.SetPositions(SimpleShootData.Path.ToArray());
            }
            else
            {
                // ¡AQUÍ ESTÁ LA CLAVE!
                // Si no es válido (hasValidTarget es false): APAGA
                _arcRenderer.enabled = false;
            }
        }
    }
    
    private void SetDecalVisible(bool visible)
    {
        if (_decal && _decal.activeSelf != visible)
            _decal.SetActive(visible);
    }
    
    private void SetDecal(Vector3 pos, Vector3 normal)
    {
        if (!_decal) return;

        _decal.transform.position = pos + normal * 0.05f;

        if (normal != Vector3.zero)
            _decal.transform.rotation = Quaternion.LookRotation(_ctx.Tf.right, normal);
    }

    private IEnumerator AnimateScale(Transform targetTransform, Vector3 targetScale, float duration,
        bool disableOnComplete = false)
    {
        Vector3 startScale = targetTransform.localScale;
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;

            float t = Mathf.Clamp01(timer / duration);
            t = 1 - (1 - t) * (1 - t);

            targetTransform.localScale = Vector3.Lerp(startScale, targetScale, t);

            yield return null;
        }

        targetTransform.localScale = targetScale;

        if (disableOnComplete)
            targetTransform.gameObject.SetActive(false);

        _scaleCoroutine = null;
    }
}