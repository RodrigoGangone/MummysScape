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
    
    private bool _isPromptActive; 

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

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("PlayerFather")) return;

        // SOLO lanzamos automáticamente si NO lo hemos visto nunca
        if (_isSizeA && FocusManager.Instance != null)
        {
            if (!Save.IsTutorialSeen(focusPoint.Id)) 
            {
                FocusManager.Instance.RequestTutorial(focusPoint);
            }
            // Expandimos el collider para pasar a modo "Opcional"
            SetColliderShape(false);
        }
    }

    // ... en OnTriggerStay (Interacción Manual / Opcional) ...
    private void OnTriggerStay(Collider other)
    {
        if (!other.CompareTag("PlayerFather")) return;
        if (_isSizeA) return; // Si estamos en fase A, ignorar

        // Mostrar UI si hace falta...
        if (!_isPromptActive)
            GameEventManager.Instance.levelEvents.OnTutorialPrompt.Raise(true, buttonType.Y);

        // Si presiona botón, pedimos el tutorial (El Manager sabe que ya es Visto, así que usará tiempo corto)
        if (FocusManager.Instance != null && Input.GetButtonDown(FocusManager.Instance.TutorialKey))
            FocusManager.Instance.RequestTutorial(focusPoint);
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