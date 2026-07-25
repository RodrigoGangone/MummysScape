using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Detecta componentes que implementan IWeightProvider dentro de un volumen trigger, evita
/// duplicar su peso cuando poseen varios colliders y notifica cambios sin realizar búsquedas por frame.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider))]
public class WeightSensor : MonoBehaviour
{
    public readonly struct ProviderData
    {
        public ProviderData(
            MonoBehaviour owner,
            IWeightProvider provider,
            int colliderCount,
            Vector3 worldCenter,
            int weight)
        {
            Owner = owner;
            Provider = provider;
            ColliderCount = colliderCount;
            WorldCenter = worldCenter;
            Weight = weight;
        }

        public MonoBehaviour Owner { get; }
        public IWeightProvider Provider { get; }
        public int ColliderCount { get; }
        public Vector3 WorldCenter { get; }
        public int Weight { get; }
    }

    private sealed class ProviderRecord
    {
        public ProviderRecord(MonoBehaviour owner, IWeightProvider provider)
        {
            Owner = owner;
            Provider = provider;
        }

        public MonoBehaviour Owner { get; }
        public IWeightProvider Provider { get; }
        public HashSet<Collider> Colliders { get; } = new HashSet<Collider>();
    }

    private sealed class ColliderRecord
    {
        public ColliderRecord(MonoBehaviour providerOwner, float lastSeenFixedTime)
        {
            ProviderOwner = providerOwner;
            LastSeenFixedTime = lastSeenFixedTime;
        }

        public MonoBehaviour ProviderOwner { get; }
        public float LastSeenFixedTime { get; set; }
    }

    [Header("Recovery")]
    [SerializeField, Min(0.05f)] private float _cleanupInterval = 0.25f;
    [SerializeField, Min(0.1f)] private float _staleColliderGrace = 0.5f;

    [Header("Debug")]
    [SerializeField] private int _totalWeight;
    [SerializeField] private int _providerCount;

    private readonly Dictionary<MonoBehaviour, ProviderRecord> _providers =
        new Dictionary<MonoBehaviour, ProviderRecord>();

    private readonly Dictionary<Collider, ColliderRecord> _colliders =
        new Dictionary<Collider, ColliderRecord>();

    private readonly List<Collider> _staleColliders = new List<Collider>(8);
    private readonly List<MonoBehaviour> _componentBuffer = new List<MonoBehaviour>(8);

    private BoxCollider _trigger;
    private Rigidbody _localRigidbody;
    private float _cleanupElapsed;

    public int TotalWeight => _totalWeight;
    public int ProviderCount => _providerCount;

    public event Action<int> TotalWeightChanged;
    public event Action SensorChanged;

    protected virtual void Awake()
    {
        _trigger = GetComponent<BoxCollider>();
        _localRigidbody = GetComponent<Rigidbody>();

        _trigger.isTrigger = true;

        if (_localRigidbody != null)
        {
            ConfigureLocalRigidbody(_localRigidbody);
        }
        else if (GetComponentInParent<Rigidbody>() == null)
        {
            Debug.LogError(
                $"{nameof(WeightSensor)} requiere un Rigidbody propio o uno en su jerarquía padre para recibir eventos trigger.",
                this);

            enabled = false;
        }
    }

    protected virtual void FixedUpdate()
    {
        _cleanupElapsed += Time.fixedDeltaTime;
        if (_cleanupElapsed < _cleanupInterval)
        {
            return;
        }

        _cleanupElapsed = 0f;
        CleanupStaleColliders();
    }

    protected virtual void OnTriggerEnter(Collider other)
    {
        TouchCollider(other);
    }

    protected virtual void OnTriggerStay(Collider other)
    {
        TouchCollider(other);
    }

    protected virtual void OnTriggerExit(Collider other)
    {
        UnregisterCollider(other);
    }

    public void CopyProviderDataTo(List<ProviderData> destination)
    {
        if (destination == null)
        {
            throw new ArgumentNullException(nameof(destination));
        }

        destination.Clear();

        foreach (KeyValuePair<MonoBehaviour, ProviderRecord> pair in _providers)
        {
            ProviderRecord record = pair.Value;
            if (!IsProviderAvailable(record))
            {
                continue;
            }

            int colliderCount = 0;
            Vector3 centerAccumulator = Vector3.zero;

            foreach (Collider collider in record.Colliders)
            {
                if (!IsColliderAvailable(collider))
                {
                    continue;
                }

                centerAccumulator += collider.bounds.center;
                colliderCount++;
            }

            if (colliderCount == 0)
            {
                continue;
            }

            destination.Add(new ProviderData(
                record.Owner,
                record.Provider,
                colliderCount,
                centerAccumulator / colliderCount,
                Mathf.Max(0, record.Provider.Weight)));
        }
    }

    private void TouchCollider(Collider other)
    {
        if (!IsColliderAvailable(other))
        {
            return;
        }

        float now = Time.fixedTime;

        if (_colliders.TryGetValue(other, out ColliderRecord existingRecord))
        {
            if (_providers.TryGetValue(existingRecord.ProviderOwner, out ProviderRecord existingProvider) &&
                IsProviderAvailable(existingProvider))
            {
                existingRecord.LastSeenFixedTime = now;
                return;
            }

            UnregisterCollider(other);
        }

        if (!TryResolveProvider(other, out MonoBehaviour owner, out IWeightProvider provider))
        {
            return;
        }

        bool isNewProvider = !_providers.TryGetValue(owner, out ProviderRecord providerRecord);
        if (isNewProvider)
        {
            providerRecord = new ProviderRecord(owner, provider);
            _providers.Add(owner, providerRecord);
            provider.WeightChanged += HandleProviderWeightChanged;
        }

        if (!providerRecord.Colliders.Add(other))
        {
            return;
        }

        _colliders.Add(other, new ColliderRecord(owner, now));
        RecalculateTotalWeight(forceSensorChanged: true);
    }

