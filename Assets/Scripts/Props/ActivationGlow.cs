using System.Collections.Generic;
using UnityEngine;

/// <summary>Emisión temporal por instancia y por slot, sin clonar ni modificar materiales.</summary>
[DisallowMultipleComponent]
public sealed class ActivationGlow : MonoBehaviour
{
    [SerializeField] private Renderer[] renderers;
    [SerializeField, ColorUsage(false, true)] private Color color = new Color(1f, 0.72f, 0.18f, 1f);
    public static readonly int IntensityProperty = Shader.PropertyToID("_ActivationGlowIntensity");
    public static readonly int ColorProperty = Shader.PropertyToID("_ActivationGlowColor");

    private sealed class Slot
    {
        public Renderer Renderer;
        public int Index;
        public readonly MaterialPropertyBlock Original = new MaterialPropertyBlock();
        public readonly MaterialPropertyBlock Working = new MaterialPropertyBlock();
    }
    private readonly List<Slot> _slots = new List<Slot>();
    private bool _resolved;
    private bool _pulsing;

    public void Configure(Renderer[] targets)
    {
        Restore();
        renderers = targets;
        _resolved = false;
    }

    public static bool IsSurface(Renderer target)
    {
        if (!(target is MeshRenderer) && !(target is SkinnedMeshRenderer)) return false;
        foreach (var material in target.sharedMaterials)
            if (material != null && material.shader != null &&
                (material.shader.name.ToLowerInvariant().Contains("decal") ||
                 material.shader.name.ToLowerInvariant().Contains("particle"))) return false;
        return true;
    }

    private void Resolve()
    {
        _slots.Clear();
        var targets = renderers != null && renderers.Length > 0
            ? renderers : GetComponentsInChildren<Renderer>(true);
        var seen = new HashSet<Renderer>();
        foreach (var target in targets)
        {
            if (target == null || !seen.Add(target) || !IsSurface(target)) continue;
            var materials = target.sharedMaterials;
            for (int i = 0; i < materials.Length; i++)
            {
                var material = materials[i];
                if (material == null) continue;
                if (!material.HasProperty(IntensityProperty) || !material.HasProperty(ColorProperty))
                {
                    Debug.LogWarning($"[ActivationGlow] {target.name}: el material {material.name} no admite emisión de activación.", target);
                    continue;
                }
                _slots.Add(new Slot { Renderer = target, Index = i });
            }
        }
        _resolved = true;
    }

    public void SetIntensity(float intensity)
    {
        if (intensity <= 0f) { Restore(); return; }
        if (!_resolved) Resolve();
        foreach (var slot in _slots)
        {
            if (slot.Renderer == null) continue;
            if (!_pulsing) slot.Renderer.GetPropertyBlock(slot.Original, slot.Index);
            slot.Renderer.GetPropertyBlock(slot.Working, slot.Index);
            // Un bloque por slot tiene prioridad sobre el bloque general del renderer.
            if (slot.Working.isEmpty) slot.Renderer.GetPropertyBlock(slot.Working);
            slot.Working.SetFloat(IntensityProperty, intensity);
            slot.Working.SetColor(ColorProperty, color);
            slot.Renderer.SetPropertyBlock(slot.Working, slot.Index);
        }
        _pulsing = true;
    }

    public void Restore()
    {
        if (!_pulsing) return;
        foreach (var slot in _slots)
            if (slot.Renderer != null) slot.Renderer.SetPropertyBlock(slot.Original, slot.Index);
        _pulsing = false;
    }

    private void OnDisable() => Restore();
    private void OnDestroy() => Restore();
}
