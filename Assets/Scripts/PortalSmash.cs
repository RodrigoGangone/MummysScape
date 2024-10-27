using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UIElements;

public class PortalSmash : MonoBehaviour
{
    private Player _player; // Referencia al jugador
    private LevelManager _levelManager;

    private bool _isActive;

    private Action OnPlayerTeleportOn;
    private Action OnPlayerTeleportOff;
    
    [SerializeField] private Transform _teleportDestination; // Punto de destino de la teletransportación
    [SerializeField] private Transform _posHeadOpen;
    [SerializeField] public Transform _posHeadClose;
    
    private InteractableOutline _interactableOrigin;
    private InteractableOutline _interactableDestiny;

    private Animator _hippoAnim;

    private const string OPEN_ANIM = "isOpen";
    private const string CLOSE_ANIM = "isClose";
    private const string ANIM_NAME_OPEN = "HippoOpen";
    private const string ANIM_NAME_CLOSE = "HippoClose";

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
        //ETAPA 1 : OPEN ANIMATION
        
        // 0- Desactivar el jugador, paro tiempo de muerte y aviso al level manager que esta ocupado
        OnPlayerTeleportOn.Invoke();
        _levelManager.isBusy = true;
        
        // 1- Reposiciono al jugador
        yield return StartCoroutine(MovePlayerToHeadFake(player));
        
        // 2- Activo la animacion Open
        _hippoAnim.SetTrigger(OPEN_ANIM);
        
        // 3- Desactivo la visual del player
        player._viewPlayer.SetValueMaterial(-0.5f, -0.5f); // -0.5f = apagado
        
        //TODO: SI LA ANIMACION OPEN FINALIZO:
        yield return StartCoroutine(WaitForAnimationEnd(_hippoAnim, ANIM_NAME_OPEN));
        
        // ETAPA 2 : CLOSE ANIMATION
        
        // 0- Transporto al player hacia "Teleport destination" asi veo la segunda anim
        player.transform.position = 
            _teleportDestination.GetComponent<PortalSmash>()._posHeadClose.position; //aumento altura para que no colisione con nada raro
        
        // 1- Activo la animacion Close
        _teleportDestination.GetComponentInChildren<Animator>().SetTrigger(CLOSE_ANIM);
        
        //TODO: SI LA ANIMACION DE CLOSE FINALIZO
        yield return StartCoroutine(WaitForAnimationEnd(_teleportDestination.GetComponentInChildren<Animator>(), ANIM_NAME_CLOSE));

        // 3- Activo la visual del player
        player._viewPlayer.SetValueMaterial(1f, 1f); // 1f = encendido
        
        // 4- Activar al jugar, aviso al lvl manager que no esta mas ocupado 
        OnPlayerTeleportOff.Invoke();
        _levelManager.isBusy = false;
        _isActive = false;
        
    }

    // Mover al jugador hacia el centro del portal
    private IEnumerator MovePlayerToHeadFake(Player player) //enviar por parametro el lastHeadfake position
    {
        float moveSpeed = 2f;
        float distanceThreshold = 0.1f;

        // Mover al jugador hasta que esté lo suficientemente cerca del centro del portal
        while (Vector3.Distance(player.transform.position, _posHeadOpen.position) >
               distanceThreshold)
        {
            player.transform.position =
                Vector3.MoveTowards(player.transform.position,
                    _posHeadOpen.position,
                    moveSpeed * Time.deltaTime);

            // Esperar al siguiente frame
            yield return null;
        }

        // Asegurar que el jugador esté exactamente en el centro del portal
        player.transform.position = _posHeadOpen.position;
    }
    
    private IEnumerator WaitForAnimationEnd(Animator hippoAnimator, string animationName)
    {
        yield return null;

        AnimatorStateInfo currentStateInfo = hippoAnimator.GetCurrentAnimatorStateInfo(0);

        while (currentStateInfo.IsName(animationName) && currentStateInfo.normalizedTime < 1.0f)
        {
            yield return null;
            currentStateInfo = hippoAnimator.GetCurrentAnimatorStateInfo(0); 
        }
    }
}