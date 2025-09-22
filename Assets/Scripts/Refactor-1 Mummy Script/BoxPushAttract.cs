using UnityEngine;

/// <summary>
/// BoxPushAttract
/// Implementa IPushable para una caja (RB + BoxCollider) alineada a los ejes locales del prefab.
/// - Decide el eje permitido según la CARA donde empuja el player: cara ±X => eje local Z; cara ±Z => eje local X.
/// - Mueve la caja sólo sobre ese eje usando Rigidbody.MovePosition y bloquea con Physics.BoxCast (skin configurable).
/// - Entrega SnapPoint = centro horizontal de la cara, para centrar al player (soft-snap) mientras dura el push.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(BoxCollider))]
public sealed class BoxPushAttract : MonoBehaviour
{
    [Header("Refs (Prefab)")]
    [SerializeField] private Rigidbody _rigidbody;
    [SerializeField] private BoxCollider _mainCollider;

    /// <summary>
    /// Retorna el centro del cuerpo proyectado en el plano horizontal del mundo.
    /// Se utiliza para alinear al jugador hacia el volumen principal durante el push.
    /// </summary>
    public Vector3 HorizontalBodyCenter
    {
        get
        {
            Vector3 fallback = transform.position;
            if (_mainCollider == null)
            {
                return new Vector3(fallback.x, fallback.y, fallback.z);
            }

            Vector3 center = _mainCollider.bounds.center;
            center.y = fallback.y;
            return center;
        }
    }

    private void Reset()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _mainCollider = GetComponent<BoxCollider>();
    }

    private void Awake()
    {
        if (_rigidbody == null)
        {
            _rigidbody = GetComponent<Rigidbody>();
        }

        if (_mainCollider == null)
        {
            _mainCollider = GetComponent<BoxCollider>();
        }
    }
}
