using UnityEngine;
using static Layers;
using static Animations.Player;
using static PlayerEnum;
using static SfxIDs;

public sealed class WalkState : State
{
    private readonly PlayerContext _ctx;

    // Distancia extra para detectar colisión antes de tocarla
    private const float CollisionBuffer = 0.1f;

    public WalkState(PlayerContext ctx)
    {
        _ctx = ctx;
    }
    
    public override void OnEnter()
    {
        _ctx.View.PlaySfx(Mummy___Normal.Walk);
        _ctx.View.Animator.SetBool(WALK, true);
        // Matamos la inercia física al entrar para tener control total inmediato
        _ctx.Rb.linearVelocity = Vector3.zero;
        
        GameEventManager.Instance.playerEvents.OnSizeChanged.Register<PlayerSize>(HandleSizeChanged);
    }

    public override void OnExit()
    {
        _ctx.View.StopSfx(Mummy___Normal.Walk);
        _ctx.View.Animator.SetBool(WALK, false);
        
        GameEventManager.Instance.playerEvents.OnSizeChanged.Unregister<PlayerSize>(HandleSizeChanged);
    }

    private void HandleSizeChanged(PlayerSize newSize)
    {
        // Cuando el tamaño cambia, reiniciamos el loop de audio.
        // Como la View actualiza su '_currentBank' en su propio OnSizeChanged,
        // este PlaySfx ya tomará el clip del banco nuevo.
        _ctx.View.StopSfx(Mummy___Normal.Walk);
        _ctx.View.PlaySfx(Mummy___Normal.Walk);
    }    
    
    public override void OnFixedUpdate()
    {
        Vector2 mv = _ctx.Input.Move;

        // Si no hay input, frenamos inmediatamente.
        if (mv.sqrMagnitude < 0.001f) return;

        Vector3 dir = _ctx.CameraRelativeDir(mv.x, mv.y);
        float baseMoveAmount = _ctx.MoveSpeed * Time.deltaTime;
        float currentMoveAmount = baseMoveAmount;

        // 1. Definir la Máscara de Capas (Layers)
        // Asegúrate de que las capas 'Wall' e 'Interactable' estén configuradas en Unity
        int layerMask = LayerMask.GetMask(WALL_LAYER, INTERACTABLE_LAYER);

        // 2. Lanzar el Raycast Predictivo
        // Usamos el Raycast para ver si golpearemos algo en la dirección 'dir'
        Vector3 rayStart = _ctx.Tf.position + Vector3.up * 0.5f;
        float rayLength = baseMoveAmount + CollisionBuffer;

        if (Physics.Raycast(rayStart, dir, out RaycastHit hit, rayLength, layerMask))
        {
            // 3. Colisión detectada: Reducir la velocidad bruscamente

            // Distancia restante hasta la colisión
            float distanceToHit = hit.distance;

            // Si la distancia a la colisión es menor que la cantidad de movimiento, 
            // necesitamos frenar. Reducimos el movimiento a un 10% del total.

            // 10% de la velocidad total
            float reducedSpeedFactor = 0.1f;

            // Calculamos el nuevo movimiento: usamos el 10% de la velocidad base, 
            // pero asegurándonos de que no traspasamos la colisión.
            currentMoveAmount = Mathf.Min(
                distanceToHit - 0.001f, // Mueve solo hasta justo antes de tocar
                baseMoveAmount * reducedSpeedFactor // Limitado al 10% de la velocidad máxima
            );

            // Si el resultado es muy pequeño (ya estamos casi tocando), podemos incluso ponerlo a cero
            if (currentMoveAmount < 0) currentMoveAmount = 0;

            // Opcional: Para una reducción suave en lugar de inmediata
            // float targetMoveAmount = baseMoveAmount * reducedSpeedFactor;
            // currentMoveAmount = Mathf.Lerp(currentMoveAmount, targetMoveAmount, Time.deltaTime * 10f);
        }

        // --- MOVIMIENTO ---
        // El movimiento se ejecuta con la velocidad modificada (o no)
        if (currentMoveAmount > 0)
        {
            _ctx.Tf.position += dir * currentMoveAmount;
        }

        // --- ROTACIÓN (No se afecta por la colisión) ---
        Quaternion targetRot = Quaternion.LookRotation(dir, Vector3.up);
        _ctx.Tf.rotation = Quaternion.Slerp(_ctx.Tf.rotation, targetRot, _ctx.TurnSpeed * Time.deltaTime);

        _ctx.View?.SetMoveSpeedVisual(1f);
    }

    public override void OnUpdate()
    {
        // Chequeo de suelo: Si deja de haber suelo, pasamos a Fall
        // (Necesario porque al mover por transform, el Rigidbody no cae solo a veces)
        if (!_ctx.IsGrounded())
        {
            StateMachine.ChangeState(PlayerStateId.Fall);
        }
    }
}