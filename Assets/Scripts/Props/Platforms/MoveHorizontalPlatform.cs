using System.Collections;
using System.Linq;
using UnityEngine;
using static Utils;
using static PauseUtils;

public class MoveHorizontalPlatform : MonoBehaviour, IPausable
{
    [Header("PLAY ON AWAKE")] 
    [SerializeField] private bool isMoving;

    [Header("SPEED")] 
    [SerializeField] private float speed = 1f;
    [SerializeField] private float stopTime = 0.5f;

    [Header("WAYPOINTS")] 
    [SerializeField] private Transform[] waypoints;

    [Header("AUDIO & FX")] 
    [SerializeField] private FxBank platformBank;
    [SerializeField] private string movingSoundKey = "Moving"; // Key en el bank
    [SerializeField] private string activeSoundKey = "Active"; // Sonido al activar
    [SerializeField] private float glowDuration = 2f;
    [SerializeField] private float glowIntensity = 0.15f;

    [Header("SAND MOUNDS EFFECTS")] 
    [SerializeField] private float moundEmergenceSpeed = 1f;
    [SerializeField] private Transform sandMoundForward;
    [SerializeField] private Transform[] sandMoundForwardWaypoints;
    [SerializeField] private Transform sandMoundBackward;
    [SerializeField] private Transform[] sandMoundBackwardWaypoints;

    [SerializeField] private ParticleSystem[] sandMoundForwardParticles;
    [SerializeField] private ParticleSystem[] sandMoundBackwardParticles;
    [SerializeField] private ParticleSystem activationParticles;

    private Material[] platformMaterials;
    private bool isMovingToFirstWaypoint;
    private int _currentWaypointIndex = 0;

    // Audio interno
    private AudioSource _movingAudio;

    // ⏸️ Estados
    private bool _paused;
    private bool _holdAtWaypoint;
    
    private void Start()
    {
        platformMaterials = GetMaterialsFromChildren();

        if (waypoints.Length > 0 && isMoving)
        {
            if (Vector3.Distance(transform.position, waypoints[0].position) == 0)
                isMovingToFirstWaypoint = true;
            else
                transform.position = waypoints[0].position;
        }

        if (sandMoundForward && sandMoundBackward)
        {
            sandMoundForward.position  = sandMoundForwardWaypoints[0].position;
            sandMoundBackward.position = sandMoundBackwardWaypoints[0].position;
        }
    }

    private void FixedUpdate()
    {
        // Determinamos si debe moverse
        bool shouldMove = !_paused 
                          && !_holdAtWaypoint 
                          && isMoving 
                          && waypoints.Length > 0;

        // Manejamos el sonido en base al estado de movimiento
        HandleMovingSound(shouldMove);

        if (!shouldMove) return;

        // Lógica de movimiento original
        if (isMovingToFirstWaypoint)
            MoveToFirstWaypoint();
        else
            MoveTowardsWaypoint();
    }

    // ---------------- AUDIO SYSTEM ----------------
    
