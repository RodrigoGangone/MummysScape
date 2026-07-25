using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Coordina los sensores izquierdo y derecho, elimina el doble conteo de proveedores compartidos
/// y comunica una única resolución de peso al resolver lógico y al controlador de movimiento.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(SeesawStateResolver))]
public sealed class SeesawCoordinator : MonoBehaviour
{
    private enum ProviderSide
    {
        None,
        Left,
        Right
    }

    private struct CombinedProviderData
    {
        public IWeightProvider Provider;
        public bool IsOnLeft;
        public bool IsOnRight;
        public int LeftColliderCount;
        public int RightColliderCount;
        public Vector3 LeftWorldCenter;
        public Vector3 RightWorldCenter;
        public int Weight;
    }

    [Header("References")]
    [SerializeField] private WeightSensor _leftWeightSensor;
    [SerializeField] private WeightSensor _rightWeightSensor;
    [SerializeField] private SeesawStateResolver _stateResolver;
    [SerializeField] private SeesawMover _mover;
    [SerializeField] private Transform _pivot;

    [Header("Shared Provider Resolution")]
    [Tooltip("Eje local del pivote. Su dirección positiva representa el lado derecho.")]
    [SerializeField] private Vector3 _leftRightLocalAxis = Vector3.right;

    [Tooltip("Zona central donde se conserva la asignación previa para impedir cambios rápidos de lado.")]
    [SerializeField, Min(0f)] private float _sharedProviderCenterDeadZone = 0.15f;

    [Header("Runtime Debug")]
    [SerializeField] private int _leftRawWeight;
    [SerializeField] private int _rightRawWeight;
    [SerializeField] private int _sharedProviderCount;
    [SerializeField] private bool _hasSharedProviders;

    private readonly List<WeightSensor.ProviderData> _leftProviders =
        new List<WeightSensor.ProviderData>(8);

    private readonly List<WeightSensor.ProviderData> _rightProviders =
        new List<WeightSensor.ProviderData>(8);

    private readonly Dictionary<MonoBehaviour, CombinedProviderData> _combinedProviders =
        new Dictionary<MonoBehaviour, CombinedProviderData>(16);

    private readonly Dictionary<MonoBehaviour, ProviderSide> _sharedAssignments =
        new Dictionary<MonoBehaviour, ProviderSide>(8);

    private readonly HashSet<MonoBehaviour> _activeSharedProviders =
        new HashSet<MonoBehaviour>();

    private readonly List<MonoBehaviour> _staleAssignments =
        new List<MonoBehaviour>(8);

    private bool _isResolving;
    private bool _resolveRequested;

    public int LeftRawWeight => _leftRawWeight;
    public int RightRawWeight => _rightRawWeight;
    public int SharedProviderCount => _sharedProviderCount;
    public bool HasSharedProviders => _hasSharedProviders;

    private void OnEnable()
    {
        SubscribeSensors();
        RequestResolve();
    }

    private void Start()
    {
        RequestResolve();
    }

    private void FixedUpdate()
    {
        if (_hasSharedProviders)
        {
            RequestResolve();
        }
    }

    private void OnDisable()
    {
        UnsubscribeSensors();
        _sharedAssignments.Clear();
        _activeSharedProviders.Clear();
    }

    private void SubscribeSensors()
    {
        if (_leftWeightSensor != null)
        {
            _leftWeightSensor.SensorChanged += RequestResolve;
        }

        if (_rightWeightSensor != null)
        {
            _rightWeightSensor.SensorChanged += RequestResolve;
        }
    }

    private void UnsubscribeSensors()
    {
        if (_leftWeightSensor != null)
        {
            _leftWeightSensor.SensorChanged -= RequestResolve;
        }

        if (_rightWeightSensor != null)
        {
            _rightWeightSensor.SensorChanged -= RequestResolve;
        }
    }

    private void RequestResolve()
    {
        if (_isResolving)
        {
            _resolveRequested = true;
            return;
        }

        do
        {
            _resolveRequested = false;
            ResolveWeights();
        }
        while (_resolveRequested);
    }

    private void ResolveWeights()
    {
        if (_leftWeightSensor == null ||
            _rightWeightSensor == null ||
            _stateResolver == null ||
            _mover == null ||
            _pivot == null)
        {
            return;
        }

        _isResolving = true;

        _leftWeightSensor.CopyProviderDataTo(_leftProviders);
        _rightWeightSensor.CopyProviderDataTo(_rightProviders);

        BuildCombinedProviderMap();

        long leftWeight = 0;
        long rightWeight = 0;

        _activeSharedProviders.Clear();
        _sharedProviderCount = 0;
        _hasSharedProviders = false;

        foreach (KeyValuePair<MonoBehaviour, CombinedProviderData> pair in _combinedProviders)
        {
            MonoBehaviour owner = pair.Key;
            CombinedProviderData providerData = pair.Value;

            if (owner == null || providerData.Provider == null)
            {
                continue;
            }

            int weight = Mathf.Max(0, providerData.Weight);

            if (providerData.IsOnLeft && !providerData.IsOnRight)
            {
                leftWeight += weight;
                continue;
            }

            if (providerData.IsOnRight && !providerData.IsOnLeft)
            {
                rightWeight += weight;
                continue;
            }

            if (!providerData.IsOnLeft || !providerData.IsOnRight)
            {
                continue;
            }

            _hasSharedProviders = true;
            _sharedProviderCount++;
            _activeSharedProviders.Add(owner);

            ProviderSide side = ResolveSharedProviderSide(owner, providerData);

            switch (side)
            {
                case ProviderSide.Left:
                    leftWeight += weight;
                    break;

                case ProviderSide.Right:
                    rightWeight += weight;
                    break;
            }
        }

        CleanupSharedAssignments();

        _leftRawWeight = ToSafeInt(leftWeight);
        _rightRawWeight = ToSafeInt(rightWeight);

        SeesawResolution resolution = _stateResolver.Resolve(
            _leftRawWeight,
            _rightRawWeight);

        _mover.SetResolution(resolution);
        _isResolving = false;
    }

