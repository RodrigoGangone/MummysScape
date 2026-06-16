using UnityEngine;
using System.Collections;
using UnityEngine.Rendering.Universal; // Necesario para acceder al DecalProjector

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

    private bool hasBeenActivated;
    
    private Coroutine releaseTimerCoroutine;

    // Variables para manejar el material instanciado
    private Material _instancedMaterial;
    private int _shaderPropertyID;
    private FocusOnActivation focus => GetComponent<FocusOnActivation>();

    private void Start()
    {
        _shaderPropertyID = Shader.PropertyToID(shaderPropertyName);
        
        // Si tenemos asignado el Decal, le creamos una instancia única de su material
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
        base.OnRelease(); 
        
        if (isOneShot) return;

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

    private void UpdateRuneVisual(float value)
    {
        // Modificamos directamente el material instanciado
        if (_instancedMaterial != null)
        {
            _instancedMaterial.SetFloat(_shaderPropertyID, value);
        }
    }

    private void OnDestroy()
    {
        // Es vital destruir el material clonado al destruir el objeto para liberar la memoria
        if (_instancedMaterial != null)
        {
            Destroy(_instancedMaterial);
        }
    }
}