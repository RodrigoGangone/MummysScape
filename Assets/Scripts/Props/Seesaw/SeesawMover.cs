using UnityEngine;

/// <summary>
/// Rota de forma cinemática y controlada la tabla alrededor de su pivote mediante Rigidbody.MoveRotation.
/// Respeta aceleración, límites angulares, pausa y bloqueos independientes del suelo en cada punta.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
public sealed class SeesawMover : MonoBehaviour, IPausable
{
    private const float AngleEpsilon = 0.01f;
    private const int PhysicsQueryBufferSize = 16;

    [Header("References")]
    [SerializeField] private Rigidbody _rigidbody;
    [SerializeField] private Transform _leftGroundCheck;
    [SerializeField] private Transform _rightGroundCheck;

    [Header("Neutral Rotation")]
    [Tooltip("Usa la rotación local inicial del pivote como posición neutral al comenzar.")]
    [SerializeField] private bool _useInitialLocalRotationAsNeutral = true;

    [Tooltip("Rotación local neutral utilizada cuando Use Initial Local Rotation As Neutral está desactivado.")]
    [SerializeField] private Vector3 _neutralLocalEulerAngles;

    [Tooltip("Eje local de rotación. Debe orientarse de forma que el ángulo positivo haga bajar el lado izquierdo.")]
    [SerializeField] private Vector3 _localRotationAxis = Vector3.forward;

    [Header("Angle Limits")]
    [SerializeField, Min(0f)] private float _maximumLeftAngle = 15f;
    [SerializeField, Min(0f)] private float _maximumRightAngle = 15f;

    [Header("Angular Motion")]
    [SerializeField, Min(0f)] private float _rotationAcceleration = 80f;
    [SerializeField, Min(0f)] private float _rotationDeceleration = 120f;
    [SerializeField, Min(0f)] private float _lowAngularSpeed = 15f;
    [SerializeField, Min(0f)] private float _mediumAngularSpeed = 25f;
    [SerializeField, Min(0f)] private float _highAngularSpeed = 40f;
    [SerializeField, Min(0f)] private float _returnToBalanceSpeed = 15f;

    [Header("Ground Checks")]
    [SerializeField, Min(0f)] private float _groundCheckDistance = 0.2f;
    [SerializeField, Min(0.001f)] private float _groundCheckRadius = 0.08f;
    [SerializeField] private LayerMask _groundLayers;
    [SerializeField, Min(0f)] private float _groundSafetyDistance = 0.02f;
    [SerializeField] private bool _showGroundCheckGizmos = true;

    [Header("Runtime Debug")]
    [SerializeField] private SeesawState _currentState = SeesawState.Balanced;
    [SerializeField] private SeesawSpeedLevel _currentSpeedLevel = SeesawSpeedLevel.None;
    [SerializeField] private bool _isLeftGroundBlocked;
    [SerializeField] private bool _isRightGroundBlocked;
    [SerializeField] private float _currentAngle;
    [SerializeField] private float _targetAngle;
    [SerializeField] private float _currentAngularSpeed;
    [SerializeField] private bool _isPaused;

    private readonly RaycastHit[] _sphereCastHits = new RaycastHit[PhysicsQueryBufferSize];
    private readonly Collider[] _overlapHits = new Collider[PhysicsQueryBufferSize];

    private Collider[] _selfColliders = System.Array.Empty<Collider>();
    private Quaternion _neutralLocalRotation;
    private SeesawResolution _resolution;

    public bool IsLeftGroundBlocked => _isLeftGroundBlocked;
    public bool IsRightGroundBlocked => _isRightGroundBlocked;
    public float CurrentAngle => _currentAngle;
    public float TargetAngle => _targetAngle;
    public float CurrentAngularSpeed => _currentAngularSpeed;
    public Quaternion NeutralLocalRotation => _neutralLocalRotation;

    private void Awake()
    {
        if (_rigidbody == null)
        {
            _rigidbody = GetComponent<Rigidbody>();
        }

        _neutralLocalRotation = _useInitialLocalRotationAsNeutral
            ? transform.localRotation
            : Quaternion.Euler(_neutralLocalEulerAngles);

        if (_useInitialLocalRotationAsNeutral)
        {
            _neutralLocalEulerAngles = transform.localEulerAngles;
        }

        _selfColliders = GetComponentsInChildren<Collider>(includeInactive: true);
        ConfigureRigidbody();
        ApplyTargetFromResolution();
        UpdateRuntimeDebug();
    }

