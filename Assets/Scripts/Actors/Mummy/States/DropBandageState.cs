using UnityEngine;

/// <summary> 
/// Estado de Soltar Bandage: Consume una venda del stock para instanciarla físicamente en el mundo, 
/// aplicando fuerzas de impulso y torque para simular un lanzamiento natural. 
/// </summary>

public sealed class DropBandageState : State, IBandageRestrictor
{
    private readonly PlayerContext _ctx;
    private readonly GameObject _bandagePickupPrefab;

    private readonly float _throwForce = 5f;
    private readonly float _upwardForce = 2f;

    public DropBandageState(PlayerContext ctx, GameObject bandagePickupPrefab)
    { 
        _ctx = ctx; 
        _bandagePickupPrefab = bandagePickupPrefab; 
    }

    public override void OnEnter()
    {
        if (!_ctx.Model.TryConsumeBandage())
            return;
        
        if (_bandagePickupPrefab != null)
            SpawnAndThrowBandage();
    }

    private void SpawnAndThrowBandage()
    {
        Vector3 spawnPos = _ctx.Rb.position + _ctx.Tf.forward * 0.5f + Vector3.up * 1.0f;
        
        GameObject bandage = Object.Instantiate(_bandagePickupPrefab, spawnPos, Random.rotation);

        Rigidbody bandageRb = bandage.GetComponent<Rigidbody>();

        if (bandageRb != null)
        {
            Vector3 forceDirection = (_ctx.Tf.forward * _throwForce) + (Vector3.up * _upwardForce);
            
            bandageRb.AddForce(forceDirection, ForceMode.Impulse);

            bandageRb.AddTorque(Random.insideUnitSphere * 10f, ForceMode.Impulse);
        }

        var pickupScript = bandage.GetComponent<Bandage>(); 
        
        if (pickupScript != null)
            pickupScript.SetupPickupDelay();
    }

    public override void OnUpdate() { }
    public override void OnFixedUpdate() { }
    public override void OnExit() { }
}