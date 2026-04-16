using UnityEngine;
using static Layers;
using static Animations.Player; // Asegúrate que aquí esté definido TERRAIN_TYPE
using static PlayerEnum;
using static SfxIDs;

public sealed class WalkState : State
{
    private readonly PlayerContext _ctx;
    private const float CollisionBuffer = 0.1f;

    private GroundCheckRuntime.TerrainType _lastTerrain;

    public WalkState(PlayerContext ctx) => _ctx = ctx;
    
    public override void OnEnter()
    {
        // Forzamos detección inicial
        _ctx.IsGrounded();
        _lastTerrain = _ctx.CurrentTerrain;
        
        // 1. Audio
        PlayTerrainSfx(_lastTerrain);
        
        // 2. Animación: Activamos caminar y seteamos el tipo de terreno
        _ctx.View.Animator.SetBool(WALK, true);
        _ctx.View.Animator.SetFloat(TERRAIN_TYPE, (float)_lastTerrain);
        
        _ctx.Rb.linearVelocity = Vector3.zero;
        
        GameEventManager.Instance.playerEvents.OnSizeChanged.Register<PlayerSize>(HandleSizeChanged);
    }

    public override void OnExit()
    {
        StopCurrentSfx();
        _ctx.View.Animator.SetBool(WALK, false);
        // Reset del terreno al salir para evitar comportamientos extraños en otros estados
        _ctx.View.Animator.SetFloat(TERRAIN_TYPE, 0); 

        GameEventManager.Instance.playerEvents.OnSizeChanged.Unregister<PlayerSize>(HandleSizeChanged);
    }

    private void HandleSizeChanged(PlayerSize newSize)
    {
        // Al cambiar de tamaño (y por ende de Animator), refrescamos los parámetros
        StopCurrentSfx();
        PlayTerrainSfx(_ctx.CurrentTerrain);
        
        // El nuevo animator necesita saber en qué terreno estamos
        _ctx.View.Animator.SetBool(WALK, true);
        _ctx.View.Animator.SetInteger(TERRAIN_TYPE, (int)_ctx.CurrentTerrain);
    }    
    
    public override void OnFixedUpdate()
    {
        Vector2 mv = _ctx.Input.Move;
        if (mv.sqrMagnitude < 0.001f) return;

        Vector3 dir = _ctx.CameraRelativeDir(mv.x, mv.y);
        
        // Modificadores físicos por terreno
        float terrainModifier = _ctx.CurrentTerrain switch {
            GroundCheckRuntime.TerrainType.Sand => 0.75f, // Más pesado
            _ => 1f
        };

        float baseMoveAmount = (_ctx.MoveSpeed * terrainModifier) * Time.deltaTime;
        float currentMoveAmount = baseMoveAmount;

        // Raycast Predictivo de colisiones
        int layerMask = LayerMask.GetMask(WALL_LAYER, INTERACTABLE_LAYER);
        Vector3 rayStart = _ctx.Tf.position + Vector3.up * 0.5f;
        float rayLength = baseMoveAmount + CollisionBuffer;

        if (Physics.Raycast(rayStart, dir, out RaycastHit hit, rayLength, layerMask))
        {
            currentMoveAmount = Mathf.Min(hit.distance - 0.001f, baseMoveAmount * 0.1f);
            if (currentMoveAmount < 0) currentMoveAmount = 0;
        }

        if (currentMoveAmount > 0)
        {
            _ctx.Tf.position += dir * currentMoveAmount;
        }

        Quaternion targetRot = Quaternion.LookRotation(dir, Vector3.up);
        _ctx.Tf.rotation = Quaternion.Slerp(_ctx.Tf.rotation, targetRot, _ctx.TurnSpeed * Time.deltaTime);

        // Ajustamos la velocidad de reproducción de la animación (opcional pero recomendado)
        _ctx.View?.SetMoveSpeedVisual(terrainModifier);
    }

    public override void OnUpdate()
    {
        if (!_ctx.IsGrounded())
        {
            StateMachine.ChangeState(PlayerStateId.Fall);
            return;
        }

        if (_ctx.CurrentTerrain != _lastTerrain)
        {
            _lastTerrain = _ctx.CurrentTerrain;
            StopCurrentSfx();
            PlayTerrainSfx(_lastTerrain);
        }
        
        _ctx.View.Animator.SetFloat(TERRAIN_TYPE, (float)_lastTerrain, 0.15f, Time.deltaTime);
    }

    private void PlayTerrainSfx(GroundCheckRuntime.TerrainType terrain)
    {
        var sfxId = terrain switch {
            GroundCheckRuntime.TerrainType.Sand => Mummy___Normal.WalkSand,
            _ => Mummy___Normal.Walk 
        };
        
        _ctx.View.PlaySfx(sfxId);
    }

    private void StopCurrentSfx()
    {
        _ctx.View.StopSfx(Mummy___Normal.Walk);
        _ctx.View.StopSfx(Mummy___Normal.WalkSand);
    }
}