using UnityEngine;
using static PlayerEnum;

/// <summary>
/// DropBandageState
/// Consume 1 venda del Model y (opcional) spawnea un pickup. Luego vuelve a Idle.
/// Permitido en Normal/Small (SizeRules).
/// </summary>
public sealed class DropBandageState : State
{
    private readonly PlayerContext _ctx;
    private readonly GameObject _bandagePickupPrefab; // opcional, puede ser null

    public DropBandageState(PlayerContext ctx, GameObject bandagePickupPrefab)
    { _ctx = ctx; _bandagePickupPrefab = bandagePickupPrefab; }

    public override void OnEnter()
    {
        Debug.Log("DropBandage!");
        
        // Si no hay vendas vuelvo a idle, sino las consumo
        if (!_ctx.Model.TryConsumeBandage())
        {
            Debug.Log("DropBandage! pero sin vendas");
            StateMachine.ChangeState(PlayerStateId.Idle); 
            return;
        }

        // Spawn del prefab venda
        if (_bandagePickupPrefab != null)
        {
            Vector3 pos = _ctx.Rb.position + _ctx.Tf.forward * 0.5f + Vector3.up * 0.2f;
            Quaternion rot = Quaternion.identity;
            Object.Instantiate(_bandagePickupPrefab, pos, rot);
        }

        StateMachine.ChangeState(PlayerStateId.Idle);
    }

    public override void OnUpdate() { }
    public override void OnFixedUpdate() { }
    public override void OnExit() { }
}
