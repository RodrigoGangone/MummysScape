using UnityEngine;
using static Tags;

/// <summary> 
/// Detector de Geyser: Actúa como puente de colisión para notificar al componente Geyser principal 
/// cuando el jugador entra o sale de su área de influencia, gestionando el emparentamiento dinámico. 
/// </summary>

public class GeyserTrigger : MonoBehaviour
{
    private Geyser _geyser;

    void Start()
    {
        _geyser = GetComponentInParent<Geyser>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(PLAYER_TAG))
        {
            _geyser.OnPlayerEnterTrigger(other);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(PLAYER_TAG))
        {
            _geyser.OnPlayerExitTrigger(other);
        }
    }
}
