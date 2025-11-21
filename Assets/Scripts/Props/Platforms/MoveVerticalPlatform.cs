using System.Collections;
using System.Linq;
using UnityEngine;
using static Utils;
using static PauseUtils;

public class MoveVerticalPlatform : MonoBehaviour, IPausable
{
    [Header("SETTINGS")]
    [SerializeField] private bool isMovingOnStart = true;
    [SerializeField] private float speed = 1f;
    [SerializeField] private float stopTime = 0.5f;

    [Header("WAYPOINTS")]
    [SerializeField] private Transform[] waypoints;

    [Header("EFFECTS")]
    [SerializeField] private ParticleSystem sandMoundsParticle;
    [SerializeField] private ParticleSystem activationParticles;
    [SerializeField] private float glowDuration = 2f;
    [SerializeField] private float glowIntensity = 0.15f;
    
    // Estado interno
    private int _targetWaypointIndex = 0;
    private bool _isMoving;
    private bool _isWaitingAtWaypoint = false;
    private bool _isGloballyPaused = false;

    private Material[] _platformMaterials;
    private Coroutine _waitCoroutine;

    private void Start()
    {
        _platformMaterials = GetMaterialsFromChildren();
        _isMoving = isMovingOnStart;

        if (waypoints.Length == 0)
        {
            Debug.LogWarning("MovingPlatform no tiene waypoints asignados. Se desactivará.", this);
            _isMoving = false;
            return;
        }

        // Iniciar en la posición del primer waypoint
        transform.position = waypoints[0].position;
        // Si se mueve al empezar, el primer objetivo es el waypoint 1
        if (_isMoving)
            _targetWaypointIndex = 1;
    }

    private void FixedUpdate()
    {
        // El movimiento se detiene si está pausado, esperando, o desactivado.
        if (_isGloballyPaused || _isWaitingAtWaypoint || !_isMoving || waypoints.Length == 0)
        {
            HandleEffects(false); // Asegurarse de detener efectos si no se mueve
            return;
        }

        HandleEffects(true); // Reproducir efectos al moverse
        MovePlatform();
    }

    /// <summary>
    /// Lógica principal de movimiento hacia el waypoint objetivo.
    /// </summary>
    /// <summary>
    /// Lógica principal de movimiento hacia el waypoint objetivo.
    /// </summary>
    private void MovePlatform()
    {
        Transform target = waypoints[_targetWaypointIndex];

        // Distancia actual al waypoint
        float distance = Vector3.Distance(transform.position, target.position);

        // Radio en el que empieza a frenar (valor chico en unidades de mundo)
        const float slowDownRadius = 1f;

        // 1 = velocidad normal lejos, 0.2 = bien lento pegado al final
        float speedFactor = 1f;
        if (distance < slowDownRadius)
        {
            float t = distance / slowDownRadius;          // 1 lejos, 0 en el punto
            speedFactor = Mathf.Lerp(0.1f, 1f, t);        // va bajando de 1 a 0.2
        }

        float step = speed * speedFactor * Time.deltaTime;
        transform.position = Vector3.MoveTowards(transform.position, target.position, step);

        // Si llega al destino, inicia la pausa en el waypoint
        if (Vector3.Distance(transform.position, target.position) < 0.001f)
        {
            _waitCoroutine = StartCoroutine(PauseAtWaypoint());
        }
    }


    /// <summary>
    /// Espera en el waypoint actual antes de moverse al siguiente.
    /// </summary>
    private IEnumerator PauseAtWaypoint()
    {
        _isWaitingAtWaypoint = true;
        
        // Asumiendo que 'WaitForSecondsPausable' maneja la pausa global internamente.
        // Si no, deberías usar un 'yield return new WaitForSeconds(stopTime)' 
        // y manejar la pausa global en el Update.
        yield return WaitForSecondsPausable(stopTime, () => _isGloballyPaused); 
        
        SetNextTarget(1); // Moverse al siguiente
        _isWaitingAtWaypoint = false;
        _waitCoroutine = null;
    }

    /// <summary>
    /// Activa o desactiva el movimiento de la plataforma.
    /// </summary>
    public void StartAction()
    {
        _isMoving = !_isMoving;
        activationParticles?.Play();
        StartCoroutine(GlowEffect());

        var cam = GetComponent<FocusOnActivation>();
        
        cam.Activate();

        // Si se activa mientras estaba esperando, cancela la espera y se mueve ya.
        if (_isMoving && _isWaitingAtWaypoint)
        {
            if (_waitCoroutine != null)
                StopCoroutine(_waitCoroutine);
            
            SetNextTarget(1);
            _isWaitingAtWaypoint = false;
        }
    }

