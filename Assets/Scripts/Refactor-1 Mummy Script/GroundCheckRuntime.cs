using UnityEngine;

/// <summary>
/// GroundCheckRuntime
/// Servicio de chequeo de suelo vía raycast corto.
/// </summary>
public sealed class GroundCheckRuntime : MonoBehaviour
{
    [SerializeField] private LayerMask _groundMask = ~0;
    [SerializeField] private float _rayDistance = 0.2f;
    [SerializeField] private Vector3 _originOffset = new Vector3(0f, 0.1f, 0f);

    public bool IsGrounded(Transform tf)
    {
        var origin = tf.position + _originOffset;
        return Physics.Raycast(origin, Vector3.down, _rayDistance, _groundMask, QueryTriggerInteraction.Ignore);
    }
}