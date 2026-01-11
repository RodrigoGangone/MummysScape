using UnityEngine;
using System.Collections;
using UnityEngine.Rendering.Universal;

public class AimState : State, IBandageRestrictor
{
    private readonly PlayerContext _ctx;
    private readonly GameObject _decal;
    private readonly DecalProjector _rangeIndicator;
    private readonly LineRenderer _arcRenderer;

    private Coroutine _scaleCoroutine;
    private Vector3 _targetScale;

    // --- VARIABLES PARA EL MATERIAL ---
    private Material _lineMaterialInstance; // Aquí guardamos la instancia
    private int _colorPropertyID;           // ID numérico de la propiedad "_Color"

    private const float ANIM_DURATION = 0.2f;
    private const float DECAL_BOX_DEPTH = 50f;
    
    private Vector2 _aimScreenPos; 
    private Vector3 _lastMousePos; 
    private const float AIM_SENSITIVITY = 500;

    public AimState(PlayerContext ctx)
    {
        _ctx = ctx;
        _decal = _ctx.View.Decal;
        _rangeIndicator = _ctx.View.RangeIndicator;
        _arcRenderer = _ctx.View.ArcRenderer;
        
        // Cacheamos el ID del shader una sola vez para optimizar
        _colorPropertyID = Shader.PropertyToID("_Color");
    }

    public override void OnEnter()
    {
        _ctx.View.Animator.SetBool("Aim", true);

        SimpleShootData.Path = null;

        _aimScreenPos = new Vector2(Screen.width / 2, Screen.height / 2);
        _lastMousePos = Input.mousePosition;

        // --- MANEJO DE MATERIAL ---
        if (_arcRenderer != null)
        {
            _arcRenderer.enabled = false;
            
            // ALERTA: Al acceder a .material, Unity crea la instancia automáticamente.
            // Guardamos esta referencia para reusarla y no crear copias nuevas en Update.
            if (_lineMaterialInstance == null)
            {
                _lineMaterialInstance = _arcRenderer.material;
            }
        }

        if (_rangeIndicator == null) return;

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
    }

    public override void OnFixedUpdate()
    {
        if (_rangeIndicator != null && _rangeIndicator.gameObject.activeSelf)
        {
            Vector3 projectorPos = _ctx.Tf.position + Vector3.up * _ctx.AimMaxHeight;
            _rangeIndicator.transform.position = projectorPos;
        }

        Vector2 aimDelta = _ctx.Input.AimMove;
        Vector3 mousePos = Input.mousePosition;

        if ((mousePos - _lastMousePos).sqrMagnitude > 0.1f)
        {
            _aimScreenPos = mousePos;
        }
        else if (aimDelta.sqrMagnitude > 0.1f)
        {
            _aimScreenPos.x += aimDelta.x * AIM_SENSITIVITY * Time.deltaTime;
            _aimScreenPos.y += aimDelta.y * AIM_SENSITIVITY * Time.deltaTime;
            _aimScreenPos.x = Mathf.Clamp(_aimScreenPos.x, 0f, Screen.width);
            _aimScreenPos.y = Mathf.Clamp(_aimScreenPos.y, 0f, Screen.height);
        }

        _lastMousePos = mousePos;

        // Llamamos al método modificado que devuelve true/false pero siempre genera path
        bool hasValidTarget = _ctx.TryGetAim(_aimScreenPos, out var pos, out var normal);

        if (hasValidTarget)
        {
            SetDecal(pos, normal);

            Vector3 direction = (pos - _ctx.Tf.position).normalized;
            direction.y = 0;

            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                _ctx.Tf.rotation = Quaternion.RotateTowards(
                    _ctx.Tf.rotation,
                    targetRotation,
                    1000 * Time.deltaTime
                );
            }
        }

        // Pasamos el estado de validez
        SetArc(hasValidTarget);
        SetDecalVisible(hasValidTarget);
    }

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
        _ctx.View.Animator.SetBool("Aim", false);
    }
    
    private void SetArc(bool hasValidTarget)
    {
        if (_arcRenderer != null)
        {
            if (SimpleShootData.Path != null && SimpleShootData.Path.Count > 0)
            {
                _arcRenderer.enabled = true;
                _arcRenderer.positionCount = SimpleShootData.Path.Count;
                _arcRenderer.SetPositions(SimpleShootData.Path.ToArray());

                if (_lineMaterialInstance != null)
                {
                    // AQUI EL CAMBIO: Leemos los colores desde tu PlayerView (donde pusiste el HDR)
                    Color targetColor = hasValidTarget ? _ctx.View.AimAllowed : _ctx.View.AimNotAllowed;
                    
                    _lineMaterialInstance.SetColor(_colorPropertyID, targetColor);
                }
            }
            else
            {
                _arcRenderer.enabled = false;
            }
        }
    }
    // ... (El resto de métodos SetDecalVisible, SetDecal y AnimateScale siguen igual) ...
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