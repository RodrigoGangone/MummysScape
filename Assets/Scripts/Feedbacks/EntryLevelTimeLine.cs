using UnityEngine;
using UnityEngine.Playables;

public class EntryLevelTimeLine : MonoBehaviour
{
    [Header("Referencias")] 
    
    [SerializeField] private GameObject fakeMummy; 
    [SerializeField] private PlayableDirector director; 

    private const string LOCK_ID = "EntryTimeline";
    
    public void LockPlayer() => GameEventManager.Instance.playerEvents.OnLockRequested.Raise(LOCK_ID, true);

    public void UnlockPlayer()
    {
        Destroy(fakeMummy);

        director.Stop();

        GameEventManager.Instance.playerEvents.OnLockRequested.Raise(LOCK_ID, false);    
    }
}