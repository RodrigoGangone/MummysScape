using UnityEngine;

/// <summary> 
/// Estado de Retroceso: Aplica una fuerza física (Impulso) al jugador tras un impacto.
/// Respeta el tiempo de Stun inmovilizando al jugador mientras vuela y cae físicamente.
/// </summary>
public class KnockBackState : State, IBandageRestrictor
{
    private readonly PlayerContext _ctx;
    private readonly GameObject _bandagePrefab;
    
    private float _duration, _timer;
    private bool _isActive;

    // Fuerza de empuje ajustable. (Puedes mover esto al PlayerModel o pasarlo en el KnockbackData en el futuro)
    private readonly float _pushForce = 300f;

    public bool isActive => _isActive;
    
    public KnockBackState(PlayerContext ctx, GameObject bandagePrefab) 
    {
        _ctx = ctx;
        _bandagePrefab = bandagePrefab;
    }

    public override void OnEnter()
    {
        if (!_ctx.Observer.HasKnockback)
        {
            _isActive = false;
            return;
        }

        var data = _ctx.Observer.PeekKnockback();
        
        if (data.Duration <= 0f)
        {
            _ctx.Observer.ConsumeKnockback(); 
            _isActive = false;
            return;
        }

        // --- LÓGICA DE DROP DE VENDAS ---
        int stock = _ctx.Model.Bandages; 
        if (_bandagePrefab != null && stock > 0)
        {
            Vector3 spawnOrigin = _ctx.Tf.position + (Vector3.up * 1f); 

            for (int i = 0; i < stock; i++)
            {
                // Instanciamos en el centro sin offset para no quedar atrapados en paredes
                GameObject bandage = Object.Instantiate(_bandagePrefab, spawnOrigin, Random.rotation);
                bandage.GetComponent<Bandage>().SetupPickupDelay();
                
                if (bandage.TryGetComponent<Rigidbody>(out var rb))
                {
                    Vector3 explosionDir = Random.onUnitSphere;
                    explosionDir.y = Mathf.Abs(explosionDir.y) + 0.5f; 
                    rb.AddForce(explosionDir.normalized * 8f, ForceMode.Impulse); 
                }
            }
        }
        
        // --- PREPARACIÓN DEL ESTADO ---
        _duration = data.Duration;
        _timer = 0f;
        _isActive = true;
        
        GameEventManager.Instance.playerEvents.OnHit.Raise();
        _ctx.View._koFX.Play();

        // --- LÓGICA DE FÍSICAS (EL EMPUJÓN) ---
        _ctx.Rb.isKinematic = false; 
        _ctx.Rb.linearVelocity = Vector3.zero; // Limpiamos cualquier inercia previa (caminar, caer)

        // Calculamos la dirección plana usando el TargetPosition que nos mandó el proyectil
        Vector3 flatDirection = (data.TargetPosition - _ctx.Tf.position);
        flatDirection.y = 0f; // Aseguramos que sea puramente horizontal
        
        // Si por alguna razón el vector es 0 (ej. superposición perfecta), empujamos hacia atrás del jugador
        if (flatDirection.sqrMagnitude < 0.001f)
        {
            flatDirection = -_ctx.Tf.forward;
        }

        // Normalizamos y le agregamos una fuerte componente vertical (diagonal hacia arriba)
        Vector3 pushDirection = flatDirection.normalized;
        pushDirection.y = 1f; // Ángulo de ~45 grados hacia arriba
        
        // Aplicamos la fuerza física real en el espacio del mundo
        _ctx.Rb.AddForce(pushDirection.normalized * _pushForce, ForceMode.Impulse);
    }

    public override void OnUpdate()
    {
        if (!_isActive) return;

        // Ahora el Update SOLO se encarga de contar el tiempo de Stun.
        // Las colisiones y el movimiento en el aire las maneja Unity automáticamente.
        _timer += Time.deltaTime;

        if (_timer >= _duration) 
        {
            _ctx.Observer.ConsumeKnockback();
            _isActive = false;
        }
    }

    public override void OnFixedUpdate() { }

    public override void OnExit()
    {
        if (_ctx.Observer.HasKnockback)
        {
            _ctx.Observer.ConsumeKnockback();
        }

        _ctx.Rb.linearVelocity = Vector3.zero; // Frenamos en seco al salir del Stun para no patinar
        _isActive = false;
        
        _ctx.View._koFX.Stop();
    }
}