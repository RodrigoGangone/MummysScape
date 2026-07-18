using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Detecta proveedores de peso mediante un volumen trigger independiente, contabiliza cada
/// proveedor una sola vez aunque tenga varios colliders y repara entradas, salidas o referencias perdidas.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider))]
[RequireComponent(typeof(Rigidbody))]
public sealed class PressureButtonWeightSensor : MonoBehaviour
{
    private sealed class ProviderRecord
    {
        public readonly HashSet<Collider> Colliders = new HashSet<Collider>();
    }

    private sealed class ColliderRecord
    {
        public ColliderRecord(WeightProviderBehaviour provider, float lastSeenFixedTime)
        {
            Provider = provider;
            LastSeenFixedTime = lastSeenFixedTime;
        }

        public WeightProviderBehaviour Provider { get; }
        public float LastSeenFixedTime { get; set; }
    }

    [Header("Recovery")]
    [SerializeField, Min(0.05f)] private float _cleanupInterval = 0.25f;
    [SerializeField, Min(0.1f)] private float _staleColliderGrace = 0.5f;

    private readonly Dictionary<WeightProviderBehaviour, ProviderRecord> _providers =
        new Dictionary<WeightProviderBehaviour, ProviderRecord>();

    private readonly Dictionary<Collider, ColliderRecord> _colliders =
        new Dictionary<Collider, ColliderRecord>();

    private readonly List<Collider> _staleColliders = new List<Collider>(8);

    private BoxCollider _trigger;
    private Rigidbody _rigidbody;
    private float _cleanupElapsed;

    public int TotalWeight { get; private set; }
    public event Action<int> TotalWeightChanged;

    private void Awake()
    {
        _trigger = GetComponent<BoxCollider>();
        _rigidbody = GetComponent<Rigidbody>();

        _trigger.isTrigger = true;
        _rigidbody.isKinematic = true;
        _rigidbody.useGravity = false;
        _rigidbody.interpolation = RigidbodyInterpolation.None;
        _rigidbody.constraints = RigidbodyConstraints.FreezeAll;
    }

    private void FixedUpdate()
    {
        _cleanupElapsed += Time.fixedDeltaTime;
        if (_cleanupElapsed < _cleanupInterval)
        {
            return;
        }

        _cleanupElapsed = 0f;
        CleanupStaleColliders();
    }

    private void OnTriggerEnter(Collider other)
    {
        TouchCollider(other);
    }

    private void OnTriggerStay(Collider other)
    {
        TouchCollider(other);
    }

    private void OnTriggerExit(Collider other)
    {
        UnregisterCollider(other);
    }

    private void TouchCollider(Collider other)
    {
        if (other == null || !other.enabled || !other.gameObject.activeInHierarchy)
        {
            return;
        }

        float now = Time.fixedTime;

        if (_colliders.TryGetValue(other, out ColliderRecord existingRecord))
        {
            if (existingRecord.Provider != null && existingRecord.Provider.isActiveAndEnabled)
            {
                existingRecord.LastSeenFixedTime = now;
                return;
            }

            UnregisterCollider(other);
        }

        WeightProviderBehaviour provider = other.GetComponentInParent<WeightProviderBehaviour>();
        if (provider == null || !provider.isActiveAndEnabled)
        {
            return;
        }

        bool isNewProvider = !_providers.TryGetValue(provider, out ProviderRecord providerRecord);
        if (isNewProvider)
        {
            providerRecord = new ProviderRecord();
            _providers.Add(provider, providerRecord);
            provider.WeightChanged += HandleProviderWeightChanged;
        }

        if (!providerRecord.Colliders.Add(other))
        {
            return;
        }

        _colliders.Add(other, new ColliderRecord(provider, now));

        if (isNewProvider)
        {
            RecalculateTotalWeight();
        }
    }

    private void UnregisterCollider(Collider collider)
    {
        if (ReferenceEquals(collider, null) || !_colliders.TryGetValue(collider, out ColliderRecord colliderRecord))
        {
            return;
        }

        _colliders.Remove(collider);

        WeightProviderBehaviour provider = colliderRecord.Provider;
        if (ReferenceEquals(provider, null) || !_providers.TryGetValue(provider, out ProviderRecord providerRecord))
        {
            RecalculateTotalWeight();
            return;
        }

        providerRecord.Colliders.Remove(collider);
        if (providerRecord.Colliders.Count > 0)
        {
            return;
        }

        if (provider != null)
        {
            provider.WeightChanged -= HandleProviderWeightChanged;
        }

        _providers.Remove(provider);
        RecalculateTotalWeight();
    }

    private void HandleProviderWeightChanged()
    {
        RecalculateTotalWeight();
    }

    private void RecalculateTotalWeight()
    {
        int total = 0;

        foreach (KeyValuePair<WeightProviderBehaviour, ProviderRecord> pair in _providers)
        {
            WeightProviderBehaviour provider = pair.Key;
            if (provider == null || !provider.isActiveAndEnabled)
            {
                continue;
            }

            total += Mathf.Max(0, provider.Weight);
        }

        if (total == TotalWeight)
        {
            return;
        }

        TotalWeight = total;
        TotalWeightChanged?.Invoke(TotalWeight);
    }

    private void CleanupStaleColliders()
    {
        _staleColliders.Clear();
        float now = Time.fixedTime;

        foreach (KeyValuePair<Collider, ColliderRecord> pair in _colliders)
        {
            Collider collider = pair.Key;
            ColliderRecord record = pair.Value;
            WeightProviderBehaviour provider = record.Provider;

            bool invalidCollider = collider == null || !collider.enabled || !collider.gameObject.activeInHierarchy;
            bool invalidProvider = provider == null || !provider.isActiveAndEnabled;
            bool wasNotSeenRecently = now - record.LastSeenFixedTime > _staleColliderGrace;

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
        foreach (KeyValuePair<WeightProviderBehaviour, ProviderRecord> pair in _providers)
        {
            WeightProviderBehaviour provider = pair.Key;
            if (provider != null)
            {
                provider.WeightChanged -= HandleProviderWeightChanged;
            }
        }

        _providers.Clear();
        _colliders.Clear();
        _staleColliders.Clear();
        _cleanupElapsed = 0f;

        if (TotalWeight != 0)
        {
            TotalWeight = 0;
            TotalWeightChanged?.Invoke(TotalWeight);
        }
    }

    private void OnDisable()
    {
        ClearTracking();
    }

    private void OnDestroy()
    {
        ClearTracking();
    }

    private void OnValidate()
    {
        _cleanupInterval = Mathf.Max(0.05f, _cleanupInterval);
        _staleColliderGrace = Mathf.Max(_cleanupInterval + 0.05f, _staleColliderGrace);

        BoxCollider trigger = GetComponent<BoxCollider>();
        if (trigger != null)
        {
            trigger.isTrigger = true;
        }
    }
}