    private void OnEnable()
    {
        if (GameEventManager.Instance != null)
        {
            GameEventManager.Instance.levelEvents.OnPauseChanged.Register<bool>(OnPauseChanged);
        }
    }

    private void OnDisable()
    {
        if (GameEventManager.Instance != null)
        {
            GameEventManager.Instance.levelEvents.OnPauseChanged.Unregister<bool>(OnPauseChanged);
        }

        _currentAngularSpeed = 0f;
    }

    private void FixedUpdate()
    {
        _currentAngle = ReadCurrentAngle();
        UpdateGroundBlocks();

        if (_isPaused)
        {
            _currentAngularSpeed = 0f;
            UpdateRuntimeDebug();
            return;
        }

        float angleDelta = _targetAngle - _currentAngle;
        float desiredAngularSpeed = CalculateDesiredAngularSpeed(angleDelta);
        float rate = ShouldAccelerate(_currentAngularSpeed, desiredAngularSpeed)
            ? _rotationAcceleration
            : _rotationDeceleration;

        _currentAngularSpeed = Mathf.MoveTowards(
            _currentAngularSpeed,
            desiredAngularSpeed,
            rate * Time.fixedDeltaTime);

        StopBlockedDirection();

        float nextAngle = _currentAngle + (_currentAngularSpeed * Time.fixedDeltaTime);
        nextAngle = PreventTargetOvershoot(_currentAngle, nextAngle, angleDelta);
        nextAngle = Mathf.Clamp(nextAngle, -_maximumRightAngle, _maximumLeftAngle);

        if ((Mathf.Approximately(nextAngle, _maximumLeftAngle) && _currentAngularSpeed > 0f) ||
            (Mathf.Approximately(nextAngle, -_maximumRightAngle) && _currentAngularSpeed < 0f))
        {
            _currentAngularSpeed = 0f;
        }

        Quaternion nextWorldRotation = CalculateWorldRotation(nextAngle);
        _rigidbody.MoveRotation(nextWorldRotation);

        _currentAngle = nextAngle;
        UpdateRuntimeDebug();
    }

    public void SetResolution(SeesawResolution resolution)
    {
        _resolution = resolution;
        _currentState = resolution.State;
        _currentSpeedLevel = resolution.SpeedLevel;
        ApplyTargetFromResolution();
    }

    public void OnPauseChanged(bool paused)
    {
        _isPaused = paused;
        _currentAngularSpeed = 0f;
    }

    private void ApplyTargetFromResolution()
    {
        _targetAngle = _resolution.State switch
        {
            SeesawState.LeftHeavy => _maximumLeftAngle,
            SeesawState.RightHeavy => -_maximumRightAngle,
            _ => 0f
        };
    }

    private float CalculateDesiredAngularSpeed(float angleDelta)
    {
        if (Mathf.Abs(angleDelta) <= AngleEpsilon)
        {
            return 0f;
        }

        float speedMagnitude = _resolution.State == SeesawState.Balanced
            ? _returnToBalanceSpeed
            : GetConfiguredSpeed(_resolution.SpeedLevel);

        float desiredSpeed = Mathf.Sign(angleDelta) * speedMagnitude;

        if ((desiredSpeed > 0f && _isLeftGroundBlocked) ||
            (desiredSpeed < 0f && _isRightGroundBlocked))
        {
            return 0f;
        }

        return desiredSpeed;
    }

    private float GetConfiguredSpeed(SeesawSpeedLevel speedLevel)
    {
        return speedLevel switch
        {
            SeesawSpeedLevel.Low => _lowAngularSpeed,
            SeesawSpeedLevel.Medium => _mediumAngularSpeed,
            SeesawSpeedLevel.High => _highAngularSpeed,
            _ => 0f
        };
    }

    private static bool ShouldAccelerate(float currentSpeed, float desiredSpeed)
    {
        if (Mathf.Approximately(desiredSpeed, 0f))
        {
            return false;
        }

        if (Mathf.Approximately(currentSpeed, 0f))
        {
            return true;
        }

        bool sameDirection = Mathf.Sign(currentSpeed) == Mathf.Sign(desiredSpeed);
        return sameDirection && Mathf.Abs(desiredSpeed) > Mathf.Abs(currentSpeed);
    }

