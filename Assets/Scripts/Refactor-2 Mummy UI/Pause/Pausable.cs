using System.Collections;
using UnityEngine;
public abstract class Pausable : MonoBehaviour, IPausable
{
    protected bool Paused { get; private set; }

    protected virtual void OnEnable()
    {
        GameEventManager.Instance.levelEvents.OnPauseChanged.Register<bool>(HandlePause);
    }

    protected virtual void OnDisable()
    {
        GameEventManager.Instance.levelEvents.OnPauseChanged.Unregister<bool>(HandlePause);
    }

    private void HandlePause(bool paused)
    {
        Paused = paused;
        OnPauseChanged(paused);
    }

    public abstract void OnPauseChanged(bool paused);

    protected IEnumerator WaitWhilePaused()
    {
        while (Paused) yield return null;
    }

    protected IEnumerator WaitForSecondsPausable(float seconds)
    {
        float t = 0f;
        while (t < seconds)
        {
            if (Paused) yield return WaitWhilePaused();
            t += Time.deltaTime;
            yield return null;
        }
    }
}
