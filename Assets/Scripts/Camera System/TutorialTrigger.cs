using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class TutorialTrigger : MonoBehaviour
{
    [Header("Referencias")] 
    [SerializeField] private TutorialFocusPoint focusPoint;

    [Header("Configuración de Áreas")] 
    [SerializeField] private Vector3 sizeA = Vector3.one;
    [SerializeField] private Vector3 sizeB = Vector3.one * 2f;
    [SerializeField] private Vector3 centerOffsetA = Vector3.zero;
    [SerializeField] private Vector3 centerOffsetB = Vector3.zero;

    [Header("Debug")]
    [SerializeField] private Color gizmoColorA = Color.green;
    [SerializeField] private Color gizmoColorB = Color.yellow;
    
    private BoxCollider _boxCollider;
    private bool _isSizeA = true;
    
    // Bandera para evitar disparar el evento GameEvent todo el tiempo
    private bool _isPromptActive = false; 

    private void Awake()
    {
        _boxCollider = GetComponent<BoxCollider>();
        SetColliderShape(true);
    }

    private void SetColliderShape(bool useSizeA)
    {
        _isSizeA = useSizeA;
        _boxCollider.size = useSizeA ? sizeA : sizeB;
        _boxCollider.center = useSizeA ? centerOffsetA : centerOffsetB;
    }

    // Usamos Enter SOLO para la lógica de "Primera Vez" (Size A)
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("PlayerFather")) return;

        if (_isSizeA && FocusManager.Instance != null)
        {
            FocusManager.Instance.RequestTutorialFirstTime(focusPoint);
            
            // Expandimos el collider. Al hacerlo, el jugador ya está dentro,
            // así que OnTriggerStay tomará el relevo inmediatamente.
            SetColliderShape(false);
        }
    }

    // Usamos Stay para mantener la UI encendida (Size B)
    // Esto arregla el bug: si el resize ocurre, Stay sigue validando que estás dentro.
    private void OnTriggerStay(Collider other)
    {
        if (!other.CompareTag("PlayerFather")) return;

        // Solo nos importa la lógica de UI en la fase B
        if (!_isSizeA)
        {
            // 1. Encender la UI si no está encendida
            if (!_isPromptActive)
            {
                GameEventManager.Instance.levelEvents.OnTutorialPrompt.Raise(true, buttonType.Y);
                _isPromptActive = true;
            }

            // 2. Detectar el Input (Interacción)
            // Es seguro hacerlo aquí o en Update, pero aquí garantizamos que sea mientras colisiona
            if (FocusManager.Instance != null && Input.GetButtonDown(FocusManager.Instance.TutorialKey))
            {
                FocusManager.Instance.RequestTutorialOptional(focusPoint);
            }
        }
    }

    // Usamos Exit para apagar la UI
    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("PlayerFather")) return;

        // Si salimos y la UI estaba activa, la apagamos
        if (_isPromptActive)
        {
            GameEventManager.Instance.levelEvents.OnTutorialPrompt.Raise(false, buttonType.Y);
            _isPromptActive = false;
        }
    }
    
    // NOTA: He quitado el Update. 
    // Al usar OnTriggerStay para el Input, nos ahorramos un ciclo Update innecesario 
    // y aseguramos que solo puedas interactuar si la física te detecta.

    #region Gizmos
    private void OnDrawGizmos()
    {
        Gizmos.matrix = transform.localToWorldMatrix;
        
        // Dibujo Size A
        Gizmos.color = _isSizeA ? gizmoColorA : new Color(gizmoColorA.r, gizmoColorA.g, gizmoColorA.b, 0.1f);
        Gizmos.DrawWireCube(centerOffsetA, sizeA);

        // Dibujo Size B
        Gizmos.color = !_isSizeA ? gizmoColorB : new Color(gizmoColorB.r, gizmoColorB.g, gizmoColorB.b, 0.1f);
        Gizmos.DrawWireCube(centerOffsetB, sizeB);
    }
    #endregion
}