using UnityEngine;

public class BandageController : MonoBehaviour
{
    // Asigna el Renderer de tu venda desde el Inspector de Unity.
    [SerializeField]
    private Renderer bandageRenderer;

    // Controla la velocidad de la aparición/desaparición.
    [SerializeField]
    private float transitionSpeed = 0.5f;

    // El booleano para controlar el efecto desde otros scripts o el Inspector.
    public bool showBandage = true;

    // Usamos un MaterialPropertyBlock para no crear instancias de material, es más eficiente.
    private MaterialPropertyBlock _propertyBlock;
    private static readonly int OffsetID = Shader.PropertyToID("_Offset");
    private float _currentOffset = 0f;

    private void Awake()
    {
        _propertyBlock = new MaterialPropertyBlock();
        // Asegúrate de que el valor inicial coincida con el estado de 'showBandage'.
        _currentOffset = showBandage ? 1f : 0f;
        UpdateMaterial();
    }

    private void Update()
    {
        /*if (Input.GetKeyDown(KeyCode.J))
        {
            showBandage = true;
        }
        if (Input.GetKeyDown(KeyCode.K))
        {
            showBandage = false;
        }*/
        
        // El valor objetivo (0 para oculto, 1 para visible).
        float targetOffset = showBandage ? 1f : 0f;

        // Si el valor actual no es el objetivo, lo movemos progresivamente.
        if (!Mathf.Approximately(_currentOffset, targetOffset))
        {
            _currentOffset = Mathf.MoveTowards(_currentOffset, targetOffset, transitionSpeed * Time.deltaTime);
            UpdateMaterial();
        }
    }

    private void UpdateMaterial()
    {
        // Obtenemos las propiedades actuales para no sobreescribir otras.
        bandageRenderer.GetPropertyBlock(_propertyBlock);
        // Establecemos nuestro valor de Offset.
        _propertyBlock.SetFloat(OffsetID, _currentOffset);
        // Aplicamos el bloque de propiedades al renderer.
        bandageRenderer.SetPropertyBlock(_propertyBlock);
    }
}