using UnityEngine;

/// <summary>
/// PlayerInputReaderLegacy
/// Implementa IPlayerInput usando Input clásico. Usa "consumibles" para edges.
/// </summary>
public sealed class PlayerInputReaderLegacy : MonoBehaviour, IPlayerInput
{
    private bool _shootDown, _dropDown, _spaceDown;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))     _shootDown = true;
        if (Input.GetKeyDown(KeyCode.Q))     _dropDown  = true;
        if (Input.GetKeyDown(KeyCode.Space)) _spaceDown = true;
    }

    public Vector2 Move => new(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));

    public bool ConsumeShootDown()   { var v = _shootDown;  _shootDown  = false; return v; }
    public bool ConsumeDropDown()    { var v = _dropDown;   _dropDown   = false; return v; }
    public bool ConsumeSpaceDown()   { var v = _spaceDown;  _spaceDown  = false; return v; }
    
    public bool IsSpaceHeld() => Input.GetKey(KeyCode.Space);

    public bool IsAnyActionHeld() => Input.GetKey(KeyCode.E) || Input.GetKey(KeyCode.Q) || Input.GetKey(KeyCode.Space);
}