using UnityEngine;
using static Tags;

/// <summary> 
/// Vinculador de Movimiento: Asegura que el jugador se desplace solidariamente con la plataforma 
/// al emparentar su Transform durante el contacto, manteniendo la integridad de la posición global. 
/// </summary>

public class PlatformParent : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(PLAYER_TAG))
        {
            other.transform.SetParent(transform, true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(PLAYER_TAG))
        {
            other.transform.SetParent(null, true);
        }
    }
}
