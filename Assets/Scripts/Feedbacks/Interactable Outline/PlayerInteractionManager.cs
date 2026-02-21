using UnityEngine;

/// <summary> 
/// Buscador de Objetivos: Escanea el entorno en tiempo real para identificar el interactuable más 
/// relevante y gestionar el encendido/apagado de su material de realce. 
/// </summary>

[RequireComponent(typeof(PlayerController))]
public class PlayerInteractionManager : MonoBehaviour
{
    private PlayerContext _ctx;
    private Interactable _lastInteractableOutline;

    private void Start()
    {
        _ctx = GetComponent<PlayerController>().Ctx; 
        if (_ctx == null)
        {
            Debug.LogError("PlayerInteractionManager no pudo encontrar el PlayerContext!");
            enabled = false;
        }
    }

    private void Update()
    {
        if (_ctx == null) return;
        
        Interactable currentInteractable = FindCurrentInteractable();
        
        HandleOutlineState(currentInteractable);
    }
    
    private Interactable FindCurrentInteractable()
    {
        if (_ctx.TryGetPushTarget(out var pushTarget, out _, out _))
        {
            return pushTarget.GetComponent<Interactable>();
        }

        if (_ctx.TryGetAttractTarget(out var attractTarget))
        {
            return attractTarget.GetComponent<Interactable>();
        }

        if (_ctx.TryGetSwingTarget(out var swingTarget))
        {
            return swingTarget.GetComponentInParent<Interactable>();
        }

        if (_ctx.TryGetQuickTravel(_ctx.Tf, out var portal))
        {
            return portal.GetComponent<Interactable>();
        }
        
        return null; 
    }
    private void HandleOutlineState(Interactable current)
    {
        if (current == _lastInteractableOutline)
            return; 

        if (_lastInteractableOutline != null)
        {
            _lastInteractableOutline.OffMaterial();
        }

        if (current != null)
        {
            current.OnMaterial(); 
        }

        _lastInteractableOutline = current;
    }
}