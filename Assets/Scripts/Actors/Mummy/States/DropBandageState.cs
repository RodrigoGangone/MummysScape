using UnityEngine;
using static PlayerEnum;

public sealed class DropBandageState : State
{
    private readonly PlayerContext _ctx;
    private readonly GameObject _bandagePickupPrefab;

    // Configuración del lanzamiento (podrías pasarlas por constructor si varían)
    private readonly float _throwForce = 5f;
    private readonly float _upwardForce = 2f;

    public DropBandageState(PlayerContext ctx, GameObject bandagePickupPrefab)
    { 
        _ctx = ctx; 
        _bandagePickupPrefab = bandagePickupPrefab; 
    }

    public override void OnEnter()
    {
        Debug.Log("DropBandage State");
        
        // 1. Chequeo de inventario
        if (!_ctx.Model.TryConsumeBandage())
        {
            return;
        }

        
        if (_bandagePickupPrefab != null)
        {
            SpawnAndThrowBandage();
        }
        
        // Asumiendo que esto es una acción instantánea, volvemos a Idle u otro estado
        // (Depende de tu máquina de estados, aquí no hago nada para dejar tu lógica original)
    }

    private void SpawnAndThrowBandage()
    {
        // Posición de salida: Un poco enfrente y arriba del jugador para evitar clipping inmediato
        Vector3 spawnPos = _ctx.Rb.position + _ctx.Tf.forward * 0.5f + Vector3.up * 1.0f;
        
        // Instanciamos con una rotación aleatoria para variedad visual
        GameObject bandage = Object.Instantiate(_bandagePickupPrefab, spawnPos, Random.rotation);

        // Lógica física (Rigidbody)
        Rigidbody bandageRb = bandage.GetComponent<Rigidbody>();

        if (bandageRb != null)
        {
            // Calculamos el vector de fuerza: Hacia adelante + Hacia arriba
            Vector3 forceDirection = (_ctx.Tf.forward * _throwForce) + (Vector3.up * _upwardForce);
            
            // Usamos Impulse porque es una fuerza instantánea (como un golpe o lanzamiento)
            bandageRb.AddForce(forceDirection, ForceMode.Impulse);

            // Agregamos un torque (giro) aleatorio para que la venda gire en el aire
            bandageRb.AddTorque(Random.insideUnitSphere * 10f, ForceMode.Impulse);
        }

        // SOLUCIÓN AL TODO:
        // Buscamos el script del pickup en el objeto instanciado e invocamos un retraso.
        // Asumo que tu script se llama 'BandagePickup' o similar.
        var pickupScript = bandage.GetComponent<Bandage>(); 
        
        if (pickupScript != null)
        {
            pickupScript.SetupPickupDelay(2.0f); 
        }
    }

    public override void OnUpdate() { }
    public override void OnFixedUpdate() { }
    public override void OnExit() { }
}