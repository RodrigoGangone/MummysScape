// InteractionRuntime.cs
using UnityEngine;

/// <summary>
/// InteractionRuntime
/// Responsabilidades:
/// - Resolver candidatos de interacción por tipo (Push/Attract/Swing) a partir del player.
/// - Para Push: usa un chequeo robusto con **doble Raycast** a media altura.
/// - Persiste la última consulta para dibujar Gizmos de debug en Scene.
/// Notas de diseño:
/// - No aplica reglas por tamaño; eso lo hace el Guard + SizeRules del Player.
/// - Mantiene la misma firma pública para no romper a los clientes (PlayerContext/Driver).
/// </summary>
public sealed class InteractionRuntime : MonoBehaviour
{
    [Header("Layers")]
    [SerializeField] private LayerMask _interactablesMask;

    [Header("Common Tuning")]
    [SerializeField, Range(5f, 45f)]   private float _maxFaceAngleDeg = 10f;
    
    // ------------------------
    // PUSH (doble raycast)
    // ------------------------
    [Header("Push (Double Ray)")]
    [Tooltip("Distancia máxima de los rayos frontales para detectar push.")]
    [SerializeField, Min(0.1f)] private float _pushRayLength = 1.5f;

    [Tooltip("Altura del rayo respecto al pivot del player (mitad de su altura aprox.).")]
    [SerializeField, Min(0f)] private float _pushRayYOffset = 0.9f;

    [Tooltip("Separación lateral de cada rayo respecto al centro del player (en metros).")]
    [SerializeField, Min(0f)] private float _pushRayHalfSpacing = 0.25f;

    [Header("Debug (Push Gizmos)")]
    [SerializeField] private bool _drawPushGizmos = true;
    [SerializeField] private Color _pushHitColor  = new(0.2f, 1f, 0.2f, 0.9f);
    [SerializeField] private Color _pushMissColor = new(1f, 0.2f, 0.2f, 0.6f);
    
    // Cache de última consulta (para Gizmos)
    private Vector3 _lastPushLeftOrigin, _lastPushRightOrigin, _lastPushDir;
    private float _lastPushLen;
    private bool _lastPushLHit, _lastPushRHit;
    private Vector3 _lastPushLPoint, _lastPushRPoint;

    // --- PUSH ---
    /// <summary>
    /// Doble Raycast frontal: ambos deben golpear al mismo interactuable con BoxPushAttract (IPushable).
    /// Si la cara es válida, el propio IPushable resuelve eje permitido y Snap (PushInfo).
    /// </summary>
    public bool TryFindPushable(Transform player, out IPushable push, out PushInfo info)
    {
        push = null; info = default;

        // Geometría de rayos
        Vector3 originCenter = player.position + Vector3.up * _pushRayYOffset;
        Vector3 right        = player.right;
        Vector3 fwd          = player.forward;

        Vector3 oL = originCenter - right * _pushRayHalfSpacing;
        Vector3 oR = originCenter + right * _pushRayHalfSpacing;

        // Persistimos para gizmos
        _lastPushLeftOrigin  = oL;
        _lastPushRightOrigin = oR;
        _lastPushDir         = fwd;
        _lastPushLen         = _pushRayLength;
        _lastPushLHit = _lastPushRHit = false;
        _lastPushLPoint = oL + fwd * _pushRayLength;
        _lastPushRPoint = oR + fwd * _pushRayLength;

        // Raycasts
        bool hitL = Physics.Raycast(oL, fwd, out var hL, _pushRayLength, _interactablesMask, QueryTriggerInteraction.Ignore);
        bool hitR = Physics.Raycast(oR, fwd, out var hR, _pushRayLength, _interactablesMask, QueryTriggerInteraction.Ignore);

        if (hitL) { _lastPushLHit = true; _lastPushLPoint = hL.point; }
        if (hitR) { _lastPushRHit = true; _lastPushRPoint = hR.point; }

        if (!(hitL && hitR)) return false;

        // Exigir que ambos rayos golpeen al MISMO interactuable y que tenga el script BoxPushAttract
        var boxL = hL.collider.GetComponentInParent<BoxPushAttract>();
        var boxR = hR.collider.GetComponentInParent<BoxPushAttract>();
        if (boxL == null || boxR == null || boxL != boxR) return false;

        // Debe implementar IPushable (contrato de push)
        push = boxL as IPushable;
        if (push == null) return false;

        // La caja valida cara/eje/snap (evita duplicar lógica acá)
        return push.TryGetPushInfo(player, _maxFaceAngleDeg, _pushRayLength, out info);
    }

    //TODO: PENDIENTE MODIFICAR LOS TRYFIND DE ATTRACK Y SWING
    //TODO: ELIMINAR ESTA VARIABLE FAKE
    private readonly int _frontRange = 1;
    // --- ATTRACT ---
    public bool TryFindAttractable(Transform player, out IAttractable a, out Vector3 axis)
    {
        a = null; axis = default;
        Vector3 origin = player.position + Vector3.up * 0.8f;
        if (!Physics.Raycast(origin, player.forward, out var hit, _frontRange, _interactablesMask, QueryTriggerInteraction.Ignore))
            return false;

        a = hit.collider.GetComponentInParent<IAttractable>();
        return a != null && a.CanAttract(player, _frontRange, out axis);
    }

    // --- SWING ---
    public bool TryFindSwingable(Transform player, out ISwingable s, out Vector3 attach)
    {
        s = null; attach = default;
        Vector3 origin = player.position + Vector3.up * 0.8f;
        if (!Physics.Raycast(origin, player.forward, out var hit, _frontRange, _interactablesMask, QueryTriggerInteraction.Ignore))
            return false;

        s = hit.collider.GetComponentInParent<ISwingable>();
        if (s == null) return false;

        // Vista despejada (sin paredes) ya la garantiza el raycast frontal.
        return s.CanSwing(player, _frontRange, out attach);
    }
    
    private void OnDrawGizmosSelected()
    {
        if (!_drawPushGizmos) return;

        // Izquierdo
        Gizmos.color = _lastPushLHit ? _pushHitColor : _pushMissColor;
        Gizmos.DrawLine(_lastPushLeftOrigin, _lastPushLeftOrigin + _lastPushDir * _lastPushLen);
        Gizmos.DrawSphere(_lastPushLeftOrigin, 0.03f);
        Gizmos.DrawSphere(_lastPushLPoint, 0.035f);

        // Derecho
        Gizmos.color = _lastPushRHit ? _pushHitColor : _pushMissColor;
        Gizmos.DrawLine(_lastPushRightOrigin, _lastPushRightOrigin + _lastPushDir * _lastPushLen);
        Gizmos.DrawSphere(_lastPushRightOrigin, 0.03f);
        Gizmos.DrawSphere(_lastPushRPoint, 0.035f);
    }
}