    /// <summary>
    /// Hace que la plataforma regrese al waypoint anterior.
    /// </summary>
    public void ReturnToPrevious()
    {
        // Si estaba esperando, cancela la espera
        if (_waitCoroutine != null)
        {
            StopCoroutine(_waitCoroutine);
            _waitCoroutine = null;
        }

        _isWaitingAtWaypoint = false;
        SetNextTarget(-1); // Moverse al anterior
        _isMoving = true; // Forzar el movimiento
    }

    /// <summary>
    /// Establece el siguiente waypoint objetivo, con un 'direction' de 1 (siguiente) o -1 (anterior).
    /// </summary>
    private void SetNextTarget(int direction)
    {
        // (índice + dirección + total) % total
        // Esta fórmula de módulo maneja correctamente los números negativos
        _targetWaypointIndex = (_targetWaypointIndex + direction + waypoints.Length) % waypoints.Length;
    }

    /// <summary>
    /// Maneja el estado (Play/Stop) de las partículas de movimiento.
    /// </summary>
    private void HandleEffects(bool isMovingActive)
    {
        if (sandMoundsParticle == null) return;

        if (isMovingActive && !sandMoundsParticle.isPlaying)
        {
            // No reproducir si está en pausa global (OnPauseChanged lo manejará)
            if (!_isGloballyPaused) 
                sandMoundsParticle.Play();
        }
        else if (!isMovingActive && sandMoundsParticle.isPlaying)
        {
            sandMoundsParticle.Stop();
        }
    }

    #region Player Parenting
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(PLAYER_TAG))
            other.transform.SetParent(transform);
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(PLAYER_TAG))
            other.transform.SetParent(null);
    }
    #endregion

    #region Glow Effect
    private Material[] GetMaterialsFromChildren() =>
        GetComponentsInChildren<Renderer>().SelectMany(r => r.materials).ToArray();

    private IEnumerator GlowEffect()
    {
        // Pasar de 0 al máximo, y luego del máximo a 0
        yield return StartCoroutine(AnimateGlow(0f, glowIntensity, glowDuration / 2));
        yield return StartCoroutine(AnimateGlow(glowIntensity, 0f, glowDuration / 2));
    }

    /// <summary>
    /// Corrutina reutilizable para animar el 'glow' de un valor a otro.
    /// </summary>
    private IEnumerator AnimateGlow(float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            // Espera simple mientras esté en pausa global
            while (_isGloballyPaused)
                yield return null; 
            
            elapsed += Time.deltaTime;
            float current = Mathf.Lerp(from, to, elapsed / duration);
            SetGlow(current);
            
            yield return null;
        }
        SetGlow(to); // Asegurarse de que termina en el valor exacto
    }

    /// <summary>
    /// Aplica el valor de intensidad a todos los materiales.
    /// </summary>
    private void SetGlow(float intensity)
    {
        foreach (var mat in _platformMaterials)
            if (mat.HasProperty("_GlowIntensity"))
                mat.SetFloat("_GlowIntensity", intensity);
    }
    #endregion

    #region Pause System
    public void OnPauseChanged(bool paused)
    {
        _isGloballyPaused = paused;

        if (sandMoundsParticle)
        {
            if (paused && sandMoundsParticle.isPlaying)
                sandMoundsParticle.Pause();
            // Si se des-pausa y DEBERÍA estar moviéndose, reanuda.
            else if (!paused && _isMoving && !_isWaitingAtWaypoint) 
                sandMoundsParticle.Play();
        }
        
        // Aquí también iría la lógica de pausar/despausar el audio
        // if (paused && _platformAudio.isPlaying) _platformAudio.Pause();
        // else if (!paused && _isMoving && !_isWaitingAtWaypoint) _platformAudio.Play();
    }

    private void OnEnable() => GameEventManager.Instance.levelEvents.OnPauseChanged.Register<bool>(OnPauseChanged);
    private void OnDisable() => GameEventManager.Instance.levelEvents.OnPauseChanged.Unregister<bool>(OnPauseChanged);
    #endregion
}