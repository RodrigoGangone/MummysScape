using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Rendering.Universal;

/// <summary> 
/// Estado de Apuntado: Gestiona la visualización de la trayectoria parabólica, el indicador de rango,
/// y la interacción con objetos mediante SphereCast en el punto de impacto.
/// </summary>
public class AimState : State, IBandageRestrictor
{
    private readonly PlayerContext _ctx;
    private readonly GameObject _decal;
    private readonly DecalProjector _rangeIndicator;
    private readonly LineRenderer _arcRenderer;

    private Coroutine _scaleCoroutine;
    private Vector3 _targetScale;

    private Material _lineMaterialInstance;
    private int _colorPropertyID;

    private const float ANIM_DURATION = 0.2f;
    private const float DECAL_BOX_DEPTH = 50f;

    private Vector2 _aimScreenPos;
    private Vector3 _lastMousePos;
    private const float AIM_SENSITIVITY = 500;

    private bool _canRotateByAim;
    private const float AIM_ROTATE_DEADZONE_SQR = 0.25f;

    // --- VARIABLES DE INTERACCIÓN ---
    private Interactable _currentInteractable; 
    private const float DETECTION_RADIUS = 0.6f; // Radio del SphereCast para detectar el objeto

    public AimState(PlayerContext ctx)
    {
        _ctx = ctx;
        _decal = _ctx.View.Decal;
        _rangeIndicator = _ctx.View.RangeIndicator;
        _arcRenderer = _ctx.View.ArcRenderer;

        _colorPropertyID = Shader.PropertyToID("_Color");
    }

    public override void OnEnter()
    {
        _ctx.View.Animator.SetBool("Aim", true);

        SimpleShootData.Path = null;
        _aimScreenPos = new Vector2(Screen.width / 2, Screen.height / 2);
        _lastMousePos = Input.mousePosition;
        _canRotateByAim = false;

        if (_arcRenderer != null)
        {
            _arcRenderer.enabled = false;
            if (_lineMaterialInstance == null)
                _lineMaterialInstance = _arcRenderer.material;
        }

        if (_rangeIndicator == null) return;

        if (_scaleCoroutine != null)
            _ctx.View.StopCoroutine(_scaleCoroutine);

        float diameter = _ctx.AimMaxDistance * 2f;
        _targetScale = new Vector3(diameter, diameter, DECAL_BOX_DEPTH);
        _rangeIndicator.gameObject.SetActive(true);

        _scaleCoroutine = _ctx.View.StartCoroutine(
            AnimateScale(_rangeIndicator.transform, _targetScale, ANIM_DURATION)
        );
    }

    public override void OnUpdate() { }

