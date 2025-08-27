using UnityEngine;

/// <summary>
/// BandagePickup
/// Pickup de venda. Al entrar el Player, intenta sumar una venda al Model a través del Controller.
/// Requiere Collider como Trigger.
/// </summary>
public sealed class BandagePickup : MonoBehaviour
{
    [SerializeField] private int _amount = 1;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("PlayerFather")) return;
        var ctrl = other.GetComponentInParent<PlayerController>();
        if (ctrl != null && ctrl.TryCollectBandage(_amount))
        {
            Destroy(gameObject);
        }
    }
}