    private void BuildCombinedProviderMap()
    {
        _combinedProviders.Clear();

        for (int i = 0; i < _leftProviders.Count; i++)
        {
            WeightSensor.ProviderData source = _leftProviders[i];
            if (source.Owner == null)
            {
                continue;
            }

            _combinedProviders.TryGetValue(source.Owner, out CombinedProviderData combined);
            combined.Provider = source.Provider;
            combined.IsOnLeft = true;
            combined.LeftColliderCount = source.ColliderCount;
            combined.LeftWorldCenter = source.WorldCenter;
            combined.Weight = source.Weight;
            _combinedProviders[source.Owner] = combined;
        }

        for (int i = 0; i < _rightProviders.Count; i++)
        {
            WeightSensor.ProviderData source = _rightProviders[i];
            if (source.Owner == null)
            {
                continue;
            }

            _combinedProviders.TryGetValue(source.Owner, out CombinedProviderData combined);
            combined.Provider = source.Provider;
            combined.IsOnRight = true;
            combined.RightColliderCount = source.ColliderCount;
            combined.RightWorldCenter = source.WorldCenter;
            combined.Weight = source.Weight;
            _combinedProviders[source.Owner] = combined;
        }
    }

    private ProviderSide ResolveSharedProviderSide(
        MonoBehaviour owner,
        CombinedProviderData providerData)
    {
        if (providerData.LeftColliderCount > providerData.RightColliderCount)
        {
            return StoreAssignment(owner, ProviderSide.Left);
        }

        if (providerData.RightColliderCount > providerData.LeftColliderCount)
        {
            return StoreAssignment(owner, ProviderSide.Right);
        }

        Vector3 combinedWorldCenter = CalculateCombinedWorldCenter(providerData);
        Vector3 localCenter = _pivot.InverseTransformPoint(combinedWorldCenter);
        Vector3 normalizedAxis = _leftRightLocalAxis.normalized;
        float signedDistance = Vector3.Dot(localCenter, normalizedAxis);

        if (signedDistance < -_sharedProviderCenterDeadZone)
        {
            return StoreAssignment(owner, ProviderSide.Left);
        }

        if (signedDistance > _sharedProviderCenterDeadZone)
        {
            return StoreAssignment(owner, ProviderSide.Right);
        }

        if (_sharedAssignments.TryGetValue(owner, out ProviderSide previousSide))
        {
            return previousSide;
        }

        return ProviderSide.None;
    }

    private ProviderSide StoreAssignment(MonoBehaviour owner, ProviderSide side)
    {
        _sharedAssignments[owner] = side;
        return side;
    }

    private static Vector3 CalculateCombinedWorldCenter(CombinedProviderData providerData)
    {
        int leftCount = Mathf.Max(0, providerData.LeftColliderCount);
        int rightCount = Mathf.Max(0, providerData.RightColliderCount);
        int totalCount = leftCount + rightCount;

        if (totalCount <= 0)
        {
            return Vector3.zero;
        }

        return ((providerData.LeftWorldCenter * leftCount) +
                (providerData.RightWorldCenter * rightCount)) / totalCount;
    }

    private void CleanupSharedAssignments()
    {
        _staleAssignments.Clear();

        foreach (KeyValuePair<MonoBehaviour, ProviderSide> pair in _sharedAssignments)
        {
            MonoBehaviour owner = pair.Key;
            if (owner == null || !_activeSharedProviders.Contains(owner))
            {
                _staleAssignments.Add(owner);
            }
        }

        for (int i = 0; i < _staleAssignments.Count; i++)
        {
            _sharedAssignments.Remove(_staleAssignments[i]);
        }
    }

    private static int ToSafeInt(long value)
    {
        if (value <= 0)
        {
            return 0;
        }

        return value > int.MaxValue
            ? int.MaxValue
            : (int)value;
    }

    private void OnValidate()
    {
        _sharedProviderCenterDeadZone = Mathf.Max(0f, _sharedProviderCenterDeadZone);

        if (_leftRightLocalAxis.sqrMagnitude <= Mathf.Epsilon)
        {
            _leftRightLocalAxis = Vector3.right;
        }

        if (_stateResolver == null)
        {
            _stateResolver = GetComponent<SeesawStateResolver>();
        }

        if (_mover == null)
        {
            _mover = GetComponentInChildren<SeesawMover>(true);
        }
    }
}