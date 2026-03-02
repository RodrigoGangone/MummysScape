using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static PauseUtils;

/// <summary>
/// Proveedor de Escena: Centraliza la referencia de los geysers disponibles y gestiona 
/// el pool y movimiento de las partículas que viajan hacia ellos para activarlos.
/// </summary>

public class GeyserPointProvider : MonoBehaviour, IPausable
{
    [Tooltip("Geysers en escena. Se usarán sus transforms como destinos.")]
    public List<Geyser> geysers = new();

    [Tooltip("Origen por defecto para lanzar partículas (ej: viewScorpion del boss).")]
    public Transform defaultTravelOrigin;

    [Header("Debug")]
    [SerializeField] private bool drawDebug = true;

    private readonly List<ParticleSystem> _pool = new();
    private readonly List<int> _free = new();

    private List<Transform> _lastTargets;
    private Transform _lastOrigin;
    private float _lastRadius;

    [SerializeField, Min(0.001f)] private float arrivalThreshold = 0.05f;

    private bool _paused;
    
    private ParticleSystem Acquire(ParticleSystem prefab)
    {
        for (int i = 0; i < _pool.Count; i++)
        {
            if (_free.Contains(i))
            {
                _free.Remove(i);
                var ps = _pool[i];
                ps.gameObject.SetActive(true);
                return ps;
            }
        }
        var inst = Instantiate(prefab, transform);
        _pool.Add(inst);
        return inst;
    }

    private void Release(ParticleSystem ps)
    {
        if (ps == null) return;
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        ps.gameObject.SetActive(false);
        int idx = _pool.IndexOf(ps);
        if (idx >= 0 && !_free.Contains(idx)) _free.Add(idx);
    }
    
    public Coroutine RunTravelFXToGeysers(
        IReadOnlyList<Geyser> selected,
        ParticleSystem prefab,
        float travelSpeed,
        Transform explicitOrigin,
        Action onArrived)
    {
        var origin = explicitOrigin != null ? explicitOrigin :
                     (defaultTravelOrigin != null ? defaultTravelOrigin : transform);

        _lastTargets = selected.Where(g => g != null).Select(g => g.transform).ToList();
        _lastOrigin = origin;

        return StartCoroutine(TravelRoutine(
            _lastTargets,
            prefab,
            travelSpeed,
            origin,
            onArrived));
    }

    private IEnumerator TravelRoutine(
        IReadOnlyList<Transform> targets,
        ParticleSystem prefab,
        float speed,
        Transform origin,
        Action onArrived)
    {
        var actives = new List<(ParticleSystem ps, Transform target)>(targets.Count);

        foreach (var tf in targets)
        {
            if (tf == null) continue;
            var ps = Acquire(prefab);
            ps.transform.position = origin.position;
            ps.Play();
            actives.Add((ps, tf));
        }

        var done = new bool[actives.Count];
        int remaining = actives.Count;
        float eps2 = arrivalThreshold * arrivalThreshold;

        while (remaining > 0)
        {
            if (_paused)
            {
                for (int i = 0; i < actives.Count; i++)
                {
                    var ps = actives[i].ps;
                    if (ps != null && ps.isPlaying) ps.Pause();
                }

                yield return WaitWhilePaused(() => _paused);

                for (int i = 0; i < actives.Count; i++)
                {
                    var ps = actives[i].ps;
                    if (ps != null && !ps.isPlaying) ps.Play();
                }

                yield return null;
                continue;
            }

            for (int i = 0; i < actives.Count; i++)
            {
                if (done[i]) continue;

                var (ps, target) = actives[i];
                if (ps == null || target == null) { done[i] = true; remaining--; continue; }

                var cur  = ps.transform.position;
                var dst  = target.position;
                var next = Vector3.MoveTowards(cur, dst, speed * Time.deltaTime);
                ps.transform.position = next;

                if ((next - dst).sqrMagnitude <= eps2)
                {
                    done[i] = true;
                    remaining--;
                }
            }
            yield return null;
        }

        foreach (var (ps, _) in actives) Release(ps);
        onArrived?.Invoke();
    }
    
    public void OnPauseChanged(bool paused) => _paused = paused;
    private void OnEnable() => GameEventManager.Instance.levelEvents.OnPauseChanged.Register<bool>(OnPauseChanged);
    private void OnDisable() => GameEventManager.Instance.levelEvents.OnPauseChanged.Unregister<bool>(OnPauseChanged);

    
    private void OnDrawGizmos()
    {
        if (!drawDebug || _lastTargets == null || _lastOrigin == null) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(_lastOrigin.position, 0.18f);

        if (_lastRadius > 0f)
        {
            Gizmos.color = new Color(1f, 1f, 0f, 0.12f);
            Gizmos.DrawWireSphere(_lastOrigin.position, _lastRadius);
        }

        Gizmos.color = Color.cyan;
        foreach (var t in _lastTargets)
        {
            if (t == null) continue;
            Gizmos.DrawLine(_lastOrigin.position, t.position);
            Gizmos.DrawSphere(t.position, 0.1f);
        }
    }
}