    public override void OnFixedUpdate()
    {
        // Posicionar el proyector de rango (el círculo en el suelo)
        if (_rangeIndicator != null && _rangeIndicator.gameObject.activeSelf)
        {
            Vector3 projectorPos = _ctx.Tf.position + Vector3.up * _ctx.AimMaxHeight;
            _rangeIndicator.transform.position = projectorPos;
        }

        // Gestión de Input (Mouse vs Stick)
        Vector2 aimDelta = _ctx.Input.AimMove;
        Vector3 mousePos = Input.mousePosition;

        bool mouseMoved = (mousePos - _lastMousePos).sqrMagnitude > AIM_ROTATE_DEADZONE_SQR;
        bool stickMoved = aimDelta.sqrMagnitude > 0.01f;

        if (mouseMoved)
        {
            _aimScreenPos = mousePos;
            _canRotateByAim = true;
        }
        else if (stickMoved)
        {
            _aimScreenPos.x += aimDelta.x * AIM_SENSITIVITY * Time.deltaTime;
            _aimScreenPos.y += aimDelta.y * AIM_SENSITIVITY * Time.deltaTime;
            _aimScreenPos.x = Mathf.Clamp(_aimScreenPos.x, 0f, Screen.width);
            _aimScreenPos.y = Mathf.Clamp(_aimScreenPos.y, 0f, Screen.height);
            _canRotateByAim = true;
        }

        _lastMousePos = mousePos;

        // Cálculo del punto de impacto de la trayectoria
        bool hasValidTarget = _ctx.TryGetAim(_aimScreenPos, out var pos, out var normal);

        if (hasValidTarget)
        {
            SetDecal(pos, normal);
            
            // Lógica de detección de objetos interactuables
            CheckInteractableAtTarget(pos);

            if (_canRotateByAim)
            {
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
        }
        else
        {
            // Si el aim apunta al vacío, limpiamos el interactuable actual
            ClearCurrentInteractable();
        }

        SetArc(hasValidTarget);
        SetDecalVisible(hasValidTarget);
    }

    public override void OnExit()
    {
        // Limpiar el estado visual del objeto interactuable al salir
        ClearCurrentInteractable();

        SetDecalVisible(false);

        if (_rangeIndicator == null) return;

        if (_arcRenderer != null)
            _arcRenderer.enabled = false;

        if (_scaleCoroutine != null)
            _ctx.View.StopCoroutine(_scaleCoroutine);

        Vector3 exitScale = new Vector3(0, 0, DECAL_BOX_DEPTH);

        _scaleCoroutine = _ctx.View.StartCoroutine(
            AnimateScale(_rangeIndicator.transform, exitScale, ANIM_DURATION, true)
        );

        _ctx.View.Animator.SetBool("Aim", false);
    }

    // --- DETECCIÓN DE INTERACTUABLES ---

    private void CheckInteractableAtTarget(Vector3 impactPoint)
    {
        // Usamos un pequeño SphereCast o OverlapSphere en el punto de impacto
        Collider[] hitColliders = Physics.OverlapSphere(impactPoint, DETECTION_RADIUS);
        Interactable foundInteractable = null;

        foreach (var col in hitColliders)
        {
            // Comprobamos si tiene el componente de lógica de bala
            if (col.TryGetComponent<ActivateObjectsBullet>(out var bulletLogic))
            {
                // Intentamos obtener el componente que maneja el material
                if (col.TryGetComponent<Interactable>(out var interactableMat))
                {
                    foundInteractable = interactableMat;
                    break;
                }
            }
        }

        // Si el objeto bajo la mira ha cambiado
        if (foundInteractable != _currentInteractable)
        {
            // Apagar el anterior
            if (_currentInteractable != null)
                _currentInteractable.OffMaterial();

            // Encender el nuevo
            if (foundInteractable != null)
                foundInteractable.OnMaterial();

            _currentInteractable = foundInteractable;
        }
    }

    private void ClearCurrentInteractable()
    {
        if (_currentInteractable != null)
        {
            _currentInteractable.OffMaterial();
            _currentInteractable = null;
        }
    }

    // --- MÉTODOS VISUALES ---

    private void SetArc(bool hasValidTarget)
    {
        if (_arcRenderer == null) return;

        var path = SimpleShootData.Path;

        if (path == null || path.Count < 2)
        {
            _arcRenderer.enabled = true;
            _arcRenderer.positionCount = 2;

            Vector3 start = _ctx.View.handAnchor != null
                ? _ctx.View.handAnchor.position
                : _ctx.Tf.position + Vector3.up;

            _arcRenderer.SetPosition(0, start);
            _arcRenderer.SetPosition(1, start + _ctx.Tf.forward * 0.1f);
        }
        else
        {
            _arcRenderer.enabled = true;
            _arcRenderer.positionCount = path.Count;
            _arcRenderer.SetPositions(path.ToArray());
        }

        if (_lineMaterialInstance != null)
        {
            Color targetColor = hasValidTarget ? _ctx.View.AimAllowed : _ctx.View.AimNotAllowed;
            _lineMaterialInstance.SetColor(_colorPropertyID, targetColor);
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

    private IEnumerator AnimateScale(Transform targetTransform, Vector3 targetScale, float duration, bool disableOnComplete = false)
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