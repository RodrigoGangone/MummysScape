using System.Collections;
using UnityEngine;

/// <summary>
/// BandagePickup
/// Pickup de venda. Al entrar el Player, intenta sumar una venda al Model a través del Controller.
/// Requiere Collider como Trigger.
/// </summary>
public sealed class BandagePickup : MonoBehaviour
{
    [SerializeField] private Material flameMaterial;
    
    private const int AMOUNT = 1;
    
    private static readonly int IsActive = Shader.PropertyToID("_isActive");

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("PlayerFather")) return;
        var ctrl = other.GetComponentInParent<PlayerController>();
        if (ctrl != null && ctrl.TryCollectBandage(AMOUNT))
        {
            Destroy(gameObject);
        }
    }
    
    public void DisablePickupForSeconds(float duration)
    {
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.enabled = false; // Desactiva el collider
            StartCoroutine(EnableColliderRoutine(duration, col));
        }
    }

    private IEnumerator EnableColliderRoutine(float delay, Collider col)
    {
        yield return new WaitForSeconds(delay);
        col.enabled = true; // Reactiva el collider, ahora sí se puede agarrar
        flameMaterial.SetFloat(IsActive, 1);
    }
}
