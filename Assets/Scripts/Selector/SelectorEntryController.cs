using System;
using UnityEngine;
using UnityEngine.Playables;

public class SelectorEntryController : MonoBehaviour
{
    [Header("Timeline")]
    [SerializeField] private PlayableDirector director;

    [Header("Rig")]
    [Tooltip("Root reposicionado sobre el EntryPoint del nivel actual.")]
    [SerializeField] private Transform entryRig;

    [Header("Actor")]
    [SerializeField] private SelectorMovement movement;

    [Header("Camera")]
    [SerializeField]
    private SelectorEntryCamera entryCamera;
    
    [Tooltip("Duración del avance del personaje durante la entrada.")]
    [SerializeField] private float actorEntryMoveDuration = 1.2f;
    
    [Header("VFX")]
    [SerializeField] private SelectorEntryVfx entryVfx;
    private Action _onComplete;
    private LevelTile _activeLevel;
    private bool _isPlaying;

    public bool IsPlaying => _isPlaying;

    private void Awake()
    {
        if (director != null)
            director.playOnAwake = false;
    }

    // ============================================================
    // PLAY
    // ============================================================

    public void Play(
        LevelTile level,
        Action onComplete)
    {
        if (_isPlaying)
            return;

        if (level == null)
        {
            onComplete?.Invoke();
            return;
        }

        _activeLevel = level;

        PrepareRig(level.EntryPoint);
        
        if (entryVfx != null)
            entryVfx.Prepare();
        
        if (entryCamera != null)
            entryCamera.Activate();
        
        if (director == null ||
            director.playableAsset == null)
        {
            CompleteWithoutTimeline();
            onComplete?.Invoke();
            return;
        }

        _isPlaying = true;
        _onComplete = onComplete;

        director.stopped +=
            HandleTimelineStopped;

        director.time = 0d;
        director.Evaluate();
        director.Play();
    }

    private void PrepareRig(
        Transform entryPoint)
    {
        if (entryRig == null ||
            entryPoint == null)
        {
            return;
        }

        entryRig.SetPositionAndRotation(
            entryPoint.position,
            entryPoint.rotation
        );
    }
    
    public void PlayEntryVfx()
    {
        if (!_isPlaying)
            return;

        if (entryVfx != null)
            entryVfx.Play();
    }

    // ============================================================
    // TIMELINE SIGNAL API
    // ============================================================

    /// <summary>
    /// Invocado mediante Signal de Timeline.
    /// Hace caminar al actor hacia el nivel seleccionado.
    /// </summary>
    public void BeginActorEntry()
    {
        if (!_isPlaying ||
            _activeLevel == null ||
            movement == null)
        {
            return;
        }

        Transform target =
            _activeLevel.ActorEntryTarget;

        if (target == null)
            return;

        movement.MoveTo(
            target,
            actorEntryMoveDuration
        );
    }

    // ============================================================
    // COMPLETE
    // ============================================================

    private void HandleTimelineStopped(
        PlayableDirector stoppedDirector)
    {
        if (director != null)
        {
            director.stopped -=
                HandleTimelineStopped;
        }

        _isPlaying = false;
        _activeLevel = null;

        Action callback =
            _onComplete;

        _onComplete = null;

        callback?.Invoke();
    }

    private void CompleteWithoutTimeline()
    {
        _activeLevel = null;
        _isPlaying = false;
    }

    // ============================================================

    public void Cancel()
    {
        if (!_isPlaying)
            return;

        if (director != null)
        {
            director.stopped -=
                HandleTimelineStopped;

            director.Stop();
        }

        if (movement != null)
            movement.StopCurrentMovement();
        
        if (entryCamera != null)
            entryCamera.Deactivate();
        
        if (entryVfx != null)
            entryVfx.Stop();

        _activeLevel = null;
        _isPlaying = false;
        _onComplete = null;
    }

    private void OnDisable()
    {
        if (director != null)
        {
            director.stopped -=
                HandleTimelineStopped;
        }
        
        if (entryCamera != null)
            entryCamera.Deactivate();

        _activeLevel = null;
        _isPlaying = false;
        _onComplete = null;
    }
}