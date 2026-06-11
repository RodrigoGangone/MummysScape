using System.Collections;
using UnityEngine;
using static Tags;
using static PauseUtils;

/// <summary> 
/// Ítem Recolectable (Venda): Gestiona el ciclo de vida del recurso principal, incorporando 
/// retrasos de recolección pausables y relocalización inmediata basada en puntos de anclaje hijos.
/// </summary>
public class Bandage : MonoBehaviour, IPausable
{
    [Header("Visuals")] 
    [SerializeField] private Renderer meshRenderer;

    [Header("Restricted Area Settings")]
    [Tooltip("Capa de las zonas donde la venda no puede quedar atrapada (ej. restrictBandage).")]
    [SerializeField] private LayerMask restrictLayer;

    private Collider _collider;
    private bool _paused;
    private Material _instancedMaterial;
    private Breakable _sourceJar;

    private const int AMOUNT = 1;
    private static readonly int IsActive = Shader.PropertyToID("_isActive");

    // Array pre-alocado de tamaño 1 para evitar Garbage Collection por completo
    private readonly Collider[] _overlapResults = new Collider[1];

    private void Awake()
    {
        _collider = GetComponent<Collider>();
        if (meshRenderer != null) _instancedMaterial = meshRenderer.material;
    }

    public void SetupPickupDelay(float duration = 2, Breakable source = null)
    {
        _sourceJar = source;

        // 1. Validar superposición y reubicar en el nodo hijo más cercano si es necesario
        ResolveRestrictedOverlap();

        // 2. Continuar con la lógica de inicialización temporal
        if (_collider != null) _collider.enabled = false;
        if (_instancedMaterial != null) _instancedMaterial.SetFloat(IsActive, 0);

        StopAllCoroutines();
        StartCoroutine(EnableColliderRoutine(duration));
    }

    /// <summary>
    /// Detecta si la venda apareció dentro de un volumen restringido y la mueve al 
    /// punto de escape predefinido (hijo) más cercano.
    /// </summary>
    private void ResolveRestrictedOverlap()
    {
        if (_collider == null) return;

        // Comprobación rápida por volumen (AABB) sin alocación de memoria
        int hits = Physics.OverlapBoxNonAlloc(
            transform.position, 
            _collider.bounds.extents, 
            _overlapResults, 
            transform.rotation, 
            restrictLayer
        );

        if (hits > 0)
        {
            Transform restrictedTransform = _overlapResults[0].transform;

            // Aseguramos que el objeto tenga al menos los 2 hijos requeridos
            if (restrictedTransform.childCount >= 2)
            {
                Transform child1 = restrictedTransform.GetChild(0);
                Transform child2 = restrictedTransform.GetChild(1);

                // Calculamos la distancia al cuadrado (es mucho más rápido porque no procesa raíces)
                float sqrDist1 = (transform.position - child1.position).sqrMagnitude;
                float sqrDist2 = (transform.position - child2.position).sqrMagnitude;

                // Teletransportamos instantáneamente la venda al punto más cercano
                transform.position = (sqrDist1 < sqrDist2) ? child1.position : child2.position;
            }
            else
            {
                Debug.LogWarning($"El objeto restringido {restrictedTransform.name} no tiene suficientes puntos hijos configurados.", restrictedTransform);
            }
        }
    }

    private IEnumerator EnableColliderRoutine(float duration)
    {
        yield return WaitForSecondsPausable(duration, () => _paused);
        if (_collider != null) _collider.enabled = true;
        if (_instancedMaterial != null) _instancedMaterial.SetFloat(IsActive, 1);
    }

    private void OnTriggerStay(Collider collision)
    {
        if (!collision.gameObject.CompareTag(PLAYER_TAG)) return;
        var ctrl = collision.gameObject.GetComponentInParent<PlayerController>();
        if (ctrl != null && ctrl.TryCollectBandage(AMOUNT)) Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (_sourceJar != null) _sourceJar.NotifyItemPickedUp();
    }

    public void OnPauseChanged(bool paused) => _paused = paused;

    private void OnEnable() => GameEventManager.Instance.levelEvents.OnRequestBandageSpawn.Raise(gameObject, true);

    private void OnDisable()
    {
        if (_sourceJar != null) _sourceJar.NotifyItemPickedUp();
        GameEventManager.Instance.levelEvents.OnRequestBandageSpawn.Raise(gameObject, false);
    }
}