using System.Collections;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using static Tags;
using static Animations.Sarcofagus;

/// <summary> 
/// Gestor de Transición: Administra la entrada y salida de niveles, coordinando cinemáticas (Timeline), 
/// persistencia de guardado y lógica diferenciada entre el Hub y los niveles de juego. 
/// </summary>
public class Portal : MonoBehaviour
{
    [Tooltip("¿Este portal es el inicio del nivel (donde aparece la momia)?")] [SerializeField]
    private bool enterLevel;

    [Tooltip("¿Este portal está ubicado en el HUB/Selector de Niveles?")] [SerializeField]
    private bool isSelectorPortal;

    [Tooltip("ID o Índice de escena a cargar (Nivel o Hub)")] [SerializeField]
    private int sceneIndex;

    [Header("Referencias")] [SerializeField]
    private PlayableDirector directorEnter;

    [SerializeField] private PlayableDirector directorExit;
    [SerializeField] private float moveDuration = 1.0f;
    [SerializeField] private Transform winTarget;

    [Tooltip("Solo es necesario si estamos configurando un sarcofagus de inicio de nivel")] [SerializeField]
    private GameObject fakeMummy;

    [SerializeField] private Animator anim;

    private FocusOnActivation Focus => GetComponent<FocusOnActivation>();
    private Collider Col => GetComponent<Collider>();

    private const string LOCK_ID = "Sarcofagus";

    private bool _canInteract;
    private bool _isPromptActive;
    private PlayerController _cachedPlayer;

    private void Start()
    {
        if (!enterLevel) return;

        GameEventManager.Instance.playerEvents.OnLockRequested.Raise(LOCK_ID, true);
        
        anim.SetTrigger(OPEN);
        
        if (directorEnter != null)
            directorEnter.Play();
    }

    private void Update()
    {
        if (_canInteract && !enterLevel && Input.GetButtonDown("Accept"))
        {
            if (_cachedPlayer == null) return;

            _canInteract = false;
            StartWinSequence(_cachedPlayer);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(PLAYER_TAG) || enterLevel) return;

        _cachedPlayer = other.GetComponent<PlayerController>();
        _canInteract = true;

        GameEventManager.Instance.levelEvents.OnContextUIChanged.Raise(
            ContextUIFactory.Prompt(ContextMessageType.Interact, ButtonType.Y)
        );

        _isPromptActive = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(PLAYER_TAG) || enterLevel) return;

        _canInteract = false;
        _cachedPlayer = null;

        if (_isPromptActive)
        {
            GameEventManager.Instance.levelEvents.OnContextUIChanged.Raise(ContextUIFactory.Hidden());
            _isPromptActive = false;
        }
    }

    private void StartWinSequence(PlayerController player)
    {
        if (Col != null)
            Col.enabled = false;

        Focus.Activate();

        GameEventManager.Instance.levelEvents.OnContextUIChanged.Raise(
            ContextUIFactory.Hidden()
        );

        GameEventManager.Instance.playerEvents.OnLockRequested.Raise(LOCK_ID, true);

        if (isSelectorPortal)
            Save.SetLastLevelPlayed(sceneIndex);

        StartCoroutine(MoveAndOrientPlayer(player));
    }

    private IEnumerator MoveAndOrientPlayer(PlayerController player)
    {
        float timer = 0f;
        Vector3 startPos = player.Ctx.Tf.position;
        Quaternion startRot = player.Ctx.Tf.rotation;

        while (timer < moveDuration)
        {
            float t = timer / moveDuration;
            t = Mathf.SmoothStep(0f, 1f, t);

            player.Ctx.Tf.position = Vector3.Lerp(startPos, winTarget.position, t);
            player.Ctx.Tf.rotation = Quaternion.Slerp(startRot, winTarget.rotation, t);

            timer += Time.deltaTime;
            yield return null;
        }

        player.Ctx.Tf.position = winTarget.position;
        player.Ctx.Tf.rotation = winTarget.rotation;

        if (directorExit != null)
            directorExit.Play();
    }

    public void LockPlayer() =>
        GameEventManager.Instance.playerEvents.OnLockRequested.Raise(LOCK_ID, true);

    public void UnlockPlayer()
    {
        if (fakeMummy != null && directorEnter != null)
        {
            directorEnter.Stop();
            Destroy(fakeMummy);
        }

        GameEventManager.Instance.playerEvents.OnLockRequested.Raise(LOCK_ID, false);
    }

    public void WinUI()
    {
        if (!isSelectorPortal)
            GameEventManager.Instance.levelEvents.OnWin.Raise(SceneManager.GetActiveScene().buildIndex);
        else
            GameEventManager.Instance.levelEvents.OnWin.Raise(-sceneIndex);
    }

    private void CompleteLevel(int index)
    {
        if (index < 0) return;

        Save.CompleteLevel(index);
        Debug.Log("Progreso registrado para nivel: " + index);
    }

    public void WinPlayer() => GameEventManager.Instance.playerEvents.OnWin.Raise();

    public void CloseAnim()
    {
        if (anim != null)
            anim.SetTrigger(CLOSE);
    }

    private void OnEnable() =>
        GameEventManager.Instance.levelEvents.OnWin.Register<int>(CompleteLevel);

    private void OnDisable() =>
        GameEventManager.Instance.levelEvents.OnWin.Unregister<int>(CompleteLevel);
}