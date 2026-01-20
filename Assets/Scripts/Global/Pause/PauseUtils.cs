using System;
using System.Collections;
using UnityEngine;

public static class PauseUtils
{
    public static IEnumerator WaitWhilePaused(Func<bool> isPaused)
    {
        while (isPaused()) yield return null;
    }

    public static IEnumerator WaitForSecondsPausable(float seconds, Func<bool> isPaused)
    {
        float t = 0f;
        while (t < seconds)
        {
            if (isPaused())
                yield return WaitWhilePaused(isPaused); // se “congela” el conteo
            t += Time.deltaTime;
            yield return null;
        }
    }
}