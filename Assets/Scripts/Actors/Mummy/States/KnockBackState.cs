using UnityEngine;

public class KnockBackState : State
{
    private readonly PlayerContext _ctx;
    private readonly GameObject _bandagePrefab; // Referencia al prefab
    
    private Vector3 _start, _end, _control;
    private float _duration, _timer;
    private bool _isActive;

    public bool isActive => _isActive;
    
    // Constructor actualizado: Ahora pide el prefab
    public KnockBackState(PlayerContext ctx, GameObject bandagePrefab) 
    {
        _ctx = ctx;
        _bandagePrefab = bandagePrefab;
    }

    public override void OnEnter()
    {
        // 1. VALIDACIÓN
        if (!_ctx.Observer.HasKnockback)
        {
            _isActive = false;
            return;
        }

        var data = _ctx.Observer.PeekKnockback();
        
        // 2. SANITIZACIÓN
        if (data.Duration <= 0f)
        {
            _ctx.Observer.ConsumeKnockback(); 
            _isActive = false;
            return;
        }

        // --- SPAWN DE VENDAS (DROP) ---
        int stock = _ctx.Model.Bandages; // Obtenemos el stock actual
        
        if (_bandagePrefab != null && stock > 0)
        {
            Vector3 spawnOrigin = _ctx.Tf.position + Vector3.up; // Desde el "pecho" del player

            for (int i = 0; i < stock; i++)
            {
                // Generar posición con pequeña variación para que no salgan todas encimadas
                Vector3 randomOffset = Random.insideUnitSphere * 0.5f;
                Vector3 spawnPos = spawnOrigin + randomOffset;

                GameObject bandage = Object.Instantiate(_bandagePrefab, spawnPos, Random.rotation);

                bandage.GetComponent<Bandage>().SetupPickupDelay();
                
                // Si el prefab tiene Rigidbody, le damos un empujón explosivo
                if (bandage.TryGetComponent<Rigidbody>(out var rb))
                {
                    // Fuerza aleatoria hacia arriba y los lados
                    Vector3 explosionDir = Random.onUnitSphere;
                    explosionDir.y = Mathf.Abs(explosionDir.y); // Asegurar que salten hacia arriba
                    rb.AddForce(explosionDir * 8f, ForceMode.Impulse); 
                }
            }
        }
        _start = _ctx.Tf.position;
        _end = data.TargetPosition; 
        _duration = data.Duration;
        
        GameEventManager.Instance.playerEvents.OnHit.Raise();

        Vector3 mid = (_start + _end) / 2f;
        mid.y += 5f; 
        _control = mid;

        _timer = 0f;
        _isActive = true;
        _ctx.Rb.isKinematic = true;
        
        _ctx.View._koFX.Play();
    }

    public override void OnUpdate()
    {
        if (!_isActive) return;

        _timer += Time.deltaTime;
        float t = Mathf.Clamp01(_timer / _duration);

        float u = 1f - t;
        Vector3 pos = (u * u * _start) + (2f * u * t * _control) + (t * t * _end);
        _ctx.Tf.position = pos;

        if (t >= 1f) 
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

        _ctx.Rb.isKinematic = false;
        _ctx.Rb.velocity = Vector3.zero;
        _isActive = false;
        
        _ctx.View._koFX.Stop();

    }
}