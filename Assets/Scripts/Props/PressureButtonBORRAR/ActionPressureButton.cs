using UnityEngine;
using System.Collections;
using System;
using static PauseUtils;
using UnityEngine.Rendering.Universal;

public class ActionPressureButton : BasePressureButton
{
    [Header("Action Settings")] 
    public bool isOneShot;
    public bool useTimer;

    [Header("Visual Cooldown & Material Settings")]
    [Tooltip("El componente DecalProjector que contiene el material de las runas.")]
    [SerializeField]
    private DecalProjector runesDecal;

    [Tooltip("El nombre exacto de la variable en tu Shader (ej: _Fill, _Emission, etc).")] 
    [SerializeField]
    private string shaderPropertyName = "_Progress";

    public UnityEngine.Events.UnityEvent OnActivated;
    public UnityEngine.Events.UnityEvent OnDeactivated;
    public UnityEngine.Events.UnityEvent OnFailedActivate;
    public UnityEngine.Events.UnityEvent OnFailedDeactivate;

    private bool hasBeenActivated;
    
    private Coroutine _transitionCoroutine;
    private float _currentVisualValue = 0f; 
    
    private bool _hasFocusedOnSuccess;
    private bool _hasFocusedOnFail;
 
    private Material _instancedMaterial;
    private int _shaderPropertyID;
    private FocusOnActivation focus => GetComponent<FocusOnActivation>();

    private void Start()
    {
        _shaderPropertyID = Shader.PropertyToID(shaderPropertyName);

        if (runesDecal != null && runesDecal.material != null)
        {
            _instancedMaterial = new Material(runesDecal.material);
            runesDecal.material = _instancedMaterial;
        }

        // Estado inicial: 0f (Cargado/Listo)
        _currentVisualValue = 0f;
        UpdateRuneVisual(_currentVisualValue);
    }

    protected override void OnPress()
    {
        base.OnPress(); 

        if (focus != null && !_hasFocusedOnSuccess)
        {
            focus.Activate();
            _hasFocusedOnSuccess = true; 
        }

        if (isOneShot && hasBeenActivated) return;

        if (_transitionCoroutine != null)
        {
            StopCoroutine(_transitionCoroutine);
            _transitionCoroutine = null;
        }

        if (!hasBeenActivated)
        {
            hasBeenActivated = true;
            OnActivated.Invoke();
        }

        if (isOneShot) 
        {
            this.enabled = false;
        }
        else
        {
            // Volvemos a cargarlo (0f) rápido si lo volvés a pisar
            _transitionCoroutine = StartCoroutine(TransitionMaterialRoutine(0f, 0.2f));
        }
    }

    protected override void OnRelease()
    {
        if (isOneShot) return;

        if (!hasBeenActivated)
        {
            base.OnRelease();
            OnFailedDeactivate.Invoke(); 
            return;
        }

        if (_transitionCoroutine != null) StopCoroutine(_transitionCoroutine);

        if (useTimer)
        {
            // Empezamos a transicionar hacia 1f (vacío/gastado).
            _transitionCoroutine = StartCoroutine(TransitionMaterialRoutine(1f, timer, () => 
            {
                base.OnRelease(); 
                Deactivate();     
            }));
        }
        else
        {
            base.OnRelease();
            Deactivate();
        }
    }

    protected override void OnFailedPress()
    {
        if (hasBeenActivated) return;
        
        if (focus != null && !_hasFocusedOnFail)
        {
            focus.Activate();
            _hasFocusedOnFail = true; 
        }
        
        OnFailed();
    }

    private void Deactivate()
    {
        hasBeenActivated = false;
        OnDeactivated.Invoke();

        if (_transitionCoroutine != null) StopCoroutine(_transitionCoroutine);
        _transitionCoroutine = StartCoroutine(TransitionMaterialRoutine(0f, 0.5f));

        currentState = ButtonState.Empty; 
    }

    private void OnFailed()
    {
        base.OnFailedPress(); 
        OnFailedActivate.Invoke();
    }

    private IEnumerator TransitionMaterialRoutine(float targetValue, float duration, Action onComplete = null)
    {
        float startValue = _currentVisualValue;
        float elapsedTime = 0f;

        if (duration > 0f)
        {
            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                _currentVisualValue = Mathf.Lerp(startValue, targetValue, elapsedTime / duration);
                UpdateRuneVisual(_currentVisualValue);

                yield return WaitWhilePaused(() => _paused);
            }
        }

        _currentVisualValue = targetValue;
        UpdateRuneVisual(_currentVisualValue);

        onComplete?.Invoke();
    }

    private void UpdateRuneVisual(float value)
    {
        if (_instancedMaterial != null)
        {
            _instancedMaterial.SetFloat(_shaderPropertyID, value);
        }
    }

    private void OnDestroy()
    {
        if (_instancedMaterial != null)
        {
            Destroy(_instancedMaterial);
        }
    }
}