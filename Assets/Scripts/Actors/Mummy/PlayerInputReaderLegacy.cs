using UnityEngine;

/// <summary> 
/// Lector de Inputs (Legacy): Implementa la interfaz IPlayerInput utilizando el sistema clásico de Unity, 
/// procesando ejes de movimiento, sticks de control y el estado de los botones de acción. 
/// </summary>
[DefaultExecutionOrder(-100)] // <--- AÑADE ESTA LÍNEA
public sealed class PlayerInputReaderLegacy : MonoBehaviour, IPlayerInput, IPausable, ILocked
{
    private bool _aimDown, _shootDown, _dropDown, _spaceDown;
    private bool _aimUp;
    private bool _cancelAim;
    private float _lastAimRt;    
    
    private bool _paused, _locked;

    private void Update()
    {
        if (_paused || _locked) return;
        
        float aimRt = Input.GetAxis("AimRT");

        bool currentAim = aimRt > 0.5f;
        bool previousAim = _lastAimRt > 0.5f;

        if (currentAim && !previousAim)
            _aimDown = true;

        if (!currentAim && previousAim)
            _aimUp = true;

        _lastAimRt = aimRt;

        if (Input.GetButtonDown("Space")) // A del joystick
            _cancelAim = true;
        
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

public bool ConsumeAimDown()
{
    bool v = _aimDown;
    _aimDown = false;
    return v;
}

public bool ConsumeAimUp()
{
    bool v = _aimUp;
    _aimUp = false;
    return v;
}

public bool ConsumeCancelAim()
{
    bool v = _cancelAim;
    _cancelAim = false;
    return v;
}    public bool ConsumeShootDown()   { var v = _shootDown;  _shootDown  = false; return v; }
    public bool ConsumeDropDown()    { var v = _dropDown;   _dropDown  = false; return v; }
    public bool ConsumeSpaceDown()   { var v = _spaceDown;  _spaceDown  = false; return v; }
    
    public bool IsSpaceHeld() => Input.GetKey(KeyCode.Space) || Input.GetButton("Space");
    public bool IsAimHeld() => Input.GetMouseButton(1) || Input.GetAxis("AimRT") > 0.5f;
    
    public void OnLockChanged(bool isLocked) => _locked = isLocked;
    public void OnPauseChanged(bool paused) => _paused = paused;
    
    private void OnEnable()
    {
        GameEventManager.Instance.levelEvents.OnPauseChanged.Register<bool>(OnPauseChanged);
        GameEventManager.Instance.playerEvents.OnLocked.Register<bool>(OnLockChanged);
    }

    private void OnDisable()
    {
        GameEventManager.Instance.levelEvents.OnPauseChanged.Unregister<bool>(OnPauseChanged);
        GameEventManager.Instance.playerEvents.OnLocked.Unregister<bool>(OnLockChanged);
    }
}