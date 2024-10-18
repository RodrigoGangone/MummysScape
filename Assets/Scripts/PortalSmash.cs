using System;
using UnityEngine;

public class PortalSmash : MonoBehaviour
{
    [SerializeField] private Transform teleportDestination; // Punto de destino de la teletransportación
    private GameObject player; // Referencia al jugador
    
    public Action OnPlayerTeleport;

    private void Start()
    {
        OnPlayerTeleport += TeleportPlayer;
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("PlayerSmash"))
        {
            player = other.gameObject; // Guardamos la referencia al jugador

            // Aquí podrías suscribirte desde el jugador cuando entre en el trigger
            Player playerScript = player.GetComponent<Player>(); // Asegúrate de tener acceso al script del jugador
            if (playerScript != null)
            {
                playerScript.OnSmashFinished += TriggerTeleport; // Subscribimos al evento del jugador
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.CompareTag("PlayerSmash"))
        {
            if (player != null)
            {
                Player playerScript = player.GetComponent<Player>();
                if (playerScript != null)
                {
                    playerScript.OnSmashFinished -= TriggerTeleport; // Nos desuscribimos cuando el jugador sale del trigger
                }
            }

            player = null; // Limpiamos la referencia del jugador
        }
    }
    
    private void TriggerTeleport()
    {
        if (OnPlayerTeleport != null)
        {
            OnPlayerTeleport.Invoke(); // Ejecutamos el Action para teletransportar
        }
    }

    private void TeleportPlayer()
    {
        if (teleportDestination != null && player != null)
        {
            player.transform.position = teleportDestination.position;
        }
    }

    private void OnDestroy()
    {
        OnPlayerTeleport -= TeleportPlayer;
    }
}
