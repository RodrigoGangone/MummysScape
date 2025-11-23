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
    [SerializeField] private string movingSoundKey = "Moving";   // 🔹 key en el bank
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

    // 🔹 Audio interno de la plataforma (una sola instancia)
    private AudioSource _movingAudio;

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

        transform.position = waypoints[0].position;

        if (_isMoving)
            _targetWaypointIndex = 1;
    }

    private void FixedUpdate()
    {
        bool shouldMove = !_isGloballyPaused 
                          && !_isWaitingAtWaypoint 
                          && _isMoving 
                          && waypoints.Length > 0;

        HandleEffects(shouldMove);
        HandleMovingSound(shouldMove);   // 🔹 aquí controlamos el sonido

        if (!shouldMove)
            return;

        MovePlatform();
    }

    /// <summary>
    /// Crea y configura el AudioSource 3D si aún no existe.
    /// </summary>
    private void EnsureMovingAudio()
    {
        if (_movingAudio != null)
            return;

        if (platformBank == null || string.IsNullOrEmpty(movingSoundKey))
        {
            Debug.LogWarning("MovingPlatform: No hay bank o key configurados para el sonido de movimiento.", this);
            return;
        }

        var entry = platformBank.Get(movingSoundKey);
        if (entry == null || entry.clip == null)
        {
            Debug.LogWarning($"MovingPlatform: key '{movingSoundKey}' no encontrada en bank '{platformBank.name}'.", this);
            return;
        }

        _movingAudio = gameObject.AddComponent<AudioSource>();
        _movingAudio.clip         = entry.clip;
        _movingAudio.loop         = true;
        _movingAudio.playOnAwake  = false;
        _movingAudio.volume       = entry.volume;
        _movingAudio.pitch        = entry.pitch;
        _movingAudio.spatialBlend = entry.is3D ? entry.spatialBlend : 0f;
        _movingAudio.maxDistance  = entry.maxDistance;
        _movingAudio.rolloffMode  = AudioRolloffMode.Logarithmic;

        // Opcional: mandarlo al mixer correcto según el bus del bank
        if (AudioManager.Instance != null)
        {
            var group = AudioManager.Instance.GetMixerGroup(platformBank.bus);
            if (group != null)
                _movingAudio.outputAudioMixerGroup = group;
        }
    }

    /// <summary>
    /// Enciende o apaga el loop de movimiento según si la plataforma se mueve o no.
    /// </summary>
    private void HandleMovingSound(bool isMovingActive)
    {
        if (isMovingActive)
        {
            EnsureMovingAudio();

            if (_movingAudio != null && !_movingAudio.isPlaying)
                _movingAudio.Play();
        }
        else
        {
            if (_movingAudio != null && _movingAudio.isPlaying)
                _movingAudio.Stop();
        }
    }

    private void MovePlatform()
    {
        Transform target = waypoints[_targetWaypointIndex];

        float distance = Vector3.Distance(transform.position, target.position);

        const float slowDownRadius = 1f;

        float speedFactor = 1f;
        if (distance < slowDownRadius)
        {
            float t = distance / slowDownRadius;
            speedFactor = Mathf.Lerp(0.1f, 1f, t);
        }

        float step = speed * speedFactor * Time.deltaTime;
        transform.position = Vector3.MoveTowards(transform.position, target.position, step);

        if (Vector3.Distance(transform.position, target.position) < 0.001f)
        {
            _waitCoroutine = StartCoroutine(PauseAtWaypoint());
        }
    }

    private IEnumerator PauseAtWaypoint()
    {
        _isWaitingAtWaypoint = true;
        
        yield return WaitForSecondsPausable(stopTime, () => _isGloballyPaused); 
        
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
            if (_waitCoroutine != null)
                StopCoroutine(_waitCoroutine);
            
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

        if (isMovingActive && !sandMoundsParticle.isPlaying)
        {
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
        yield return StartCoroutine(AnimateGlow(0f, glowIntensity, glowDuration / 2));
        
        platformBank.Play3D("Active", transform.position);
        
        yield return StartCoroutine(AnimateGlow(glowIntensity, 0f, glowDuration / 2));
    }

    private IEnumerator AnimateGlow(float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            while (_isGloballyPaused)
                yield return null; 
            
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

    #region Pause System
    public void OnPauseChanged(bool paused)
    {
        _isGloballyPaused = paused;

        if (sandMoundsParticle)
        {
            if (paused && sandMoundsParticle.isPlaying)
                sandMoundsParticle.Pause();
            else if (!paused && _isMoving && !_isWaitingAtWaypoint) 
                sandMoundsParticle.Play();
        }

        if (_movingAudio != null)
        {
            if (paused && _movingAudio.isPlaying)
                _movingAudio.Pause();
            else if (!paused && _isMoving && !_isWaitingAtWaypoint)
                _movingAudio.UnPause();
        }
    }

    private void OnEnable() => GameEventManager.Instance.levelEvents.OnPauseChanged.Register<bool>(OnPauseChanged);
    private void OnDisable() => GameEventManager.Instance.levelEvents.OnPauseChanged.Unregister<bool>(OnPauseChanged);
    #endregion
}