    private bool TryResolveProvider(
        Collider sourceCollider,
        out MonoBehaviour owner,
        out IWeightProvider provider)
    {
        Transform current = sourceCollider.transform;

        while (current != null)
        {
            _componentBuffer.Clear();
            current.GetComponents(_componentBuffer);

            for (int i = 0; i < _componentBuffer.Count; i++)
            {
                MonoBehaviour component = _componentBuffer[i];
                if (component == null || !component.isActiveAndEnabled || component is not IWeightProvider candidate)
                {
                    continue;
                }

                owner = component;
                provider = candidate;
                return true;
            }

            current = current.parent;
        }

        owner = null;
        provider = null;
        return false;
    }

    private void UnregisterCollider(Collider collider)
    {
        if (ReferenceEquals(collider, null) ||
            !_colliders.TryGetValue(collider, out ColliderRecord colliderRecord))
        {
            return;
        }

        _colliders.Remove(collider);

        if (!_providers.TryGetValue(colliderRecord.ProviderOwner, out ProviderRecord providerRecord))
        {
            RecalculateTotalWeight(forceSensorChanged: true);
            return;
        }

        providerRecord.Colliders.Remove(collider);

        if (providerRecord.Colliders.Count > 0)
        {
            RecalculateTotalWeight(forceSensorChanged: true);
            return;
        }

        providerRecord.Provider.WeightChanged -= HandleProviderWeightChanged;
        _providers.Remove(colliderRecord.ProviderOwner);
        RecalculateTotalWeight(forceSensorChanged: true);
    }

    private void HandleProviderWeightChanged()
    {
        RecalculateTotalWeight(forceSensorChanged: true);
    }

    private void RecalculateTotalWeight(bool forceSensorChanged)
    {
        long accumulatedWeight = 0;

        foreach (KeyValuePair<MonoBehaviour, ProviderRecord> pair in _providers)
        {
            ProviderRecord record = pair.Value;
            if (!IsProviderAvailable(record))
            {
                continue;
            }

            accumulatedWeight += Mathf.Max(0, record.Provider.Weight);
        }

        int newTotal = accumulatedWeight > int.MaxValue
            ? int.MaxValue
            : (int)accumulatedWeight;

        bool totalChanged = newTotal != _totalWeight;

        _totalWeight = newTotal;
        _providerCount = _providers.Count;

        if (totalChanged)
        {
            TotalWeightChanged?.Invoke(_totalWeight);
        }

        if (totalChanged || forceSensorChanged)
        {
            SensorChanged?.Invoke();
        }
    }

    private void CleanupStaleColliders()
    {
        _staleColliders.Clear();
        float now = Time.fixedTime;

        foreach (KeyValuePair<Collider, ColliderRecord> pair in _colliders)
        {
            Collider collider = pair.Key;
            ColliderRecord colliderRecord = pair.Value;

            bool invalidCollider = !IsColliderAvailable(collider);
            bool invalidProvider =
                !_providers.TryGetValue(colliderRecord.ProviderOwner, out ProviderRecord providerRecord) ||
                !IsProviderAvailable(providerRecord);
            bool wasNotSeenRecently = now - colliderRecord.LastSeenFixedTime > _staleColliderGrace;

            if (invalidCollider || invalidProvider || wasNotSeenRecently)
            {
                _staleColliders.Add(collider);
            }
        }

        for (int i = 0; i < _staleColliders.Count; i++)
        {
            UnregisterCollider(_staleColliders[i]);
        }
    }

    private void ClearTracking()
    {
        foreach (KeyValuePair<MonoBehaviour, ProviderRecord> pair in _providers)
        {
            ProviderRecord record = pair.Value;
            if (record.Provider != null)
            {
                record.Provider.WeightChanged -= HandleProviderWeightChanged;
            }
        }

        _providers.Clear();
        _colliders.Clear();
        _staleColliders.Clear();
        _componentBuffer.Clear();
        _cleanupElapsed = 0f;
        _providerCount = 0;

        if (_totalWeight != 0)
        {
            _totalWeight = 0;
            TotalWeightChanged?.Invoke(_totalWeight);
        }

        SensorChanged?.Invoke();
    }

    private static bool IsProviderAvailable(ProviderRecord record)
    {
        return record != null &&
               record.Owner != null &&
               record.Owner.isActiveAndEnabled &&
               record.Provider != null;
    }

    private static bool IsColliderAvailable(Collider collider)
    {
        return collider != null && collider.enabled && collider.gameObject.activeInHierarchy;
    }

    private static void ConfigureLocalRigidbody(Rigidbody body)
    {
        body.isKinematic = true;
        body.useGravity = false;
        body.interpolation = RigidbodyInterpolation.None;
        body.constraints = RigidbodyConstraints.FreezeAll;
    }

    protected virtual void OnDisable()
    {
        ClearTracking();
    }

    protected virtual void OnDestroy()
    {
        ClearTracking();
    }

    protected virtual void OnValidate()
    {
        _cleanupInterval = Mathf.Max(0.05f, _cleanupInterval);
        _staleColliderGrace = Mathf.Max(_cleanupInterval + 0.05f, _staleColliderGrace);

        BoxCollider trigger = GetComponent<BoxCollider>();
        if (trigger != null)
        {
            trigger.isTrigger = true;
        }
    }

    private void OnDrawGizmosSelected()
    {
        BoxCollider box = GetComponent<BoxCollider>();
        if (box == null)
        {
            return;
        }

        Matrix4x4 previousMatrix = Gizmos.matrix;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(box.center, box.size);
        Gizmos.matrix = previousMatrix;
    }
}