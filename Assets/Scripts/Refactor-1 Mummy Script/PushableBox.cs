using UnityEngine;

/// <summary>
/// PushableBox
/// Caja empujable y atraíble. Restringe movimiento a X o Z cuando el player colisiona,
/// y expone PullTowards para ser atraída. Requiere Rigidbody.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public sealed class PushableBox : MonoBehaviour, IPushable, IAttractable
{
    private Rigidbody _rb;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ | RigidbodyConstraints.FreezeRotationY;
        _rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    // --- IPushable ---
    public void LockAxisX()
    {
        _rb.constraints = RigidbodyConstraints.FreezePositionZ
                        | RigidbodyConstraints.FreezeRotationX
                        | RigidbodyConstraints.FreezeRotationY
                        | RigidbodyConstraints.FreezeRotationZ;
    }

    public void LockAxisZ()
    {
        _rb.constraints = RigidbodyConstraints.FreezePositionX
                        | RigidbodyConstraints.FreezeRotationX
                        | RigidbodyConstraints.FreezeRotationY
                        | RigidbodyConstraints.FreezeRotationZ;
    }

    public void UnlockAxes()
    {
        _rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezeRotationZ;
    }

    // --- Colisiones con el Player para decidir eje ---
    private void OnCollisionEnter(Collision other)
    {
        if (!other.collider.CompareTag("PlayerFather")) return;

        Vector3 delta = transform.position - other.transform.position;
        // Elegimos eje dominante: si la diferencia en X es mayor, permitimos X; si no, Z.
        if (Mathf.Abs(delta.x) > Mathf.Abs(delta.z)) LockAxisX();
        else LockAxisZ();
    }

    private void OnCollisionExit(Collision other)
    {
        if (!other.collider.CompareTag("PlayerFather")) return;
        UnlockAxes();
    }

    // --- IAttractable ---
    public bool PullTowards(Vector3 targetPosition, float strength, float maxSpeed)
    {
        Vector3 toTarget = targetPosition - _rb.position;
        toTarget.y = 0f;
        float dist = toTarget.magnitude;
        if (dist < 0.001f) return false;

        Vector3 dir = toTarget / dist;

        // Aceleración controlada
        _rb.AddForce(dir * strength, ForceMode.Acceleration);

        // Clamp de velocidad en plano XZ
        Vector3 vel = _rb.velocity; vel.y = 0f;
        if (vel.magnitude > maxSpeed)
        {
            vel = vel.normalized * maxSpeed;
            _rb.velocity = new Vector3(vel.x, _rb.velocity.y, vel.z);
        }
        return true;
    }
}
