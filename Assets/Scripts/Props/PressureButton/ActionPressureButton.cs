using UnityEngine;
using System.Collections;
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
    private Coroutine releaseTimerCoroutine;
    
    // Flags para controlar que el focus suceda una sola vez
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

        UpdateRuneVisual(0f);
    }

    protected override void OnPress()
    {
        base.OnPress(); // El botón baja físicamente.

        // Validamos si hay focus y si NO lo hemos activado por éxito aún
        if (focus != null && !_hasFocusedOnSuccess)
        {
            focus.Activate();
            _hasFocusedOnSuccess = true; // Lo marcamos como usado
        }

        if (isOneShot && hasBeenActivated) return;

        // Si pisan el botón mientras el timer de liberación está corriendo, lo cancelamos.
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
        if (isOneShot) return;

        // Si el botón nunca se activó con éxito (ej. se subió una momia de tamaño incorrecto),
        // dejamos que se levante físicamente de forma normal y abortamos.
        if (!hasBeenActivated)
        {
            base.OnRelease();
            OnFailedDeactivate.Invoke(); 
            return;
        }

        if (useTimer)
        {
            // IMPORTANTE: Si usamos timer, NO llamamos a base.OnRelease() todavía.
            if (releaseTimerCoroutine != null) StopCoroutine(releaseTimerCoroutine);
            releaseTimerCoroutine = StartCoroutine(TimerRoutine());
        }
        else
        {
            // Si no hay timer, se levanta físicamente y se desactiva al instante.
            base.OnRelease();
            Deactivate();
        }
    }

    protected override void OnFailedPress()
    {
        if (hasBeenActivated) return;
        
        // Validamos si hay focus y si NO lo hemos activado por fallo aún
        if (focus != null && !_hasFocusedOnFail)
        {
            focus.Activate();
            _hasFocusedOnFail = true; // Lo marcamos como usado
        }
        
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

            yield return WaitWhilePaused(() => _paused);
        }

        UpdateRuneVisual(1f);
        
        // AHORA SÍ: El timer terminó, levantamos el botón físicamente en el Animator.
        base.OnRelease(); 
        Deactivate();
    }

    private void Deactivate()
    {
        hasBeenActivated = false;
        OnDeactivated.Invoke();
    }

    private void OnFailed()
    {
        base.OnFailedPress(); // Dispara el trigger "Failed" en la clase base
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