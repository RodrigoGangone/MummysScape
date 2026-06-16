using UnityEngine;
using System.Collections;
using UnityEngine.Rendering.Universal;

public class ActionPressureButton : BasePressureButton
{
    [Header("Action Settings")]
    public bool isOneShot;
    public bool useTimer; 

    [Header("Visual Cooldown & Material Settings")]
    [Tooltip("El componente DecalProjector que contiene el material de las runas.")]
    [SerializeField] private DecalProjector runesDecal;
    [Tooltip("El nombre exacto de la variable en tu Shader (ej: _Fill, _Emission, etc).")]
    [SerializeField] private string shaderPropertyName = "_Progress";

    public UnityEngine.Events.UnityEvent OnActivated;
    public UnityEngine.Events.UnityEvent OnDeactivated;
    public UnityEngine.Events.UnityEvent OnFailedActivate;

    private bool hasBeenActivated;
    
    private Coroutine releaseTimerCoroutine;

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

        UpdateRuneVisual(0f);
    }

    protected override void OnPress()
    {
        base.OnPress(); 
        
        if(focus != null) focus.Activate();
        
        if ((isOneShot && hasBeenActivated)) return;

        if (releaseTimerCoroutine != null)
        {
            StopCoroutine(releaseTimerCoroutine);
            releaseTimerCoroutine = null;
            UpdateRuneVisual(0f);
        }

        if (!hasBeenActivated)
        {
            hasBeenActivated = true;
            OnActivated.Invoke();
        }

        if (isOneShot) this.enabled = false;
    }

    protected override void OnRelease()
    {
        // Esto subirá el botón físicamente (animación base), lo cual es correcto
        // porque la momia (aunque sea la incorrecta) se bajó del botón.
        base.OnRelease(); 
        
        if (isOneShot) return;

        // SOLUCIÓN: Si el botón nunca se activó con éxito (ej. venimos de un estado Failed),
        // abortamos la lógica de desactivación/timer para que no se pisen.
        if (!hasBeenActivated) return;

        if (useTimer)
        {
            if (releaseTimerCoroutine != null) StopCoroutine(releaseTimerCoroutine);
            releaseTimerCoroutine = StartCoroutine(TimerRoutine());
        }
        else
        {
            Deactivate();
        }
    }

    protected override void OnFailedPress()
    {
        // IMPORTANTE: Lo tenías comentado. Si quieres que el Animator en la clase base
        // ejecute _animator.SetTrigger("Failed"), necesitas descomentar esta línea.
        //base.OnFailedPress(); 
        if (hasBeenActivated) return;
        
        OnFailed();
    }

    private IEnumerator TimerRoutine()
    {
        float elapsedTime = 0f;

        while (elapsedTime < timer)
        {
            elapsedTime += Time.deltaTime;
            float fillValue = Mathf.Clamp01(elapsedTime / timer);
            UpdateRuneVisual(fillValue);
            
            yield return null;
        }

        UpdateRuneVisual(1f);
        Deactivate();
    }

    private void Deactivate()
    {
        hasBeenActivated = false;
        OnDeactivated.Invoke();
    }

    private void OnFailed()
    {
        OnFailedActivate.Invoke();
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