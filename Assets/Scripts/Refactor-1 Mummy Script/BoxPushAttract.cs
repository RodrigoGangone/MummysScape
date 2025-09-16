using UnityEngine;

/// <summary>
/// BoxPushAttract
/// Caja empujable/atraíble: se mueve sólo en X o Z (no diagonal).
/// - Determina la cara válida (dot + ángulo) y el eje permitido.
/// - Durante Push: congela el eje ortogonal y aplica velocidad sobre el eje permitido.
/// - Durante Attract: se acerca al player sobre el eje permitido.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public sealed class BoxPushAttract : MonoBehaviour, IPushable, IAttractable
{
    [Header("Setup")] [SerializeField] private float _halfExtent;
    [SerializeField] private ParticleSystem _canFX; // verde
    [SerializeField] private ParticleSystem _sizeFX; // rojo por tamaño (opcional, se activa sólo si falla por Size)

    [Header("Tuning")] [SerializeField, Min(0f)]
    private float _pushSpeed = 4.0f;

    [SerializeField, Min(0f)] private float _attractSpeed = 4.0f;

    private Rigidbody _rb;
    private Object _owner; // lock
    private RigidbodyConstraints _defaultConstraints;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.interpolation = RigidbodyInterpolation.Interpolate;
        _defaultConstraints = _rb.constraints;
    }

    #region IExclusiveInteractable

    public bool IsBusy => _owner != null;

    public bool TryAcquire(Object user)
    {
        if (IsBusy) return false;
        _owner = user;
        return true;
    }

    public void Release(Object user)
    {
        if (_owner == user) _owner = null;
    }

    #endregion

    #region IPushable
    
    void OnValidate() {
        if (TryGetComponent(out BoxCollider box)) {
            // mitad del tamaño en el eje de la cara; si tus caras son ±X/±Z, tomá el mayor
            _halfExtent = Mathf.Max(box.size.x, box.size.z) * 0.5f;
        }
    }

    public bool TryGetPushInfo(Transform player, float maxFaceAngleDeg, float maxDist, out PushInfo info)
    {
        info = default;

        // Vector desde centro de la caja al jugador (en mundo)
        Vector3 toPlayer = (player.position - transform.position);
        toPlayer.y = 0f;
        if (toPlayer.sqrMagnitude > maxDist * maxDist) return false;

        // Ejes locales (mundo) de la caja
        Vector3 right = transform.right;
        right.y = 0f;
        right.Normalize();
        Vector3 fwd = transform.forward;
        fwd.y = 0f;
        fwd.Normalize();

        // Elegimos la cara más "frontal" al player
        float dotX = Vector3.Dot(toPlayer.normalized, right);
        float dotZ = Vector3.Dot(toPlayer.normalized, fwd);

        // Cara más cercana
        Vector3 faceNormal;
        if (Mathf.Abs(dotX) > Mathf.Abs(dotZ))
            faceNormal = Mathf.Sign(dotX) > 0 ? right : -right; // caras ±X
        else
            faceNormal = Mathf.Sign(dotZ) > 0 ? fwd : -fwd; // caras ±Z

        // *** MOVEMOS EN LA NORMAL DE LA CARA (X o Z), no ortogonal ***
        Vector3 moveAxis = faceNormal;

        // Ángulo máximo permitido entre mirada del player y la cara
        float angle = Vector3.Angle(player.forward, faceNormal * -1f);
        if (angle > maxFaceAngleDeg) return false;

        // Punto de snap sobre el plano de la cara (a ras)
        Vector3 snap = transform.position + faceNormal * (_halfExtent + 0.15f);

        info = new PushInfo(faceNormal, moveAxis, snap);
        return true;
    }

    public void OnPushStart(in PushInfo info)
    {
        // Congelar eje ortogonal: si movés en X, congelá Z; si movés en Z, congelá X
        bool moveIsX = Mathf.Abs(Vector3.Dot(info.Axis, Vector3.right)) > 0.5f;
        var freeze = _defaultConstraints |
                     (moveIsX ? RigidbodyConstraints.FreezePositionZ : RigidbodyConstraints.FreezePositionX);
        _rb.constraints = freeze;
    }

    public void OnPushUpdate(in PushInfo info, float signedInput01, float speed)
    {
        // signedInput01: +1 empuja en +Axis, -1 en -Axis, 0 frena.
        Vector3 vel = info.Axis * (signedInput01 * speed);
        vel.y = _rb.velocity.y;
        _rb.velocity = vel;
    }

    public void OnPushEnd()
    {
        _rb.constraints = _defaultConstraints;
        // frenar suavemente
        Vector3 v = _rb.velocity;
        v.x *= 0.1f;
        v.z *= 0.1f;
        _rb.velocity = v;
    }

    #endregion

    #region IAttractable

    public bool CanAttract(Transform player, float maxDist, out Vector3 allowedAxis)
    {
        allowedAxis = default;
        // Reusar mismo análisis de caras/eje de TryGetPushInfo pero sin chequear ángulo estricto
        if (!TryGetPushInfo(player, 90f, maxDist, out var info)) return false;
        allowedAxis = info.Axis;
        return true;
    }

    public void OnAttractStart()
    {
        /* opcional fx */
    }

    public void OnAttractUpdate(Vector3 playerPos, Vector3 allowedAxis, float speed)
    {
        // Proyectar vector hacia el player sobre el eje permitido
        Vector3 toPlayer = playerPos - _rb.position;
        toPlayer.y = 0f;
        float signed = Mathf.Sign(Vector3.Dot(toPlayer, allowedAxis));
        Vector3 vel = allowedAxis * (signed * speed);
        vel.y = _rb.velocity.y;
        _rb.velocity = vel;
    }

    public void OnAttractEnd()
    {
        // nada especial; colisiones de Unity frenan contra paredes
    }

    #endregion

    #region Visual helpers

    // Llamá a estos desde un HighlightSystem si querés:
    public void SetHoverCan(bool can)
    {
        if (_canFX)
        {
            if (can && !_canFX.isPlaying) _canFX.Play();
            else if (!can && _canFX.isPlaying) _canFX.Stop();
        }
    }

    public void SetHoverBlockedBySize(bool blocked)
    {
        if (_sizeFX)
        {
            if (blocked && !_sizeFX.isPlaying) _sizeFX.Play();
            else if (!blocked && _sizeFX.isPlaying) _sizeFX.Stop();
        }
    }

    #endregion
}