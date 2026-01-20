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
    [SerializeField] private FxBank platformBank;
    [SerializeField] private string movingSoundKey = "Moving";   
    [SerializeField] private ParticleSystem sandMoundsParticle;
    [SerializeField] private ParticleSystem activationParticles;
    [SerializeField] private float glowDuration = 2f;
    [SerializeField] private float glowIntensity = 0.15f;
    
    // Estado interno
    private int _targetWaypointIndex = 0;
    private bool _isMoving;
    private bool _isWaitingAtWaypoint = false;
    
    // ⏸️ Estados de Pausa
    private bool _isGloballyPaused = false; // Pausa de Menú
    private bool _isLocked = false;         // Pausa de Evento/Cinemática

    private Material[] _platformMaterials;
    private Coroutine _waitCoroutine;

    // Componentes
    private AudioSource _movingAudio;
    // ❌ Rigidbody eliminado

    private void Start()
    {
        _platformMaterials = GetMaterialsFromChildren();
        _isMoving = isMovingOnStart;
        
        // ❌ Eliminada configuración de Rigidbody

        if (waypoints.Length == 0)
        {
            Debug.LogWarning("MovingPlatform no tiene waypoints asignados. Se desactivará.", this);
            _isMoving = false;
            return;
        }

        // Posicionamiento directo por Transform
        transform.position = waypoints[0].position; 

        if (_isMoving)
            _targetWaypointIndex = 1;
    }

    // 🔄 Cambiado a Update para movimiento visual suave sin físicas
    private void Update()
    {
        // 🔹 CONDICIÓN: Se detiene con Pausa O Lock
        bool shouldMove = !_isGloballyPaused 
                          && !_isLocked
                          && !_isWaitingAtWaypoint 
                          && _isMoving 
                          && waypoints.Length > 0;

        HandleEffects(shouldMove);
        HandleMovingSound(shouldMove);

        if (!shouldMove)
            return;

        MovePlatform();
    }

    private void MovePlatform()
    {
        Transform target = waypoints[_targetWaypointIndex];
        
        // Usamos transform.position en lugar de _rb.position
        float distance = Vector3.Distance(transform.position, target.position);
        const float slowDownRadius = 1f;

        float speedFactor = 1f;
        if (distance < slowDownRadius)
        {
            float t = distance / slowDownRadius;
            speedFactor = Mathf.Lerp(0.1f, 1f, t);
        }

        // Usamos deltaTime porque estamos en Update
        float step = speed * speedFactor * Time.deltaTime;
        
        // Movemos el transform directamente
        transform.position = Vector3.MoveTowards(transform.position, target.position, step);

        if (Vector3.Distance(transform.position, target.position) < 0.001f)
        {
            _waitCoroutine = StartCoroutine(PauseAtWaypoint());
        }
    }

    private void EnsureMovingAudio()
    {
        if (_movingAudio != null) return;
        if (platformBank == null || string.IsNullOrEmpty(movingSoundKey)) return;

        var entry = platformBank.Get(movingSoundKey);
        if (entry == null || entry.clip == null) return;

        _movingAudio = gameObject.AddComponent<AudioSource>();
        _movingAudio.clip         = entry.clip;
        _movingAudio.loop         = true;
        _movingAudio.playOnAwake  = false;
        _movingAudio.volume       = entry.volume;
        _movingAudio.pitch        = entry.pitch;
        _movingAudio.spatialBlend = entry.is3D ? entry.spatialBlend : 0f;
        _movingAudio.maxDistance  = entry.maxDistance;
        _movingAudio.rolloffMode  = AudioRolloffMode.Logarithmic;

        if (AudioManager.Instance != null)
        {
            var group = AudioManager.Instance.GetMixerGroup(platformBank.bus);
            if (group != null) _movingAudio.outputAudioMixerGroup = group;
        }
    }

    private void HandleMovingSound(bool isMovingActive)
    {
        if (isMovingActive)
        {
            EnsureMovingAudio();
            if (_movingAudio != null && !_movingAudio.isPlaying) _movingAudio.Play();
        }
        else
        {
            if (_movingAudio != null && _movingAudio.isPlaying) _movingAudio.Stop();
        }
    }

    private IEnumerator PauseAtWaypoint()
    {
        _isWaitingAtWaypoint = true;
        yield return WaitForSecondsPausable(stopTime, () => _isGloballyPaused || _isLocked); 
        SetNextTarget(1);
        _isWaitingAtWaypoint = false;
        _waitCoroutine = null;
    }

    public void StartAction()
    {
        _isMoving = !_isMoving;
        activationParticles?.Play();
        StartCoroutine(GlowEffect());
        
        var cam = GetComponent<FocusOnActivation>();
        cam?.Activate();

        if (_isMoving && _isWaitingAtWaypoint)
        {
            if (_waitCoroutine != null) StopCoroutine(_waitCoroutine);
            SetNextTarget(1);
            _isWaitingAtWaypoint = false;
        }
    }

    public void ReturnToPrevious()
    {
        if (_waitCoroutine != null)
        {
            StopCoroutine(_waitCoroutine);
            _waitCoroutine = null;
        }
        _isWaitingAtWaypoint = false;
        SetNextTarget(-1);
        _isMoving = true;
    }

    private void SetNextTarget(int direction)
    {
        _targetWaypointIndex = (_targetWaypointIndex + direction + waypoints.Length) % waypoints.Length;
    }

    private void HandleEffects(bool isMovingActive)
    {
        if (sandMoundsParticle == null) return;

        if (isMovingActive && !sandMoundsParticle.isPlaying && !_isGloballyPaused && !_isLocked)
        {
            sandMoundsParticle.Play();
        }
        else if (!isMovingActive && sandMoundsParticle.isPlaying)
        {
            sandMoundsParticle.Stop();
        }
    }

    #region Glow Effect
    private Material[] GetMaterialsFromChildren() =>
        GetComponentsInChildren<Renderer>().SelectMany(r => r.materials).ToArray();

    private IEnumerator GlowEffect()
    {
        yield return StartCoroutine(AnimateGlow(0f, glowIntensity, glowDuration / 2));
        platformBank.Play3D("Active", transform.position);
        yield return StartCoroutine(AnimateGlow(glowIntensity, 0f, glowDuration / 2));
    }

    private IEnumerator AnimateGlow(float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            while (_isGloballyPaused || _isLocked) yield return null; 
            
            elapsed += Time.deltaTime;
            float current = Mathf.Lerp(from, to, elapsed / duration);
            SetGlow(current);
            yield return null;
        }
        SetGlow(to);
    }

    private void SetGlow(float intensity)
    {
        foreach (var mat in _platformMaterials)
            if (mat.HasProperty("_GlowIntensity"))
                mat.SetFloat("_GlowIntensity", intensity);
    }
    #endregion

    #region Pause & Lock System
    
    public void OnPauseChanged(bool paused)
    {
        _isGloballyPaused = paused;
        UpdatePauseState();
    }

    public void OnLockChanged(bool locked)
    {
        _isLocked = locked;
        UpdatePauseState();
    }

    private void UpdatePauseState()
    {
        bool isFrozen = _isGloballyPaused || _isLocked;

        if (sandMoundsParticle)
        {
            if (isFrozen && sandMoundsParticle.isPlaying) 
                sandMoundsParticle.Pause();
            else if (!isFrozen && _isMoving && !_isWaitingAtWaypoint) 
                sandMoundsParticle.Play();
        }

        if (_movingAudio != null)
        {
            if (isFrozen && _movingAudio.isPlaying) 
                _movingAudio.Pause();
            else if (!isFrozen && _isMoving && !_isWaitingAtWaypoint) 
                _movingAudio.UnPause();
        }
    }

    private void OnEnable()
    {
        GameEventManager.Instance.levelEvents.OnPauseChanged.Register<bool>(OnPauseChanged);
        GameEventManager.Instance.playerEvents.OnLocked.Register<bool>(OnLockChanged);
    }

    private void OnDisable()
    {
        if (GameEventManager.Instance == null) return;
        
        GameEventManager.Instance.levelEvents.OnPauseChanged.Unregister<bool>(OnPauseChanged);
        GameEventManager.Instance.playerEvents.OnLocked.Unregister<bool>(OnLockChanged);
    }
    #endregion

    #region Player Collision Logic (Requerido para Transform)
    // ⚠️ IMPORTANTE: Al mover por Transform, el jugador NO se moverá con la plataforma
    // automáticamente (se resbalará). Necesitas emparentarlo.
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) 
        {
            other.transform.SetParent(this.transform);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            other.transform.SetParent(null);
            // Opcional: DontDestroyOnLoad puede requerir lógica extra si tu player es persistente
        }
    }
    #endregion
}