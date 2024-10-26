using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

public class PortalSmash : MonoBehaviour
{
    private Player _player; // Referencia al jugador
    private LevelManager _levelManager;

    private bool _isActive;

    private Action OnPlayerTeleportOn;
    private Action OnPlayerTeleportOff;

    [SerializeField] private Transform _teleportDestination; // Punto de destino de la teletransportación

    private InteractableOutline _interactableOrigin;
    private InteractableOutline _interactableDestiny;

    private Animator _hippoAnim;

    private const string OPEN_ANIM = "isOpen";
    private const string CLOSE_ANIM = "isClose";

    private void Start()
    {
        _levelManager = FindObjectOfType<LevelManager>();

        _hippoAnim = GetComponentInChildren<Animator>();

        _interactableOrigin = GetComponent<InteractableOutline>();
        _interactableDestiny = _teleportDestination.GetComponent<InteractableOutline>();

        OnPlayerTeleportOn += _levelManager.DesActivePlayer;
        OnPlayerTeleportOn += _levelManager.StopTimerDeath;

        OnPlayerTeleportOff += _levelManager.ActivePlayer;
    }

    // Detectar cuando el jugador entra en el portal
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("PlayerFather"))
        {
            _interactableOrigin.OnMaterial();
            _interactableDestiny.OnMaterial();
        }

        if (other.gameObject.CompareTag("Smash"))
        {
            _player = other.gameObject.GetComponentInParent<Player>();

            if (_player != null && !_isActive)
            {
                _isActive = true;
                StartCoroutine(HandleSmashCoroutine(_player));
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.CompareTag("PlayerFather"))
        {
            _interactableOrigin.OffMaterial();
            _interactableDestiny.OffMaterial();
        }
    }

    // Corutina principal para manejar el teletransporte
    private IEnumerator HandleSmashCoroutine(Player player)
    {
        _hippoAnim.SetTrigger(OPEN_ANIM);

        _levelManager.isBusy = true;

        // Desactivar el jugador y realizar acciones iniciales
        OnPlayerTeleportOn.Invoke();

        // Fase 1: Mover al jugador al centro del portal
        yield return StartCoroutine(MovePlayerToTeleport(player));

        // Fase 2: Desaparecer al jugador (transición de material inicial)
        yield return StartCoroutine(player._viewPlayer.MaterialTransitionCoroutine(true));

        // Fase 3: Teletransportar al jugador
        player.transform.position = _teleportDestination.position + new Vector3(0, 1, 0);

        _hippoAnim.SetTrigger(CLOSE_ANIM);
        //TeleportPlayer();

        _teleportDestination.GetComponentInChildren<Animator>().SetTrigger(OPEN_ANIM);

        // Fase 4: Aparecer al jugador en el destino (transición de material final)
        yield return StartCoroutine(player._viewPlayer.MaterialTransitionCoroutine(false));

        // Reactivar el jugador después de la teletransportación
        OnPlayerTeleportOff.Invoke();

        _levelManager.isBusy = false;

        _teleportDestination.GetComponentInChildren<Animator>().SetTrigger(CLOSE_ANIM);

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
}