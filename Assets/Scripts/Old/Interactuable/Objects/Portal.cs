using UnityEngine;
using UnityEngine.SceneManagement;
using static Save;
using static Utils;

public class Portal : MonoBehaviour
{
    [SerializeField] private bool isOpen;
    private Animator Anim => GetComponentInChildren<Animator>();
    private FocusOnActivation Focus => GetComponent<FocusOnActivation>();
    
    private void OnEnable() => GameEventManager.Instance.levelEvents.OnWin.Register<int>(CompleteLevel);
    private void OnDisable() => GameEventManager.Instance.levelEvents.OnWin.Unregister<int>(CompleteLevel);

    private void Start()
    {
        if (!isOpen) return;

        Locked();
        
        Focus.Activate();
        
        InitLevelFx();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.gameObject.CompareTag(PLAYER_TAG) || isOpen) return;

        var col = GetComponent<Collider>();
        col.enabled = false;
        
        PassedLevelFX();
        Focus.Activate();
    }

    /// <summary>
    /// Eventos que se encargan de avisar cuando debe Iniciar le nivel y darlo por finalizado
    /// </summary>
    /// 
    public void Locked() => GameEventManager.Instance.playerEvents.OnLocked.Raise(true);
    public void UnLocked() => GameEventManager.Instance.playerEvents.OnLocked.Raise(false);
    public void PassedLevel() => GameEventManager.Instance.levelEvents.OnWin.Raise(SceneManager.GetActiveScene().buildIndex);
    
    /// <summary>
    /// Fx's correspondientes a las animaciones del sarcofago
    ///
    /// Open -> Al iniciar el nivel
    /// Close -> Al finalizar el nivel
    /// </summary>
    private void PassedLevelFX() => Anim.SetTrigger("Close");
    private void InitLevelFx() => Anim.SetTrigger("Open");
}