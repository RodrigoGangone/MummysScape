using UnityEngine;

//// <summary>
//// BoxPushAttract
//// - Dinámica siempre (cae con gravedad).
//// - Fuera de Push: FreezePositionX | FreezePositionZ para que no se mueva por colisión.
//// - En Push: solo FreezeRotation (se libera XZ y se mueve por código).
//// </summary>
[RequireComponent(typeof(Rigidbody), typeof(BoxCollider))]
public sealed class BoxPushAttract : MonoBehaviour
{
    [SerializeField] private PhysicMaterial _materialBajaFriccion;

    private Rigidbody _rb;

    // Congelada en XZ cuando no empujamos; libre en XZ durante push.
    private static readonly RigidbodyConstraints IdleConstraints =
        RigidbodyConstraints.FreezeRotation |
        RigidbodyConstraints.FreezePositionX |
        RigidbodyConstraints.FreezePositionZ;

    private static readonly RigidbodyConstraints PushConstraints =
        RigidbodyConstraints.FreezeRotation;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();

        if (TryGetComponent<BoxCollider>(out var col) && _materialBajaFriccion)
            col.sharedMaterial = _materialBajaFriccion;

        _rb.useGravity = true;
        _rb.isKinematic = false; // dinámica para que pueda caer
        _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        _rb.interpolation = RigidbodyInterpolation.Interpolate;

        // ⬇️ Al iniciar NO estamos empujando, por eso congelamos XZ
        _rb.constraints = IdleConstraints;
    }

    /// <summary>Habilita/deshabilita el modo push (libera o congela XZ).</summary>
    public void SetPushMode(bool enabled)
    {
        _rb.constraints = enabled ? PushConstraints : IdleConstraints;

        if (!enabled)
        {
            // por si venía con inercia horizontal
            var v = _rb.velocity;
            v.x = 0f; v.z = 0f;
            _rb.velocity = v;
        }
    }

    /// <summary>Mueve la caja por delta en XZ (solo tiene efecto en modo push).</summary>
    public void MoveBy(in Vector3 deltaWorld)
    {
        if ((_rb.constraints & (RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionZ)) != 0)
            return; // fuera de Push, ignoramos
        _rb.MovePosition(_rb.position + new Vector3(deltaWorld.x, 0f, deltaWorld.z));
    }

    public void StopImmediate() => _rb.velocity = Vector3.zero;
}