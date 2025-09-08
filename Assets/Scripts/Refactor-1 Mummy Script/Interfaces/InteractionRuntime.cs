// InteractionRuntime.cs
using UnityEngine;

/// <summary>
/// InteractionRuntime
/// Hace los queries de escena (raycast/capsule/overlap) y resuelve un único candidate por tipo.
/// No aplica reglas de tamaño: eso lo hace el Guard + SizeRules del Player.
/// </summary>
public sealed class InteractionRuntime : MonoBehaviour
{
    [Header("Layers")]
    [SerializeField] private LayerMask _interactablesMask;

    [Header("Tuning")]
    [SerializeField, Range(0.5f, 8f)]  private float _frontRange = 5f;
    [SerializeField, Range(5f, 45f)]   private float _maxFaceAngleDeg = 25f;
    [SerializeField, Range(0.05f, 0.5f)]private float _snapInset = 0.2f;

    // --- PUSH ---
    public bool TryFindPushable(Transform player, out IPushable push, out PushInfo info)
    {
        push = null; info = default;
        Vector3 origin = player.position + Vector3.up * 0.8f;
        Vector3 dir = player.forward;
        if (!Physics.Raycast(origin, dir, out var hit, _frontRange, _interactablesMask, QueryTriggerInteraction.Ignore))
            return false;

        push = hit.collider.GetComponentInParent<IPushable>();
        if (push == null) return false;

        // El propio objeto define si esa cara es válida y resuelve eje/snap.
        return push.TryGetPushInfo(player, _maxFaceAngleDeg, _frontRange, out info);
    }

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
}