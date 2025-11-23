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

    [Header("EFFECTS")] 
    [SerializeField] private float moundEmergenceSpeed = 1f;
    [SerializeField] private Transform sandMoundForward;
    [SerializeField] private Transform[] sandMoundForwardWaypoints;
    [SerializeField] private Transform sandMoundBackward;
    [SerializeField] private Transform[] sandMoundBackwardWaypoints;

    [SerializeField] private ParticleSystem[] sandMoundForwardParticles;
    [SerializeField] private ParticleSystem[] sandMoundBackwardParticles;
    [SerializeField] private ParticleSystem activationParticles;

    [SerializeField] private float glowDuration = 2f;
    [SerializeField] private float glowIntensity = 0.15f;

    private Material[] platformMaterials;
    private bool isMovingToFirstWaypoint;
    private int _currentWaypointIndex = 0;

    private AudioSource platformAudio;

    // ⏸️ pausa global
    private bool _paused;
    // ⏱️ pausa local (espera entre waypoints)
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

       // platformAudio = AudioManager.Instance.GetClipByName(NameSounds.SFX_MovingPlatform);
    }

    private void Update()
    {
        if (_paused) return; // pausa global

        if (isMovingToFirstWaypoint)
            MoveToFirstWaypoint();
        else if (isMoving && waypoints.Length > 0)
            MoveTowardsWaypoint();

        HandleAudio();
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

    private IEnumerator PauseAtWaypoint()
    {
        _holdAtWaypoint = true;
        yield return WaitForSecondsPausable(stopTime, () => _paused); // respeta pausa global
        _currentWaypointIndex = (_currentWaypointIndex + 1) % waypoints.Length;
        _holdAtWaypoint = false;
    }

    public void StartAction()
    {
        isMoving = !isMoving;
        activationParticles?.Play();
        StartCoroutine(GlowEffect(glowDuration));
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
            foreach (var ps in sandMoundForwardParticles) if (!ps.isPlaying) ps.Play();
            foreach (var ps in sandMoundBackwardParticles) ps.Stop();
        }
        else if (_currentWaypointIndex == 0)
        {
            foreach (var ps in sandMoundBackwardParticles) if (!ps.isPlaying) ps.Play();
            foreach (var ps in sandMoundForwardParticles) ps.Stop();
        }
    }

    private Material[] GetMaterialsFromChildren() =>
        GetComponentsInChildren<Renderer>().SelectMany(r => r.materials).ToArray();

    private IEnumerator GlowEffect(float duration)
    {
        StartCoroutine(IncreaseIntensity(duration / 2f));
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

    private void HandleAudio()
    {
        if (isMoving && !_holdAtWaypoint && !_paused)
        {
            //StartCoroutine(AudioManager.Instance.FollowTransform(platformAudio, transform, -1));
            if (!platformAudio.isPlaying) platformAudio.Play();
        }
        else
        {
            if (platformAudio.isPlaying) platformAudio.Pause();
        }
    }

    // ===== IPausable =====
    public void OnPauseChanged(bool paused)
    {
        _paused = paused;

        foreach (var ps in sandMoundForwardParticles)
            if (paused && ps.isPlaying) ps.Pause();
            else if (!paused && !ps.isPlaying && isMoving) ps.Play();

        foreach (var ps in sandMoundBackwardParticles)
            if (paused && ps.isPlaying) ps.Pause();
            else if (!paused && !ps.isPlaying && isMoving) ps.Play();

        if (platformAudio != null)
        {
            if (paused && platformAudio.isPlaying) platformAudio.Pause();
            else if (!paused && !platformAudio.isPlaying && isMoving) platformAudio.Play();
        }
    }

    private void OnEnable() => GameEventManager.Instance.levelEvents.OnPauseChanged.Register<bool>(OnPauseChanged);
    private void OnDisable() => GameEventManager.Instance.levelEvents.OnPauseChanged.Unregister<bool>(OnPauseChanged);

}
