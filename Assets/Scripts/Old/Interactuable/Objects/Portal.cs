using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using static Save;
using UnityEngine.Timeline;

public class Portal : MonoBehaviour
{
    [Header("Estado")] [SerializeField] private bool enterLevel;

    [Header("Referencias")] 
    
    [SerializeField] private PlayableDirector directorEnter;
    [SerializeField] private PlayableDirector directorExit;
    
    [SerializeField] private float moveDuration = 1.0f; // Tiempo de transición
    [SerializeField] private Transform winTarget;

    [Tooltip("Solo es necesario si estamos configurando un sarcofagus de inicio de nivel")]
    [SerializeField] private GameObject fakeMummy;

    [SerializeField] private Animator anim;
    private FocusOnActivation Focus => GetComponent<FocusOnActivation>();
    private Collider Col => GetComponent<Collider>();

    private const string LOCK_ID = "Sarcofagus";
    private const string PLAYER_TAG = "PlayerFather";

    private void Start()
    {
        if (!enterLevel) return;

        GameEventManager.Instance.playerEvents.OnLockRequested.Raise(LOCK_ID, true);

        Focus.Activate();
        OpenAnim();

        if (directorEnter != null)
            directorEnter.Play();
        else
            UnlockPlayer();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.gameObject.CompareTag(PLAYER_TAG) || enterLevel) return;
        GameEventManager.Instance.levelEvents.OnPrompt.Raise(true, buttonType.A);
    }

    private void OnTriggerStay(Collider other)
    {
        if (enterLevel) return;
        if (!other.CompareTag(PLAYER_TAG)) return;

        if (Input.GetButtonDown("Space"))
            StartWinSequence(other.gameObject.GetComponent<PlayerController>());
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.gameObject.CompareTag(PLAYER_TAG) || enterLevel) return;
        GameEventManager.Instance.levelEvents.OnPrompt.Raise(false, buttonType.A);
    }

    private void StartWinSequence(PlayerController player)
    {
        Col.enabled = false;
        
        Focus.Activate();
        
        GameEventManager.Instance.levelEvents.OnPrompt.Raise(false, buttonType.A);
        
        GameEventManager.Instance.playerEvents.OnLockRequested.Raise(LOCK_ID, true);

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
        else
            WinUI();
    }

    public void LockPlayer() => GameEventManager.Instance.playerEvents.OnLockRequested.Raise(LOCK_ID, true);

    public void UnlockPlayer()
    {
        if (fakeMummy != null && directorEnter != null)
        {
            directorEnter.Stop();
            Destroy(fakeMummy);
        }
        
        GameEventManager.Instance.playerEvents.OnLockRequested.Raise(LOCK_ID, false);
    }

    public void WinUI() => GameEventManager.Instance.levelEvents.OnWin.Raise(SceneManager.GetActiveScene().buildIndex);
    public void WinPlayer() => GameEventManager.Instance.playerEvents.OnWin.Raise();
    private void OpenAnim() => anim.SetTrigger("Open");
    public void CloseAnim() => anim.SetTrigger("Close");

    private void OnEnable() => GameEventManager.Instance.levelEvents.OnWin.Register<int>(CompleteLevel);
    private void OnDisable() => GameEventManager.Instance.levelEvents.OnWin.Unregister<int>(CompleteLevel);
}