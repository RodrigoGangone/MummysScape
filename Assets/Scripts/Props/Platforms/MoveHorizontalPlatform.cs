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
    [SerializeField] private string movingSoundKey = "Moving"; 
    [SerializeField] private string activeSoundKey = "Active"; 
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

    private AudioSource _movingAudio;

    // ⏸️ Estados
    private bool _paused;     
    private bool _isLocked;   
    private bool _holdAtWaypoint;

    private void Start()
    {
        platformMaterials = GetMaterialsFromChildren();
        
        // Se eliminó la configuración de Rigidbody.isKinematic

        if (waypoints.Length > 0 && isMoving)
        {
            if (Vector3.Distance(transform.position, waypoints[0].position) < 0.001f)
                isMovingToFirstWaypoint = true;
            else
                transform.position = waypoints[0].position;
        }

        if (sandMoundForward && sandMoundBackward)
        {
            if (sandMoundForwardWaypoints.Length > 0)
                sandMoundForward.position = sandMoundForwardWaypoints[0].position;
            
            if (sandMoundBackwardWaypoints.Length > 0)
                sandMoundBackward.position = sandMoundBackwardWaypoints[0].position;
        }
    }

    private void Update()
    {
        // Se detiene si hay pausa de menú o bloqueo de evento (Locked)
        bool shouldMove = !_paused 
                          && !_isLocked 
                          && !_holdAtWaypoint
                          && isMoving
                          && waypoints.Length > 0;

        HandleMovingSound(shouldMove);

        if (!shouldMove) return;

        if (isMovingToFirstWaypoint)
            MoveToFirstWaypoint();
        else
            MoveTowardsWaypoint();
    }

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

    private void EnsureMovingAudio()
    {
        if (_movingAudio != null) return;
        if (platformBank == null || string.IsNullOrEmpty(movingSoundKey)) return;

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
            if (_movingAudio != null && !_movingAudio.isPlaying) _movingAudio.Play();
        }
        else
        {
            if (_movingAudio != null && _movingAudio.isPlaying) _movingAudio.Stop();
        }
    }

    private IEnumerator PauseAtWaypoint()
    {
        _holdAtWaypoint = true;
        yield return WaitForSecondsPausable(stopTime, () => _paused || _isLocked);
        _currentWaypointIndex = (_currentWaypointIndex + 1) % waypoints.Length;
        _holdAtWaypoint = false;
    }

    public void StartAction()
    {
        isMoving = !isMoving;
        activationParticles?.Play();
        StartCoroutine(GlowEffect(glowDuration));
        var cam = GetComponent<FocusOnActivation>();
        cam?.Activate();
    }
    
    public void ReturnToPrevious()
    {
        _currentWaypointIndex = _currentWaypointIndex > 0 ? _currentWaypointIndex - 1 : waypoints.Length - 1;
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

        if (_currentWaypointIndex == 1)
        {
            foreach (var ps in sandMoundForwardParticles)
                if (!ps.isPlaying && !_paused && !_isLocked) ps.Play();
            foreach (var ps in sandMoundBackwardParticles) ps.Stop();
        }
        else
        {
            foreach (var ps in sandMoundBackwardParticles)
                if (!ps.isPlaying && !_paused && !_isLocked) ps.Play();
            foreach (var ps in sandMoundForwardParticles) ps.Stop();
        }
    }
    
    // ---------------- VISUALS & GLOW ----------------
    private Material[] GetMaterialsFromChildren() =>
        GetComponentsInChildren<Renderer>().SelectMany(r => r.materials).ToArray();

    private IEnumerator GlowEffect(float duration)
    {
        StartCoroutine(AnimateGlow(0f, glowIntensity, duration / 2f));
        if (platformBank != null) platformBank.Play3D(activeSoundKey, transform.position);
        yield return WaitForSecondsPausable(duration / 2f, () => _paused || _isLocked);
        StartCoroutine(AnimateGlow(glowIntensity, 0f, duration / 2f));
    }

    private IEnumerator AnimateGlow(float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            if (_paused || _isLocked) { yield return null; continue; }
            elapsed += Time.deltaTime;
            float current = Mathf.Lerp(from, to, elapsed / duration);
            foreach (var mat in platformMaterials)
                if (mat.HasProperty("_GlowIntensity")) mat.SetFloat("_GlowIntensity", current);
            yield return null;
        }
    }

    // ---------------- EVENT HANDLERS ----------------

    public void OnPauseChanged(bool paused)
    {
        _paused = paused;
        UpdateState();
    }

    public void OnLockChanged(bool locked)
    {
        _isLocked = locked;
        UpdateState();
    }

    private void UpdateState()
    {
        bool isFrozen = _paused || _isLocked;

        HandleParticlesPause(sandMoundForwardParticles, isFrozen);
        HandleParticlesPause(sandMoundBackwardParticles, isFrozen);

        if (_movingAudio != null)
        {
            if (isFrozen && _movingAudio.isPlaying) 
                _movingAudio.Pause();
            else if (!isFrozen && !_movingAudio.isPlaying && isMoving && !_holdAtWaypoint) 
                _movingAudio.UnPause();
        }
    }

    private void HandleParticlesPause(ParticleSystem[] systems, bool isFrozen)
    {
        if (systems == null) return;
        foreach (var ps in systems)
        {
            if (ps == null) continue;
            if (isFrozen && ps.isPlaying) ps.Pause();
            else if (!isFrozen && ps.isPaused) ps.Play();
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

    #region Player Collision Logic
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) 
            other.transform.SetParent(this.transform);
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
            other.transform.SetParent(null);
    }
    #endregion
}