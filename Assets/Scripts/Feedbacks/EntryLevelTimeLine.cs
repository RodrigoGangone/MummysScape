using UnityEngine;
using UnityEngine.Playables; // Necesario para apagar el director si quieres

public class EntryLevelTimeLine : MonoBehaviour
{
    [Header("Referencias")] 
    
    [SerializeField] private GameObject fakeMummy; 
    [SerializeField] private PlayableDirector director; 

    public void LockPlayer() => GameEventManager.Instance.playerEvents.OnLocked.Raise(true);

    public void UnlockPlayer()
    {
        Destroy(fakeMummy);

        director.Stop();

        GameEventManager.Instance.playerEvents.OnLocked.Raise(false);
    }
}