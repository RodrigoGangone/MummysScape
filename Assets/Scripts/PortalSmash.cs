using System;
using System.Collections;
using UnityEngine;

public class PortalSmash : MonoBehaviour
{
    private Player player; // Referencia al jugador
    private LevelManager levelManager;

    private bool _isActive;

    private Action OnPlayerTeleportOn;
    private Action OnPlayerTeleportOff;

    [SerializeField] private Transform teleportDestination; // Punto de destino de la teletransportación

    private void Start()
    {
        levelManager = FindObjectOfType<LevelManager>();

        OnPlayerTeleportOn += levelManager.DesActivePlayer;
        OnPlayerTeleportOn += levelManager.StopTimerDeath;

        OnPlayerTeleportOff += levelManager.ActivePlayer;
    }

    // Detectar cuando el jugador entra en el portal
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Smash"))
        {
            player = other.gameObject.GetComponentInParent<Player>();

            if (player != null && !_isActive)
            {
                _isActive = true;
                StartCoroutine(HandleSmashCoroutine(player));
            }
        }
    }

    // Corutina principal para manejar el teletransporte
    private IEnumerator HandleSmashCoroutine(Player player)
    {
        levelManager.isBusy = true;

        // Desactivar el jugador y realizar acciones iniciales
        OnPlayerTeleportOn.Invoke();

        // Fase 1: Mover al jugador al centro del portal
        yield return StartCoroutine(MovePlayerToTeleport(player));

        // Fase 2: Desaparecer al jugador (transición de material inicial)
        yield return StartCoroutine(player._viewPlayer.MaterialTransitionCoroutine(true));

        // Fase 3: Teletransportar al jugador
        player.transform.position = teleportDestination.position + new Vector3(0, 1, 0);
        //TeleportPlayer();

        // Fase 4: Aparecer al jugador en el destino (transición de material final)
        yield return StartCoroutine(player._viewPlayer.MaterialTransitionCoroutine(false));

        // Reactivar el jugador después de la teletransportación
        OnPlayerTeleportOff.Invoke();

        levelManager.isBusy = false;

        _isActive = false; // Permitir nuevas activaciones del portal
    }

    // Mover al jugador hacia el centro del portal
    private IEnumerator MovePlayerToTeleport(Player player)
    {
        float moveSpeed = 1f;
        float distanceThreshold = 0.1f;

        // Mover al jugador hasta que esté lo suficientemente cerca del centro del portal
        while (Vector3.Distance(player.transform.position, transform.position + new Vector3(0, 1, 0)) >
               distanceThreshold)
        {
            player.transform.position =
                Vector3.MoveTowards(player.transform.position, transform.position + new Vector3(0, 1, 0),
                    moveSpeed * Time.deltaTime);

            // Esperar al siguiente frame
            yield return null;
        }

        // Asegurar que el jugador esté exactamente en el centro del portal
        player.transform.position = transform.position + new Vector3(0, 1, 0);
    }

    // Teletransportar al jugador al destino
    //private void TeleportPlayer()
    //{
    //    if (teleportDestination != null && player != null)
    //    {
    //        // Mover al jugador al destino del teletransporte
    //        player.transform.position = teleportDestination.position + new Vector3(0, 1, 0);
    //    }
    //}

//private void TriggerTeleport()
//{
//    if (OnPlayerTeleport != null)
//    {
//        OnPlayerTeleport.Invoke(); // Ejecutamos el Action para teletransportar
//    }
//}

//private void OnDestroy()
//{
//    OnPlayerTeleport -= TeleportPlayer;
//}
}