using Cinemachine;
using UnityEngine;

/// <summary>Layers a short gem-impact recoil over the hourglass's existing animations.</summary>
[DisallowMultipleComponent]
public sealed class UIGemArrivalImpact : MonoBehaviour
{
    [Header("Hourglass")]
    [Tooltip("Dedicated pivot above Models, excluding the render camera and heartbeat root.")]
    [SerializeField] private Transform _hourglassTarget;
    [SerializeField] private Camera _hourglassCamera;
    [SerializeField, Min(0.01f)] private float _duration = 0.46f;
    [SerializeField, Min(0f)] private float _recoilDistance = 0.045f;
    [SerializeField, Min(0f)] private float _rollDegrees = 3.2f;
    [SerializeField, Range(0f, 0.3f)] private float _squash = 0.075f;
    [SerializeField] private AnimationCurve _impactCurve = new AnimationCurve(
        new Keyframe(0f, 0f),
        new Keyframe(0.1f, 1f),
        new Keyframe(0.34f, -0.38f),
        new Keyframe(0.62f, 0.14f),
        new Keyframe(1f, 0f));

    [Header("Gameplay Camera")]
    [SerializeField] private CinemachineImpulseSource _impulseSource;
    [SerializeField, Min(0f)] private float _shakeForce = 0.08f;
    [SerializeField, Min(0f)] private float _shakeCooldown = 0.2f;

    private Transform _cachedTarget;
    private Vector3 _basePosition;
    private Quaternion _baseRotation;
    private Vector3 _baseScale;
    private Vector3 _recoilDirection = Vector3.down;
    private Vector3 _rollAxis = Vector3.forward;
    private float _elapsed;
    private float _direction;
    private float _recoil;
    private float _roll;
    private float _startRecoil;
    private float _startRoll;
    private bool _playing;
    private bool _paused;
    private float _lastShakeTime = float.NegativeInfinity;

    private void Awake() => CacheTarget();

    public void Play(int gemNumber, Transform gemTarget)
    {
        if (!isActiveAndEnabled || gemNumber < 1 || gemNumber > 3)
            return;

        if (_cachedTarget != _hourglassTarget)
        {
            RestorePose();
            CacheTarget();
        }

        if (_hourglassTarget != null)
        {
            CacheViewAxes();

            _startRecoil = _recoil;
            _startRoll = _roll;

            _direction = gemNumber == 1
                ? -1f
                : gemNumber == 3
                    ? 1f
                    : 0.35f;

            _elapsed = 0f;
            _playing = true;
        }

        PlayCameraShake(gemTarget);
    }

    public void SetPaused(bool paused) => _paused = paused;

    private void LateUpdate()
    {
        if (!_playing || _paused || _cachedTarget == null || Time.deltaTime <= 0f)
            return;

        _elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(_elapsed / Mathf.Max(0.01f, _duration));
        if (t >= 1f)
        {
            RestorePose();
            return;
        }

        float impact = _impactCurve != null ? _impactCurve.Evaluate(t) : 0f;
        float carry = 1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / 0.25f));

        // Retriggers carry the current pose into the next hit, while keeping its amplitude bounded.
        _recoil = Mathf.Clamp(_startRecoil * carry + impact, -0.5f, 1.25f);
        _roll = Mathf.Clamp(_startRoll * carry + impact * _direction, -1.25f, 1.25f);

        _cachedTarget.localPosition = _basePosition + _recoilDirection * (_recoilDistance * _recoil);
        _cachedTarget.localRotation = Quaternion.AngleAxis(_rollDegrees * _roll, _rollAxis) * _baseRotation;
        float squash = _squash * _recoil;
        _cachedTarget.localScale = Vector3.Scale(_baseScale,
            new Vector3(1f + squash * 0.5f, 1f - squash, 1f + squash * 0.5f));
    }

    private void CacheTarget()
    {
        _cachedTarget = _hourglassTarget;
        if (_cachedTarget == null)
            return;

        _basePosition = _cachedTarget.localPosition;
        _baseRotation = _cachedTarget.localRotation;
        _baseScale = _cachedTarget.localScale;
    }

    private void PlayCameraShake(Transform gemTarget)
    {
        if (_paused ||
            _impulseSource == null ||
            _impulseSource.m_ImpulseDefinition == null ||
            _shakeForce <= 0f ||
            Time.time - _lastShakeTime < _shakeCooldown ||
            gemTarget == null)
            return;

        Camera gameplayCamera = Camera.main;

        if (gameplayCamera == null)
            return;

        int channel = _impulseSource.m_ImpulseDefinition.m_ImpulseChannel;

        EnsureImpulseListener(gameplayCamera, channel);

        _impulseSource.GenerateImpulseAtPositionWithVelocity(
            gemTarget.position,
            Vector3.down * _shakeForce);

        _lastShakeTime = Time.time;
    }

    private static void EnsureImpulseListener(Camera camera, int channel)
    {
        foreach (var listener in camera.GetComponents<CinemachineIndependentImpulseListener>())
            if (listener.enabled && (listener.m_ChannelMask & channel) != 0)
                return;

        CinemachineBrain brain = camera.GetComponent<CinemachineBrain>();
        if (brain != null)
        {
            EnsureVirtualCameraListener(brain.ActiveVirtualCamera, channel);
            if (brain.ActiveBlend != null)
            {
                EnsureVirtualCameraListener(brain.ActiveBlend.CamA, channel);
                EnsureVirtualCameraListener(brain.ActiveBlend.CamB, channel);
            }
            return;
        }

        var independent = camera.gameObject.AddComponent<CinemachineIndependentImpulseListener>();
        independent.m_ChannelMask = channel;
        independent.m_Gain = 1f;
        independent.m_UseLocalSpace = true;
    }

    private static void EnsureVirtualCameraListener(ICinemachineCamera camera, int channel)
    {
        if (camera == null || camera.VirtualCameraGameObject == null)
            return;

        // Parent rigs can already process their child camera's impulse.
        for (ICinemachineCamera current = camera; current != null; current = current.ParentCamera)
        {
            GameObject cameraObject = current.VirtualCameraGameObject;
            if (cameraObject == null)
                continue;

            foreach (var listener in cameraObject.GetComponents<CinemachineImpulseListener>())
                if (listener.enabled && (listener.m_ChannelMask & channel) != 0)
                    return;
        }

        var added = camera.VirtualCameraGameObject.AddComponent<CinemachineImpulseListener>();
        added.m_ChannelMask = channel;
        added.m_Gain = 1f;
        added.m_UseCameraSpace = true;
        added.m_ApplyAfter = CinemachineCore.Stage.Noise;
    }

    private void CacheViewAxes()
    {
        if (_hourglassCamera == null)
            return;

        Transform parent = _cachedTarget.parent;
        Vector3 down = -_hourglassCamera.transform.up;
        Vector3 forward = _hourglassCamera.transform.forward;
        _recoilDirection = parent != null ? parent.InverseTransformDirection(down).normalized : down;
        _rollAxis = parent != null ? parent.InverseTransformDirection(forward).normalized : forward;
    }

    private void RestorePose()
    {
        if (_cachedTarget != null)
        {
            _cachedTarget.localPosition = _basePosition;
            _cachedTarget.localRotation = _baseRotation;
            _cachedTarget.localScale = _baseScale;
        }

        _playing = false;
        _recoil = _roll = _startRecoil = _startRoll = 0f;
    }

    private void OnDisable()
    {
        RestorePose();
        _paused = false;
    }
}
