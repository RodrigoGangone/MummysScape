using UnityEngine;

/// <summary> 
/// Lector de Inputs (Legacy): Implementa la interfaz IPlayerInput utilizando el sistema clásico de Unity, 
/// procesando ejes de movimiento, sticks de control y el estado de los botones de acción. 
/// </summary>

public sealed class PlayerInputReaderLegacy : MonoBehaviour, IPlayerInput
{
    private bool _aimDown, _shootDown, _dropDown, _spaceDown;
    private float _lastAimRt; 
    
    private void Update()
    {
        float aimRt = Input.GetAxis("AimRT"); 

        bool mouseAimDown = Input.GetMouseButtonDown(1);
        bool triggerAimDown = aimRt > 0.5f && _lastAimRt <= 0.5f;

        if (mouseAimDown || triggerAimDown)
            _aimDown = true;
            
        _lastAimRt = aimRt;
        
        if (Input.GetMouseButtonDown(0) || Input.GetButtonDown("Shoot"))     
            _shootDown = true;
        
        if (Input.GetKeyDown(KeyCode.Q) || Input.GetButtonDown("Drop"))     
            _dropDown  = true;
        
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetButtonDown("Space")) 
            _spaceDown = true;

        AimMove = new Vector2(Input.GetAxis("RightStickX"), Input.GetAxis("RightStickY"));
    }
    
    public Vector2 Move => new(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
    public Vector2 AimMove { get; private set; } 

    public bool ConsumeAimHeld()     { var v = _aimDown;    _aimDown  = false; return v; }
    public bool ConsumeShootDown()   { var v = _shootDown;  _shootDown  = false; return v; }
    public bool ConsumeDropDown()    { var v = _dropDown;   _dropDown  = false; return v; }
    public bool ConsumeSpaceDown()   { var v = _spaceDown;  _spaceDown  = false; return v; }
    
    public bool IsSpaceHeld() => Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.Joystick1Button0);
    public bool IsAimHeld() => Input.GetMouseButton(1) || Input.GetAxis("AimRT") > 0.5f;
    public bool IsAnyActionHeld() => 
        Input.GetKey(KeyCode.Q) || Input.GetButtonDown("Drop") || 
        Input.GetKey(KeyCode.Space) || Input.GetButtonDown("Space");
}