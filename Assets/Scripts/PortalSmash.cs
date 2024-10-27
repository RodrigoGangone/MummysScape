using System;
using System.Collections;
using UnityEngine;
using static Utils;

public class PortalSmash : MonoBehaviour
{
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
        if (other.gameObject.CompareTag(PLAYER_TAG))
        {
            _interactableOrigin.OnMaterial();
            _interactableDestiny.OnMaterial();
        }

        if (other.gameObject.CompareTag(PLAYER_SMASH_TAG))
        {
            var player = other.gameObject.GetComponentInParent<Player>();

            if (player != null && !_isActive)
            {
                _isActive = true;
                StartCoroutine(HandleSmashCoroutine(player));
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.CompareTag(PLAYER_TAG))
        {
            _interactableOrigin.OffMaterial();
            _interactableDestiny.OffMaterial();
        }
    }

    private IEnumerator HandleSmashCoroutine(Player player)
    {
        var destinationScript = _teleportDestination.GetComponent<PortalSmash>();
        var destinationAnim = _teleportDestination.GetComponentInChildren<Animator>();
        
        #region OPEN ANIM

            // 0- Desactivar el jugador, paro tiempo de muerte y aviso al level manager que esta ocupado
            OnPlayerTeleportOn.Invoke();
            _levelManager.isBusy = true;
            
            // 1- Reposiciono al jugador
            yield return StartCoroutine(MovePlayerToHeadFake(player.transform));
            // 2- Activo la animacion Open
            _hippoAnim.SetTrigger(OPEN_ANIM);
            // 3- Desactivo la visual del player
            player._viewPlayer.SetValueMaterial(-0.5f, -0.5f); // -0.5f = apagado
            
            //TODO: SI LA ANIMACION OPEN FINALIZO:
            yield return StartCoroutine(WaitForAnimationEnd(_hippoAnim, ANIM_NAME_OPEN));

        #endregion

        #region CLOSE ANIM
        
            // 0- Transporto al player hacia "Teleport destination" asi veo la segunda anim
            player.transform.rotation =
                destinationScript._posHeadClose.rotation;
            player.transform.position = 
                destinationScript._posHeadClose.position;
            
            // 1- Activo la animacion Close
            destinationAnim.SetTrigger(CLOSE_ANIM);
            
            yield return StartCoroutine(WaitForAnimationEnd(destinationAnim, ANIM_NAME_CLOSE));

            // 3- Activo la visual del player
            player._viewPlayer.SetValueMaterial(1f, 1f); // 1f = encendido
            
            // 4- Activar al jugar, aviso al lvl manager que no esta mas ocupado 
            OnPlayerTeleportOff.Invoke();
            _levelManager.isBusy = false;
            _isActive = false;

        #endregion
    }

    // Mover al jugador hacia el centro del portal
    private IEnumerator MovePlayerToHeadFake(Transform playerTrans) //enviar por parametro el lastHeadfake position
    {
        float moveSpeed = 3f;
        float rotationSpeed = 90f; 
        float distanceThreshold = 0.05f;
        float rotationThreshold = 0.05f;

        // Mover y rotar al jugador hasta que esté lo suficientemente cerca de la posición y rotación objetivo
        while (Vector3.Distance(playerTrans.transform.position, _posHeadOpen.position) > distanceThreshold ||
               Quaternion.Angle(playerTrans.transform.rotation, _posHeadOpen.rotation) > rotationThreshold)
        {
            // Mover hacia la posición objetivo
            playerTrans.position = Vector3.MoveTowards(
                playerTrans.position,
                _posHeadOpen.position,
                moveSpeed * Time.deltaTime
            );

            // Rotar hacia la rotación objetivo
            playerTrans.rotation = Quaternion.RotateTowards(
                playerTrans.rotation,
                _posHeadOpen.rotation,
                rotationSpeed * Time.deltaTime
            );

            // Esperar al siguiente frame
            yield return null;
        }

        // Asegurar que el jugador esté exactamente en la posición y rotación objetivo
        playerTrans.position = _posHeadOpen.position;
        playerTrans.rotation = _posHeadOpen.rotation;
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