    private void StopBlockedDirection()
    {
        if ((_currentAngularSpeed > 0f && _isLeftGroundBlocked) ||
            (_currentAngularSpeed < 0f && _isRightGroundBlocked))
        {
            _currentAngularSpeed = 0f;
        }
    }

    private float PreventTargetOvershoot(float currentAngle, float nextAngle, float angleDelta)
    {
        if (Mathf.Approximately(_currentAngularSpeed, 0f) ||
            Mathf.Sign(_currentAngularSpeed) != Mathf.Sign(angleDelta))
        {
            return nextAngle;
        }

        bool passedTarget = _currentAngularSpeed > 0f
            ? nextAngle >= _targetAngle
            : nextAngle <= _targetAngle;

        if (!passedTarget)
        {
            return nextAngle;
        }

        _currentAngularSpeed = 0f;
        return _targetAngle;
    }

    private void UpdateGroundBlocks()
    {
        _isLeftGroundBlocked = IsGroundBlocked(_leftGroundCheck);
        _isRightGroundBlocked = IsGroundBlocked(_rightGroundCheck);
    }

    private bool IsGroundBlocked(Transform groundCheck)
    {
        if (groundCheck == null || _groundLayers.value == 0)
        {
            return false;
        }

        Vector3 origin = groundCheck.position;

        int overlapCount = Physics.OverlapSphereNonAlloc(
            origin,
            _groundCheckRadius,
            _overlapHits,
            _groundLayers,
            QueryTriggerInteraction.Ignore);

        for (int i = 0; i < overlapCount; i++)
        {
            Collider collider = _overlapHits[i];
            if (collider != null && !IsSelfCollider(collider))
            {
                return true;
            }
        }

        float castDistance = _groundCheckDistance + _groundSafetyDistance;
        int hitCount = Physics.SphereCastNonAlloc(
            origin,
            _groundCheckRadius,
            Vector3.down,
            _sphereCastHits,
            castDistance,
            _groundLayers,
            QueryTriggerInteraction.Ignore);

        for (int i = 0; i < hitCount; i++)
        {
            Collider collider = _sphereCastHits[i].collider;
            if (collider != null && !IsSelfCollider(collider))
            {
                return true;
            }
        }

        return false;
    }

    private bool IsSelfCollider(Collider candidate)
    {
        for (int i = 0; i < _selfColliders.Length; i++)
        {
            if (_selfColliders[i] == candidate)
            {
                return true;
            }
        }

        return false;
    }

    private float ReadCurrentAngle()
    {
        Quaternion parentRotation = transform.parent != null
            ? transform.parent.rotation
            : Quaternion.identity;

        Quaternion currentLocalRotation = Quaternion.Inverse(parentRotation) * _rigidbody.rotation;
        Quaternion delta = Quaternion.Inverse(_neutralLocalRotation) * currentLocalRotation;
        return GetSignedAngleAroundAxis(delta, _localRotationAxis.normalized);
    }

    private Quaternion CalculateWorldRotation(float angle)
    {
        Quaternion targetLocalRotation =
            _neutralLocalRotation * Quaternion.AngleAxis(angle, _localRotationAxis.normalized);

        return transform.parent != null
            ? transform.parent.rotation * targetLocalRotation
            : targetLocalRotation;
    }

    private static float GetSignedAngleAroundAxis(Quaternion rotation, Vector3 axis)
    {
        Vector3 reference = Mathf.Abs(Vector3.Dot(axis, Vector3.up)) < 0.99f
            ? Vector3.up
            : Vector3.right;

        reference = Vector3.ProjectOnPlane(reference, axis).normalized;
        Vector3 rotatedReference = rotation * reference;
        return Vector3.SignedAngle(reference, rotatedReference, axis);
    }

    private void ConfigureRigidbody()
    {
        _rigidbody.isKinematic = true;
        _rigidbody.useGravity = false;
        _rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
        _rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        _rigidbody.constraints = RigidbodyConstraints.FreezePosition;
    }

    private void UpdateRuntimeDebug()
    {
        _currentState = _resolution.State;
        _currentSpeedLevel = _resolution.SpeedLevel;
    }

