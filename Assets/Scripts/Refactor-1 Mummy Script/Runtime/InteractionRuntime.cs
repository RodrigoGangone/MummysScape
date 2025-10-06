using UnityEngine;

//// <summary>
//// InteractionRuntime
//// PushChecker: 2 raycast frontales a media altura, separados por _separation.
//// - Devuelve true si AMBOS rayos golpean el MISMO objeto en la capa Interactable
////   y dicho objeto (o su root) tiene BoxPushAttract.
//// - OnDrawGizmos: cada rayo en VERDE si colisiona con capa Interactable, ROJO si no.
//// - Todo tunable por Inspector (altura, separación, distancia y LayerMask).
//// </summary>
[DisallowMultipleComponent]
public sealed class InteractionRuntime : MonoBehaviour
{
    [Header("Push Checker")] [SerializeField, Tooltip("Altura de los rayos respecto al pivot del player.")]
    private float _heightY = 1.0f;

    [SerializeField, Tooltip("Distancia de los rayos hacia adelante.")]
    private float _distance = 1.0f;

    [SerializeField, Tooltip("Separación total entre rayos (centro a centro).")]
    private float _separation = 0.5f;

    [SerializeField, Tooltip("Capa Interactable (solo se castea contra este LayerMask).")]
    private LayerMask _interactMask;

    [Header("Swing Checker")] [SerializeField, Tooltip("")]
    private Vector3 halfExtents = new(1, 1, 1);

    [SerializeField, Tooltip("")] private Vector3 direction = Vector3.forward;

    [SerializeField, Tooltip("")] private Vector3 origin;

    [Header("Debug")] [SerializeField] private bool _drawGizmos = true;
    [SerializeField] private Color _hitColor = new(0.2f, 1f, 0.2f, 0.9f);
    [SerializeField] private Color _missColor = new(1f, 0.2f, 0.2f, 0.9f);

    // cache último chequeo para gizmos
    private Vector3 _oLeft, _oRight, _dLeft, _dRight;
    private bool _leftHitInteract, _rightHitInteract;
    private Vector3 _leftHitPoint, _rightHitPoint;

    /// <summary>
    /// Chequea si hay una caja empujable enfrente del player (doble rayo).
    /// </summary>
    public bool TryGetPushTarget(Transform playerTf, out BoxPushAttract target, out RaycastHit hitLeft,
        out RaycastHit hitRight)
    {
        target = null;
        hitLeft = default;
        hitRight = default;

        var fwd = playerTf.forward;
        var right = playerTf.right;
        var center = playerTf.position + Vector3.up * _heightY;
        float half = _separation * 0.5f;

        _oLeft = center - right * half;
        _oRight = center + right * half;
        _dLeft = _dRight = fwd;

        bool lHit = Physics.Raycast(_oLeft, fwd, out hitLeft, _distance, _interactMask, QueryTriggerInteraction.Ignore);
        bool rHit = Physics.Raycast(_oRight, fwd, out hitRight, _distance, _interactMask,
            QueryTriggerInteraction.Ignore);

        _leftHitInteract = lHit;
        _rightHitInteract = rHit;
        _leftHitPoint = lHit ? hitLeft.point : _oLeft + fwd * _distance;
        _rightHitPoint = rHit ? hitRight.point : _oRight + fwd * _distance;

        if (!(lHit && rHit)) return false;

        // ¿Mismo objeto raíz y con BoxPushAttract?
        Transform a = hitLeft.collider.transform.root;
        Transform b = hitRight.collider.transform.root;
        if (a != b) return false;

        target = a.GetComponentInChildren<BoxPushAttract>();
        if (target == null) return false;

        // ⬇️ NO se puede empujar si la caja no está soportada por el suelo permitido
        return target.IsGroundedForPush();
    }

    public bool TryGetSwingTarget(Transform playerTf, out Rigidbody target)
    {
        target = null;
        Vector3 center = playerTf.position + origin; // offset definido por inspector
        Collider[] hits = Physics.OverlapBox(center, halfExtents, playerTf.rotation, _interactMask);

        float minDist = float.MaxValue;
        Rigidbody nearest = null;

        foreach (var hit in hits)
        {
            if (!hit.CompareTag("Hook")) continue;
            Rigidbody rb = hit.attachedRigidbody;
            if (rb == null) continue;

            float dist = Vector3.Distance(playerTf.position, rb.position);
            if (dist < minDist)
            {
                minDist = dist;
                nearest = rb;
            }
        }

        if (nearest == null) return false;

        Debug.Log("TrySwing - true");

        target = nearest;
        return true;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (!_drawGizmos) return;

        Transform playerTf = transform;

        // --- Área de detección ---
        Vector3 center = playerTf.position + origin;
        Quaternion rot = playerTf.rotation;

        Collider[] hits = Physics.OverlapBox(center, halfExtents, rot, _interactMask);

        Rigidbody nearest = null;
        float minDist = float.MaxValue;

        foreach (var hit in hits)
        {
            if (!hit.CompareTag("Hook")) continue;
            var rb = hit.attachedRigidbody;
            if (rb == null) continue;

            float dist = Vector3.Distance(playerTf.position, rb.position);
            if (dist < minDist)
            {
                minDist = dist;
                nearest = rb;
            }
        }

        // Dibuja el área
        Gizmos.color = nearest ? _hitColor : _missColor;
        Gizmos.matrix = Matrix4x4.TRS(center, rot, Vector3.one);
        Gizmos.DrawWireCube(Vector3.zero, halfExtents * 2);
    }
#endif


    private void OnDrawGizmos()
    {
        if (!_drawGizmos) return;

        // Left
        Gizmos.color = _leftHitInteract ? _hitColor : _missColor;
        Gizmos.DrawLine(_oLeft, _oLeft + _dLeft * _distance);
        Gizmos.DrawSphere(_leftHitPoint, 0.03f);

        // Right
        Gizmos.color = _rightHitInteract ? _hitColor : _missColor;
        Gizmos.DrawLine(_oRight, _oRight + _dRight * _distance);
        Gizmos.DrawSphere(_rightHitPoint, 0.03f);
    }
}