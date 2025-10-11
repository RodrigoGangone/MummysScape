using UnityEngine;

[DisallowMultipleComponent]
public class ExcludeFromBlobShadow : MonoBehaviour
{
    [Tooltip("Bit 0..31 de Rendering Layer que el projector EXCLUIRÁ.")]
    [Range(0,31)] public int layerBit = 20;

    void Awake()
    {
        uint m = 1u << Mathf.Clamp(layerBit, 0, 31);
        foreach (var r in GetComponentsInChildren<Renderer>())
            r.renderingLayerMask |= m;
    }
}