using System.Collections.Generic;
using UnityEngine;

/// <summary>Una barrera de preparación y un reloj de física por cambio de aporte compartido.</summary>
[DefaultExecutionOrder(1000)]
public sealed class SpikeActivationGroup : MonoBehaviour
{
    private sealed class Batch
    {
        public readonly HashSet<SpikeTrapController> Traps = new HashSet<SpikeTrapController>();
        public readonly HashSet<PressureButtonStateResolver> Buttons = new HashSet<PressureButtonStateResolver>();
        public bool Started;
        public float Elapsed;
        public float Duration;
    }

    private static SpikeActivationGroup _instance;
    private readonly List<Batch> _batches = new List<Batch>();

    internal static void Request(SpikeTrapController trap, IEnumerable<PressureButtonStateResolver> changedButtons)
    {
        if (_instance == null)
            _instance = new GameObject("Spears activation groups").AddComponent<SpikeActivationGroup>();
        var batch = new Batch();
        batch.Traps.Add(trap);
        batch.Buttons.UnionWith(changedButtons);
        // Unión transitiva: dos aportes coincidentes pueden compartir una tercera lanza.
        bool merged;
        do
        {
            merged = false;
            for (int i = _instance._batches.Count - 1; i >= 0; i--)
            {
                var other = _instance._batches[i];
                if (!batch.Traps.Overlaps(other.Traps) && !batch.Buttons.Overlaps(other.Buttons)) continue;
                batch.Traps.UnionWith(other.Traps);
                batch.Buttons.UnionWith(other.Buttons);
                _instance._batches.RemoveAt(i);
                merged = true;
            }
        } while (merged);
        foreach (var member in batch.Traps)
            if (member != null) member.JoinActivationGroup();
        _instance._batches.Add(batch);
    }

    private void FixedUpdate()
    {
        for (int i = _batches.Count - 1; i >= 0; i--)
        {
            var batch = _batches[i];
            batch.Traps.RemoveWhere(trap => trap == null || !trap.isActiveAndEnabled || !trap.IsTransitioning);
            if (batch.Traps.Count == 0) { _batches.RemoveAt(i); continue; }
            bool waiting = false;
            foreach (var trap in batch.Traps)
                waiting |= trap.ActivationPaused || !trap.GroupReady;
            if (waiting) continue;
            if (!batch.Started)
            {
                batch.Started = true;
                foreach (var trap in batch.Traps) batch.Duration = Mathf.Max(batch.Duration, trap.MoveDuration);
                foreach (var trap in batch.Traps) trap.BeginGroupMovement();
            }
            batch.Elapsed += Time.fixedDeltaTime;
            float t = batch.Duration <= Mathf.Epsilon ? 1f : Mathf.Clamp01(batch.Elapsed / batch.Duration);
            foreach (var trap in batch.Traps) trap.TickGroupMovement(t);
            if (t >= 1f) _batches.RemoveAt(i);
        }
    }

    private void OnDestroy()
    {
        foreach (var batch in _batches)
            foreach (var trap in batch.Traps) if (trap != null) trap.LeaveActivationGroup();
        if (_instance == this) _instance = null;
    }
}
