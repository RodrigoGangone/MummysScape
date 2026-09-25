using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static SfxIDs;
using static Tags;

/// <summary> 
/// Interruptor por Impacto: Detecta colisiones de proyectiles ("Bullet") para activar secuencias 
/// de movimiento en plataformas y ejecutar efectos visuales de intensidad en materiales. 
/// </summary>

public class ActivateObjectsBullet : MonoBehaviour
{
    [SerializeField] private List<GameObject> _platformsAll;
    [SerializeField] private FxBank eagleBank;
    private Animator _animator;
    private Material _material;
    private BoxCollider _boxCollider;
    private bool _activated;
    private readonly List<MonoBehaviour> _activationGroup = new List<MonoBehaviour>();
    private FocusManager.ActivationHandle _activation;

    private void Start()
    {
        _animator = GetComponent<Animator>();
        var targetRenderer = GetComponentInChildren<Renderer>();
        if (targetRenderer != null) _material = targetRenderer.material;
        _boxCollider = GetComponent<BoxCollider>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_activated || !other.gameObject.CompareTag(PROJECTILE_TAG)) return;
        _activated = true;
        
        if (_boxCollider != null) _boxCollider.enabled = false;
        
        if (eagleBank != null) eagleBank.Play3D(Eagle.Active, transform.position);
        
        if (_animator != null) _animator.SetBool("IsActive", true);

        if (_material != null) StartCoroutine(SineIntensity());

        ActivatePlatforms();
    }

    private void ActivatePlatforms()
    {
        _activationGroup.Clear();
        FocusOnActivation firstFocus = null;
        var seen = new HashSet<MonoBehaviour>();
        if (_platformsAll == null) return;

        foreach (GameObject platform in _platformsAll)
        {
            if (platform == null) continue;
            foreach (var component in platform.GetComponents<MonoBehaviour>())
            {
                if (component is not IFocusActivatablePlatform || !component.isActiveAndEnabled ||
                    !seen.Add(component)) continue;
                _activationGroup.Add(component);
                var focus = component.GetComponent<FocusOnActivation>();
                if (firstFocus == null && focus != null && focus.CanFocus && !focus.IsPending)
                    firstFocus = focus;
            }
        }

        if (_activationGroup.Count == 0) return;
        if (firstFocus != null)
            _activation = firstFocus.ActivateWhenFocused(this, StartGroup, FinishGroupFocus, IsGroupPreparing);
        else
        {
            Debug.LogWarning("[ActivateObjectsBullet] El grupo no tiene un foco disponible; activando directamente.", this);
            StartGroup(false);
        }
    }

    private void StartGroup(bool hasFocus)
    {
        foreach (var component in _activationGroup)
            if (component != null && component.isActiveAndEnabled)
                ((IFocusActivatablePlatform)component).StartActionWithoutFocus(hasFocus);
    }

    private void FinishGroupFocus()
    {
        foreach (var component in _activationGroup)
            if (component != null) ((IFocusActivatablePlatform)component).EndActivationFocus();
    }

    private bool IsGroupPreparing()
    {
        foreach (var component in _activationGroup)
            if (component != null && component.isActiveAndEnabled &&
                ((IFocusActivatablePlatform)component).IsPreparingActivation) return true;
        return false;
    }

    private void OnDisable()
    {
        _activation?.Dispose();
        _activation = null;
        FinishGroupFocus();
    }

    IEnumerator SineIntensity()
    {
        yield return StartCoroutine(ChangeIntensity(0f, 1f, 0.8f));
        yield return StartCoroutine(ChangeIntensity(1f, 0f, 0.8f));
    }

    IEnumerator ChangeIntensity(float startValue, float endValue, float duration)
    {
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float intensity = Mathf.Lerp(startValue, endValue, elapsedTime / duration);
            _material.SetFloat("_intensity", intensity);
            yield return null;
        }

        _material.SetFloat("_intensity", endValue);
    }
}
