using UnityEngine;

public class AimState : State
{
    private readonly PlayerContext _ctx;
    private readonly GameObject _decal;
    private readonly GameObject _rangeIndicator; // <--- NUEVA LÍNEA

    public AimState(PlayerContext ctx)
    {
        _ctx = ctx;
        _decal = _ctx.View.Decal;
        // Obtenemos la referencia al indicador a través del InteractionRuntime
        _rangeIndicator = _ctx.View.RangeIndicator; // <--- NUEVA LÍNEA
    }
    
    public override void OnEnter()
    {
        SimpleShootData.Path = null;
        
        // --- LÓGICA DEL CÍRCULO DE RANGO ---
        if (_rangeIndicator != null)
        {
            _rangeIndicator.SetActive(true);
            
            // El radio es _maxDistance. El scale de un Quad es su diámetro.
            float diameter = _ctx.AimMaxDistance * 2f;
            _rangeIndicator.transform.localScale = new Vector3(diameter, diameter, 1f);
        }
    }

    public override void OnUpdate()
    {
        // Actualizamos la posición del círculo para que siga al jugador
        if (_rangeIndicator != null && _rangeIndicator.activeSelf)
        {
            // Lo posicionamos a los pies del jugador
            _rangeIndicator.transform.position = _ctx.Tf.position;
        }

        bool hasValidTarget = _ctx.TryGetAim(out var pos, out var normal);

        if (hasValidTarget)
        {
            SetDecalVisible(true);
            SetDecal(pos, normal);
        }
        else
        {
            SetDecalVisible(false);
        }
    }

    public override void OnFixedUpdate()
    {
        
    }

    public override void OnExit()
    {
        SetDecalVisible(false);
        
        // Ocultamos el círculo al salir del estado de apuntado
        if (_rangeIndicator != null)
        {
            _rangeIndicator.SetActive(false); // <--- NUEVA LÍNEA
        }
    }

    // ... (tus otros métodos como SetDecalVisible y SetDecal sin cambios)
    private void SetDecalVisible(bool visible)
    {
        if (_decal && _decal.activeSelf != visible) _decal.SetActive(visible);
    }
    private void SetDecal(Vector3 pos, Vector3 normal)
    {
        if (!_decal) return;
        _decal.transform.position = pos;
        _decal.transform.rotation = Quaternion.FromToRotation(Vector3.up, normal);
    }
}