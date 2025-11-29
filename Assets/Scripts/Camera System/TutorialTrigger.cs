using UnityEngine;

[RequireComponent(typeof(Collider))]
public class TutorialTrigger : MonoBehaviour
{
    [Header("Tutorial asociado")] [SerializeField]
    private TutorialFocusPoint focusPoint;

    [Header("Configuración de Colisión")] 
    [SerializeField] private Vector3 sizeA = Vector3.one;
    [SerializeField] private Vector3 sizeB = Vector3.one * 2f;

    [Header("Offsets de Posición")]
    [Tooltip("Compensación (offset) del centro para el Tamaño A (Obligatorio).")]
    [SerializeField] private Vector3 centerOffsetA = Vector3.zero;
    [Tooltip("Compensación (offset) del centro para el Tamaño B (Opcional).")]
    [SerializeField] private Vector3 centerOffsetB = Vector3.zero;

    [Header("Configuración de Gizmos")]
    [SerializeField] private Color gizmoColorA = Color.green;
    [SerializeField] private Color gizmoColorB = Color.yellow;
    
    private BoxCollider _boxCollider;
    private bool _playerInside;
    private bool _isSizeA = true;

    private void Awake()
    {
        _boxCollider = GetComponent<BoxCollider>();
        if (_boxCollider == null)
        {
            Debug.LogError("TutorialTrigger requiere un BoxCollider para modificar sus tamaños.");
            enabled = false;
            return;
        }
        
        // Inicializar el Collider con el tamaño y offset de la zona A
        SetColliderSize(true);
    }

    /// <summary>
    /// Establece el tamaño y el centro (offset) del BoxCollider.
    /// </summary>
    /// <param name="useSizeA">Si es true, usa SizeA y OffsetA; si es false, usa SizeB y OffsetB.</param>
    private void SetColliderSize(bool useSizeA)
    {
        if (_boxCollider == null) return;

        // Establecer el tamaño y el centro basándose en si es A o B
        if (useSizeA)
        {
            _boxCollider.size = sizeA;
            _boxCollider.center = centerOffsetA; // Aplica el offset de A
        }
        else
        {
            _boxCollider.size = sizeB;
            _boxCollider.center = centerOffsetB; // Aplica el offset de B
        }
        
        _isSizeA = useSizeA;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("PlayerFather")) return;
        if (FocusManager.Instance == null) return;
        if (focusPoint == null) return;

        _playerInside = true;

        if (_isSizeA)
        {
            FocusManager.Instance.RequestTutorialFirstTime(focusPoint);

            // Cambia el tamaño y offset del collider de A a B
            SetColliderSize(false);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("PlayerFather")) return;
        _playerInside = false;
    }

    private void Update()
    {
        if (!_playerInside) return;
        if (FocusManager.Instance == null) return;
        if (focusPoint == null) return;

        if (!_isSizeA)
        {
            if (Input.GetButtonDown(FocusManager.Instance.TutorialKey))
                FocusManager.Instance.RequestTutorialOptional(focusPoint);
        }
    }

    #region Gizmos

    // --- Implementación de Gizmos para Visualización en el Editor ---
    private void OnDrawGizmos()
    {
        // --- Cálculo de Tamaños Escalados Correcto ---
        Vector3 scaledSizeA = Vector3.Scale(transform.lossyScale, sizeA);
        Vector3 scaledSizeB = Vector3.Scale(transform.lossyScale, sizeB);
        
        // Establecer la matriz del Gizmo basada en el transform (posición, rotación, escala)
        Gizmos.matrix = transform.localToWorldMatrix;
        
        // Dibuja el tamaño actual del collider (si existe)
        if (_boxCollider != null)
        {
            Gizmos.color = _isSizeA ? gizmoColorA : gizmoColorB;
            // El collider real siempre se dibuja usando su centro y tamaño actuales.
            Gizmos.DrawWireCube(_boxCollider.center, _boxCollider.size);
        }
        
        // Dibuja el tamaño A (Obligatorio)
        Gizmos.color = gizmoColorA;
        Color colorA = gizmoColorA;
        colorA.a = 0.3f;
        Gizmos.color = colorA;
        
        // Uso de centerOffsetA para la posición
        Gizmos.DrawCube(centerOffsetA, scaledSizeA); 
        
        Gizmos.color = gizmoColorA;
        
        // Uso de centerOffsetA para la posición
        Gizmos.DrawWireCube(centerOffsetA, scaledSizeA);


        // Dibuja el tamaño B (Opcional/Repetir)
        Gizmos.color = gizmoColorB;
        Color colorB = gizmoColorB;
        colorB.a = 0.15f;
        Gizmos.color = colorB;
        
        // Uso de centerOffsetB para la posición
        Gizmos.DrawCube(centerOffsetB, scaledSizeB);
        
        Gizmos.color = gizmoColorB;
        
        // Uso de centerOffsetB para la posición
        Gizmos.DrawWireCube(centerOffsetB, scaledSizeB);
    }

    #endregion
}