using System.Collections;
using System.Linq;
using UnityEngine;
using static Utils;

public class MoveVerticalPlatform : Pausable
{
    [Header("PLAY ON AWAKE")] 
    [SerializeField] private bool isMoving;

    [Header("SPEED")] 
    [SerializeField] private float speed = 1;
    [SerializeField] private float stopTime = 0.5f;

    [Header("WAYPOINTS")] 
    [SerializeField] private Transform[] waypoints;

    [Header("EFFECTS")] 
    [SerializeField] private ParticleSystem sandMoundsParticle;
    [SerializeField] private ParticleSystem activationParticles;

    [SerializeField] private float glowDuration = 2f;
    [SerializeField] private float glowIntensity = 0.15f;

    private Material[] platformMaterials;

    // Pausa interna (local)
    private bool _localPaused;
    private bool _isMovingToFirstWaypoint;
    private int _currentWaypointIndex = 0;
    private AudioSource _platformAudio;

    private void Start()
    {
        platformMaterials = GetMaterialsFromChildren();

        if (waypoints.Length > 0 && isMoving)
        {
            if (Vector3.Distance(transform.position, waypoints[0].position) == 0)
                _isMovingToFirstWaypoint = true;
            else
                transform.position = waypoints[0].position;
        }

        _platformAudio = AudioManager.Instance.GetClipByName(NameSounds.SFX_MovingPlatform);
    }

    private void Update()
    {
        if (Paused) return; // 🔸 pausa global

        if (_isMovingToFirstWaypoint)
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
            _isMovingToFirstWaypoint = false;
            _currentWaypointIndex = 1;
        }
    }

    private void MoveTowardsWaypoint()
    {
        if (_localPaused)
        {
            if (sandMoundsParticle && sandMoundsParticle.isPlaying)
                sandMoundsParticle.Stop();
            return;
        }

        if (sandMoundsParticle && !sandMoundsParticle.isPlaying)
            sandMoundsParticle.Play();

        Transform target = waypoints[_currentWaypointIndex];
        float step = speed * Time.deltaTime;
        transform.position = Vector3.MoveTowards(transform.position, target.position, step);

        if (Vector3.Distance(transform.position, target.position) <= 0.001f)
            StartCoroutine(PauseAtWaypoint());
    }

    private IEnumerator PauseAtWaypoint()
    {
        _localPaused = true;

        yield return WaitForSecondsPausable(stopTime); // ⏸️ pausa global respetada

        _currentWaypointIndex = (_currentWaypointIndex + 1) % waypoints.Length;
        _localPaused = false;
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

    private Material[] GetMaterialsFromChildren() =>
        GetComponentsInChildren<Renderer>().SelectMany(r => r.materials).ToArray();

    private IEnumerator GlowEffect(float duration)
    {
        StartCoroutine(IncreaseIntensity(duration / 2));
        yield return WaitForSecondsPausable(duration / 2);
        StartCoroutine(DecreaseIntensity(duration / 2));
    }

    private IEnumerator IncreaseIntensity(float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            if (Paused) { yield return WaitWhilePaused(); continue; }

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
            if (Paused) { yield return WaitWhilePaused(); continue; }

            elapsed += Time.deltaTime;
            float current = Mathf.Lerp(glowIntensity, 0f, elapsed / duration);

            foreach (var mat in platformMaterials)
                if (mat.HasProperty("_GlowIntensity")) mat.SetFloat("_GlowIntensity", current);

            yield return null;
        }
    }

    private void HandleAudio()
    {
        if (isMoving && !_localPaused && !Paused)
        {
            StartCoroutine(AudioManager.Instance.FollowTransform(_platformAudio, transform, -1));
            if (!_platformAudio.isPlaying)
                _platformAudio.Play();
        }
        else
        {
            if (_platformAudio.isPlaying)
                _platformAudio.Pause();
        }
    }

    // 🔸 Integración con el sistema global de pausa
    public override void OnPauseChanged(bool paused)
    {
        if (sandMoundsParticle)
        {
            if (paused && sandMoundsParticle.isPlaying)
                sandMoundsParticle.Pause();
            else if (!paused && !sandMoundsParticle.isPlaying && isMoving)
                sandMoundsParticle.Play();
        }

        if (paused && _platformAudio.isPlaying)
            _platformAudio.Pause();
        else if (!paused && !_platformAudio.isPlaying && isMoving)
            _platformAudio.Play();
    }
}
