using UnityEngine;

/// <summary>
/// Gestiona la detección de interactables en Update (independiente de la StateMachine)
/// para activar/desactivar sus outlines (InteractableOutline).
/// </summary>
[RequireComponent(typeof(PlayerController))]
public class PlayerInteractionManager : MonoBehaviour
{
    private PlayerContext _ctx; // El contexto compartido que usan los States
    private Interactable _lastInteractableOutline;

    private void Start()
    {
        // Obtenemos el Context (Ctx) desde el PlayerController
        _ctx = GetComponent<PlayerController>().Ctx; 
        if (_ctx == null)
        {
            Debug.LogError("PlayerInteractionManager no pudo encontrar el PlayerContext!");
            this.enabled = false;
        }
    }

    private void Update()
    {
        if (_ctx == null) return;
        
        // 1. Encontrar el interactable más relevante ahora mismo
        Interactable currentInteractable = FindCurrentInteractable();
        
        // 2. Actualizar el estado del outline
        HandleOutlineState(currentInteractable);
    }

    /// <summary>
    /// Usa el PlayerContext para chequear todos los tipos de interacción
    /// y devuelve el outline del primer objeto válido que encuentre.
    /// </summary>
    private Interactable FindCurrentInteractable()
    {
        // El orden importa. Decide qué interacción tiene prioridad.
        
        // 1. Chequear Push
        if (_ctx.TryGetPushTarget(out var pushTarget, out _, out _))
        {
            return pushTarget.GetComponent<Interactable>();
        }

        // 2. Chequear Attract
        if (_ctx.TryGetAttractTarget(out var attractTarget))
        {
            return attractTarget.GetComponent<Interactable>();
        }

        // 3. Chequear Swing (Hook)
        if (_ctx.TryGetSwingTarget(out var swingTarget))
        {
            // swingTarget es un Rigidbody, el outline puede estar en el mismo objeto o en su padre
            return swingTarget.GetComponentInParent<Interactable>();
        }

        // 4. Chequear Quick Travel (Hippo)
        if (_ctx.TryGetQuickTravel(_ctx.Tf, out var portal))
        {
            return portal.GetComponent<Interactable>();
        }
        
        // 5. Chequear Aim (Disparo)
        // Nota: TryGetAim() no devuelve el *objeto* golpeado, solo el punto.
        // Si quieres que el objeto apuntado muestre un outline,
        // habría que modificar InteractionRuntime.TryGetAim para que devuelva el transform.
        
        return null; // No se encontró ningún interactable
    }

    /// <summary>
    /// Compara el interactable actual con el anterior para decidir
    /// cuál apagar y cuál encender, evitando llamadas innecesarias.
    /// </summary>
    private void HandleOutlineState(Interactable current)
    {
        if (current == _lastInteractableOutline)
            return; // No hay cambios

        // Apagar el anterior (si existía)
        if (_lastInteractableOutline != null)
        {
            _lastInteractableOutline.OffMaterial();
        }

        // Encender el nuevo (si existe)
        if (current != null)
        {
            current.OnMaterial(); 
        }

        // Actualizar la referencia para el próximo frame
        _lastInteractableOutline = current;
    }
}