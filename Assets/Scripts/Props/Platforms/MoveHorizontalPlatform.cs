using System.Collections;
using System.Linq;
using UnityEngine;
using static Utils;
using static PauseUtils;

[RequireComponent(typeof(Rigidbody))]
public class MoveHorizontalPlatform : MonoBehaviour, IPausable
{
    [Header("PLAY ON AWAKE")] [SerializeField]
    private bool isMoving;

    [Header("SPEED")] [SerializeField] private float speed = 1f;
    [SerializeField] private float stopTime = 0.5f;

    [Header("WAYPOINTS")] [SerializeField] private Transform[] waypoints;

    [Header("AUDIO & FX")] [SerializeField]
    private FxBank platformBank;

    [SerializeField] private string movingSoundKey = "Moving"; 
    [SerializeField] private string activeSoundKey = "Active"; 
    [SerializeField] private float glowDuration = 2f;
    [SerializeField] private float glowIntensity = 0.15f;

    [Header("SAND MOUNDS EFFECTS")] [SerializeField]
    private float moundEmergenceSpeed = 1f;

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

    // Componentes
    private AudioSource _movingAudio;
    private Rigidbody _rb;

    // ⏸️ Estados
    private bool _paused;     // Pausa de Menú (Hard Pause)
    private bool _isLocked;   // Pausa de Cinemática/Interacción (Soft Pause)
    private bool _holdAtWaypoint;

    private void Start()
    {
        platformMaterials = GetMaterialsFromChildren();
        
        _rb = GetComponent<Rigidbody>();
        _rb.isKinematic = true;
        _rb.interpolation = RigidbodyInterpolation.Interpolate;

        if (waypoints.Length > 0 && isMoving)
        {
            if (Vector3.Distance(_rb.position, waypoints[0].position) == 0)
                isMovingToFirstWaypoint = true;
            else
                _rb.position = waypoints[0].position;
        }

        if (sandMoundForward && sandMoundBackward)
        {
            sandMoundForward.position = sandMoundForwardWaypoints[0].position;
            sandMoundBackward.position = sandMoundBackwardWaypoints[0].position;
        }
    }

    private void FixedUpdate()
    {
        // 🔹 AHORA SE DETIENE SI ESTÁ PAUSADO O SI ESTÁ BLOQUEADO (LOCKED)
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

    // ... [Resto de lógica de movimiento igual (MoveToFirstWaypoint, MoveTowardsWaypoint)] ...
    
    // Omito el cuerpo de estos métodos para ahorrar espacio, ya que no cambian
    // MoveToFirstWaypoint() ...
    // MoveTowardsWaypoint() ...
    // EnsureMovingAudio() ...
    // HandleMovingSound() ...

    private void MoveToFirstWaypoint()
    {
        Transform firstWaypoint = waypoints[0];
        float step = speed * Time.deltaTime; 

        Vector3 newPos = Vector3.MoveTowards(_rb.position, firstWaypoint.position, step);
        _rb.MovePosition(newPos);

        if (Vector3.Distance(_rb.position, firstWaypoint.position) <= 0.001f)
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

        Vector3 newPos = Vector3.MoveTowards(_rb.position, targetWaypoint.position, step);
        _rb.MovePosition(newPos);

        if (Vector3.Distance(_rb.position, targetWaypoint.position) <= 0.01f)
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
        
        // 🔹 Esperamos considerando ambas pausas
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
        _currentWaypointIndex = _currentWaypointIndex > 0 ? _currentWaypointIndex - 1 : _currentWaypointIndex + 1;
    }

    // ... [Métodos de SandMound y GlowEffect iguales] ...
    
    // Omito SandMounds Logic y Visuals para brevedad, no cambian...
    // Solo recuerda que en FixedUpdate ya los controlas con "shouldMove"

    // ---------------- SAND MOUND LOGIC (Visuals) ----------------
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
                if (!ps.isPlaying && !_paused && !_isLocked) ps.Play(); // 🔹 Check Locked
            foreach (var ps in sandMoundBackwardParticles) ps.Stop();
        }
        else if (_currentWaypointIndex == 0)
        {
            foreach (var ps in sandMoundBackwardParticles)
                if (!ps.isPlaying && !_paused && !_isLocked) ps.Play(); // 🔹 Check Locked
            foreach (var ps in sandMoundForwardParticles) ps.Stop();
        }
    }
    
    // ... GlowEffect methods ...
    private Material[] GetMaterialsFromChildren() =>
        GetComponentsInChildren<Renderer>().SelectMany(r => r.materials).ToArray();

    private IEnumerator GlowEffect(float duration)
    {
        StartCoroutine(IncreaseIntensity(duration / 2f));
        if (platformBank != null) platformBank.Play3D(activeSoundKey, transform.position);
        yield return WaitForSecondsPausable(duration / 2f, () => _paused || _isLocked); // 🔹 Check Locked
        StartCoroutine(DecreaseIntensity(duration / 2f));
    }

    private IEnumerator IncreaseIntensity(float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            if (_paused || _isLocked) { yield return null; continue; } // 🔹 Espera simple
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
             if (_paused || _isLocked) { yield return null; continue; } // 🔹 Espera simple
            elapsed += Time.deltaTime;
            float current = Mathf.Lerp(glowIntensity, 0f, elapsed / duration);
            foreach (var mat in platformMaterials)
                if (mat.HasProperty("_GlowIntensity")) mat.SetFloat("_GlowIntensity", current);
            yield return null;
        }
    }

    // ---------------- EVENT HANDLERS ----------------

    // Evento de Pausa (Menu)
    public void OnPauseChanged(bool paused)
    {
        _paused = paused;
        UpdateState();
    }

    // 🔹 NUEVO: Evento de Lock (Cinemática/Interacción)
    public void OnLockChanged(bool locked)
    {
        _isLocked = locked;
        UpdateState();
    }

    // Lógica centralizada para detener/reanudar efectos
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
        if (systems == null || systems.Length == 0) return;
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
        // 🔹 REGISTRAMOS EL LOCKED
        GameEventManager.Instance.playerEvents.OnLocked.Register<bool>(OnLockChanged);
    }

    private void OnDisable()
    {
        if (GameEventManager.Instance == null) return;
        
        GameEventManager.Instance.levelEvents.OnPauseChanged.Unregister<bool>(OnPauseChanged);
        // 🔹 DES-REGISTRAMOS EL LOCKED
        GameEventManager.Instance.playerEvents.OnLocked.Unregister<bool>(OnLockChanged);
    }
}