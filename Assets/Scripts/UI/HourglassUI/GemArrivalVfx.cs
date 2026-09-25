using UnityEngine;

/// <summary>Aplica a los fragmentos el mismo cristal que acaba de encenderse en la UI.</summary>
[DisallowMultipleComponent]
public sealed class GemArrivalVfx : MonoBehaviour
{
    [SerializeField] private ParticleSystemRenderer[] _fragmentRenderers;

    public void SetGemMaterial(Material material)
    {
        if (material == null || _fragmentRenderers == null)
            return;

        foreach (ParticleSystemRenderer fragment in _fragmentRenderers)
            if (fragment != null)
                fragment.sharedMaterial = material;
    }
}