    private void OnValidate()
    {
        _maximumLeftAngle = Mathf.Max(0f, _maximumLeftAngle);
        _maximumRightAngle = Mathf.Max(0f, _maximumRightAngle);
        _rotationAcceleration = Mathf.Max(0f, _rotationAcceleration);
        _rotationDeceleration = Mathf.Max(0f, _rotationDeceleration);
        _lowAngularSpeed = Mathf.Max(0f, _lowAngularSpeed);
        _mediumAngularSpeed = Mathf.Max(_lowAngularSpeed, _mediumAngularSpeed);
        _highAngularSpeed = Mathf.Max(_mediumAngularSpeed, _highAngularSpeed);
        _returnToBalanceSpeed = Mathf.Max(0f, _returnToBalanceSpeed);
        _groundCheckDistance = Mathf.Max(0f, _groundCheckDistance);
        _groundCheckRadius = Mathf.Max(0.001f, _groundCheckRadius);
        _groundSafetyDistance = Mathf.Max(0f, _groundSafetyDistance);

        if (_localRotationAxis.sqrMagnitude <= Mathf.Epsilon)
        {
            _localRotationAxis = Vector3.forward;
        }

        if (_rigidbody == null)
        {
            _rigidbody = GetComponent<Rigidbody>();
        }
    }

    [ContextMenu("Capture Current Local Rotation As Neutral")]
    private void CaptureCurrentLocalRotationAsNeutral()
    {
        _neutralLocalEulerAngles = transform.localEulerAngles;
        _useInitialLocalRotationAsNeutral = false;
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 normalizedAxis = _localRotationAxis.sqrMagnitude > Mathf.Epsilon
            ? _localRotationAxis.normalized
            : Vector3.forward;

        Quaternion neutralLocalRotation = _useInitialLocalRotationAsNeutral
            ? transform.localRotation
            : Quaternion.Euler(_neutralLocalEulerAngles);

        Quaternion parentRotation = transform.parent != null
            ? transform.parent.rotation
            : Quaternion.identity;

        Vector3 pivotPosition = transform.position;
        Vector3 worldAxis = parentRotation * (neutralLocalRotation * normalizedAxis);

        Gizmos.DrawWireSphere(pivotPosition, 0.08f);
        Gizmos.DrawLine(pivotPosition - (worldAxis * 0.5f), pivotPosition + (worldAxis * 0.5f));

        DrawRotationLimitLine(
            pivotPosition,
            parentRotation,
            neutralLocalRotation,
            0f);

        DrawRotationLimitLine(
            pivotPosition,
            parentRotation,
            neutralLocalRotation,
            _maximumLeftAngle);

        DrawRotationLimitLine(
            pivotPosition,
            parentRotation,
            neutralLocalRotation,
            -_maximumRightAngle);

        if (_showGroundCheckGizmos)
        {
            DrawGroundCheckGizmo(_leftGroundCheck);
            DrawGroundCheckGizmo(_rightGroundCheck);
        }
    }

    private void DrawRotationLimitLine(
        Vector3 pivotPosition,
        Quaternion parentRotation,
        Quaternion neutralLocalRotation,
        float angle)
    {
        if (_leftGroundCheck == null || _rightGroundCheck == null)
        {
            return;
        }

        Quaternion worldRotation = parentRotation *
                                   (neutralLocalRotation *
                                    Quaternion.AngleAxis(angle, _localRotationAxis.normalized));

        Vector3 leftLocalPosition = transform.InverseTransformPoint(_leftGroundCheck.position);
        Vector3 rightLocalPosition = transform.InverseTransformPoint(_rightGroundCheck.position);

        Vector3 leftWorldPosition = pivotPosition + (worldRotation * leftLocalPosition);
        Vector3 rightWorldPosition = pivotPosition + (worldRotation * rightLocalPosition);

        Gizmos.DrawLine(leftWorldPosition, rightWorldPosition);
        Gizmos.DrawWireSphere(leftWorldPosition, 0.04f);
        Gizmos.DrawWireSphere(rightWorldPosition, 0.04f);
    }

    private void DrawGroundCheckGizmo(Transform groundCheck)
    {
        if (groundCheck == null)
        {
            return;
        }

        Vector3 origin = groundCheck.position;
        float castDistance = _groundCheckDistance + _groundSafetyDistance;
        Vector3 end = origin + (Vector3.down * castDistance);

        Gizmos.DrawWireSphere(origin, _groundCheckRadius);
        Gizmos.DrawLine(origin, end);
        Gizmos.DrawWireSphere(end, _groundCheckRadius);
    }
}