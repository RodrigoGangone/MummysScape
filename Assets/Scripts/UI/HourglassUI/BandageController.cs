using UnityEngine;

/// <summary> 
/// Animador de Visibilidad: Controla la aparición y desaparición gradual de los iconos de vendas 
/// mediante MaterialPropertyBlocks, evitando la creación de instancias de material innecesarias. 
/// </summary>

public class BandageController : MonoBehaviour
{
    [SerializeField]
    private Renderer bandageRenderer;

    [SerializeField]
    private float transitionSpeed = 0.5f;

    public bool showBandage = true;

    private MaterialPropertyBlock _propertyBlock;
    private static readonly int OffsetID = Shader.PropertyToID("_Offset");
    private float _currentOffset = 0f;

    private void Awake()
    {
        _propertyBlock = new MaterialPropertyBlock();
        _currentOffset = showBandage ? 1f : 0f;
        UpdateMaterial();
    }

    private void Update()
    {
        float targetOffset = showBandage ? 1f : 0f;

        if (!Mathf.Approximately(_currentOffset, targetOffset))
        {
            _currentOffset = Mathf.MoveTowards(_currentOffset, targetOffset, transitionSpeed * Time.deltaTime);
            UpdateMaterial();
        }
    }

    private void UpdateMaterial()
    {
        bandageRenderer.GetPropertyBlock(_propertyBlock);
        _propertyBlock.SetFloat(OffsetID, _currentOffset);
        bandageRenderer.SetPropertyBlock(_propertyBlock);
    }
}