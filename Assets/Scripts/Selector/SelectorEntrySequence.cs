using System;
using UnityEngine;
using UnityEngine.Playables;

public class SelectorEntrySequence : MonoBehaviour
{
    [SerializeField]
    private PlayableDirector director;

    [SerializeField]
    private bool restartFromBeginning = true;

    private Action _onComplete;

    public bool IsPlaying =>
        director != null &&
        director.state == PlayState.Playing;

    private void Awake()
    {
        if (director != null)
            director.playOnAwake = false;
    }

    public void Play(Action onComplete)
    {
        CancelCurrent(false);

        if (director == null)
        {
            onComplete?.Invoke();
            return;
        }

        _onComplete = onComplete;

        director.stopped += HandleStopped;

        if (restartFromBeginning)
            director.time = 0d;

        director.Play();
    }

    public void CancelCurrent(
        bool stopDirector = true)
    {
        if (director != null)
            director.stopped -= HandleStopped;

        _onComplete = null;

        if (stopDirector &&
            director != null &&
            director.state == PlayState.Playing)
        {
            director.Stop();
        }
    }

    private void HandleStopped(
        PlayableDirector stoppedDirector)
    {
        if (director != null)
            director.stopped -= HandleStopped;

        Action callback =
            _onComplete;

        _onComplete = null;

        callback?.Invoke();
    }

    private void OnDisable()
    {
        CancelCurrent(false);
    }
}