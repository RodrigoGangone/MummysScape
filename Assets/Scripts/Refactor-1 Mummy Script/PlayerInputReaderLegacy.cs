using UnityEngine;

/// <summary>
/// PlayerInputReaderLegacy
/// Implementa IPlayerInput usando Input clásico. Usa "consumibles" para edges.
/// </summary>
public sealed class PlayerInputReaderLegacy : MonoBehaviour, IPlayerInput
{
    private bool _shootDown, _dropDown, _smashDown, _attractDown;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))     _shootDown = true;
        if (Input.GetKeyDown(KeyCode.Q))     _dropDown  = true;
        if (Input.GetKeyDown(KeyCode.Space)) _smashDown = true;
        // _attractDown si mapeás otra tecla para atraer
    }

    public Vector2 Move => new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));

    public bool ConsumeShootDown()   { var v = _shootDown;  _shootDown  = false; return v; }
    public bool ConsumeDropDown()    { var v = _dropDown;   _dropDown   = false; return v; }
    public bool ConsumeSmashDown()   { var v = _smashDown;  _smashDown  = false; return v; }
    public bool ConsumeAttractDown() { var v = _attractDown;_attractDown= false; return v; }

    public bool IsAnyActionHeld() =>
        Input.GetKey(KeyCode.E) || Input.GetKey(KeyCode.Q) || Input.GetKey(KeyCode.Space);
}