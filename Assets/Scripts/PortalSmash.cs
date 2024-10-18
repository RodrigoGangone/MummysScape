using System;
using UnityEngine;

public class PortalSmash : MonoBehaviour
{
    [SerializeField] private Transform teleportDestination; // Punto de destino de la teletransportación
    private Player player; // Referencia al jugador

    public Action OnPlayerTeleport;

    private void Start()
    {
        OnPlayerTeleport += TeleportPlayer;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Smash"))
        {
            player = other.gameObject.GetComponentInParent<Player>();

            if (player != null)
            {
            }
            //player.OnSmashFinished += TriggerTeleport; //ejecutar teleport una vez terminada animacion smash.
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