    private void EnsureMovingAudio()
    {
        if (_movingAudio != null) return;

        if (platformBank == null || string.IsNullOrEmpty(movingSoundKey))
        {
            Debug.LogWarning("MovingHorizontalPlatform: No hay bank o key configurados.", this);
            return;
        }

        var entry = platformBank.Get(movingSoundKey);
        if (entry == null || entry.clip == null) return;

        _movingAudio = gameObject.AddComponent<AudioSource>();
        _movingAudio.clip = entry.clip;
        _movingAudio.loop = true;
        _movingAudio.playOnAwake = false;
        _movingAudio.volume = entry.volume;
        _movingAudio.pitch = entry.pitch;
        _movingAudio.spatialBlend = entry.is3D ? entry.spatialBlend : 0f;
        _movingAudio.maxDistance = entry.maxDistance;
        _movingAudio.rolloffMode = AudioRolloffMode.Logarithmic;

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
            if (_movingAudio != null && !_movingAudio.isPlaying)
                _movingAudio.Play();
        }
        else
        {
            if (_movingAudio != null && _movingAudio.isPlaying)
                _movingAudio.Stop();
        }
    }

    // ---------------- MOVEMENT LOGIC ----------------

    private void MoveToFirstWaypoint()
    {
        Transform firstWaypoint = waypoints[0];
        float step = speed * Time.deltaTime;

        transform.position = Vector3.MoveTowards(transform.position, firstWaypoint.position, step);

        if (Vector3.Distance(transform.position, firstWaypoint.position) <= 0.001f)
        {
            isMovingToFirstWaypoint = false;
            _currentWaypointIndex = 1;
        }
    }

    private void MoveTowardsWaypoint()
    {
        // Esta comprobación ya se hace en FixedUpdate, pero se mantiene por seguridad de la lógica interna de montículos
        if (_holdAtWaypoint)
        {
            if (sandMoundForward && sandMoundBackward)
                ResetSandMoundPositions();
            return;
        }

        Transform targetWaypoint = waypoints[_currentWaypointIndex];
        float step = speed * Time.deltaTime;

        transform.position = Vector3.MoveTowards(transform.position, targetWaypoint.position, step);

        if (Vector3.Distance(transform.position, targetWaypoint.position) <= 0.01f)
            StartCoroutine(PauseAtWaypoint());

        if (sandMoundForward && sandMoundBackward)
            MoveSandMound();
    }

    private IEnumerator PauseAtWaypoint()
    {
        _holdAtWaypoint = true;
        yield return WaitForSecondsPausable(stopTime, () => _paused);
        _currentWaypointIndex = (_currentWaypointIndex + 1) % waypoints.Length;
        _holdAtWaypoint = false;
    }

    // ---------------- INTERACTION & EVENTS ----------------

    public void StartAction()
    {
        isMoving = !isMoving;
        activationParticles?.Play();
        StartCoroutine(GlowEffect(glowDuration));
        
        // 🔹 Activación de cámara (Focus)
        var cam = GetComponent<FocusOnActivation>();
        cam?.Activate();
    }

    public void ReturnToPrevious()
    {
        _currentWaypointIndex = _currentWaypointIndex > 0 ? _currentWaypointIndex - 1 : _currentWaypointIndex + 1;
    }

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

    // ---------------- SAND MOUND LOGIC ----------------

    private void ResetSandMoundPositions()
    {
        sandMoundForward.position = Vector3.MoveTowards(
            sandMoundForward.position, sandMoundForwardWaypoints[0].position, moundEmergenceSpeed * Time.deltaTime);
        sandMoundBackward.position = Vector3.MoveTowards(
            sandMoundBackward.position, sandMoundBackwardWaypoints[0].position, moundEmergenceSpeed * Time.deltaTime);
    }

    private void MoveSandMound()
    {
        Transform targetWaypoint = sandMoundForwardWaypoints[_currentWaypointIndex];
        int inverseIndex = (sandMoundForwardWaypoints.Length - 1) - _currentWaypointIndex;
        Transform targetBackward = sandMoundBackwardWaypoints[inverseIndex];

        sandMoundForward.position = Vector3.MoveTowards(
            sandMoundForward.position, targetWaypoint.position, moundEmergenceSpeed * Time.deltaTime);
        sandMoundBackward.position = Vector3.MoveTowards(
            sandMoundBackward.position, targetBackward.position, moundEmergenceSpeed * Time.deltaTime);

        // Control de partículas de los montículos
        if (_currentWaypointIndex == 1)
        {
            foreach (var ps in sandMoundForwardParticles) if (!ps.isPlaying && !_paused) ps.Play();
            foreach (var ps in sandMoundBackwardParticles) ps.Stop();
        }
        else if (_currentWaypointIndex == 0)
        {
            foreach (var ps in sandMoundBackwardParticles) if (!ps.isPlaying && !_paused) ps.Play();
            foreach (var ps in sandMoundForwardParticles) ps.Stop();
        }
    }

    // ---------------- VISUALS ----------------

    private Material[] GetMaterialsFromChildren() =>
        GetComponentsInChildren<Renderer>().SelectMany(r => r.materials).ToArray();

    private IEnumerator GlowEffect(float duration)
    {
        StartCoroutine(IncreaseIntensity(duration / 2f));
        
        // 🔹 Sonido de activación desde el Bank
        if(platformBank != null) platformBank.Play3D(activeSoundKey, transform.position);

        yield return WaitForSecondsPausable(duration / 2f, () => _paused);
        StartCoroutine(DecreaseIntensity(duration / 2f));
    }

    private IEnumerator IncreaseIntensity(float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            if (_paused) { yield return WaitWhilePaused(() => _paused); continue; }

            elapsed += Time.deltaTime;
            float current = Mathf.Lerp(0f, glowIntensity, elapsed / duration);
            foreach (var mat in platformMaterials)
                if (mat.HasProperty("_GlowIntensity")) mat.SetFloat("_GlowIntensity", current);
            yield return null;
        }
    }

    private IEnumerator DecreaseIntensity(float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            if (_paused) { yield return WaitWhilePaused(() => _paused); continue; }

            elapsed += Time.deltaTime;
            float current = Mathf.Lerp(glowIntensity, 0f, elapsed / duration);
            foreach (var mat in platformMaterials)
                if (mat.HasProperty("_GlowIntensity")) mat.SetFloat("_GlowIntensity", current);
            yield return null;
        }
    }

    // ---------------- PAUSE SYSTEM ----------------

    public void OnPauseChanged(bool paused)
    {
        _paused = paused;

        // Pausa de partículas de los montículos
        HandleParticlesPause(sandMoundForwardParticles, paused);
        HandleParticlesPause(sandMoundBackwardParticles, paused);

        // Pausa del Audio
        if (_movingAudio != null)
        {
            if (paused && _movingAudio.isPlaying) _movingAudio.Pause();
            else if (!paused && !_movingAudio.isPlaying && isMoving && !_holdAtWaypoint) _movingAudio.UnPause();
        }
    }

    private void HandleParticlesPause(ParticleSystem[] systems, bool paused)
    {
        foreach (var ps in systems)
        {
            if (paused && ps.isPlaying) ps.Pause();
            else if (!paused && ps.isPaused) ps.Play(); 
        }
    }

    private void OnEnable() => GameEventManager.Instance.levelEvents.OnPauseChanged.Register<bool>(OnPauseChanged);
    private void OnDisable() => GameEventManager.Instance.levelEvents.OnPauseChanged.Unregister<bool>(OnPauseChanged);
}