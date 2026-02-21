using System;
using System.Collections;
using UnityEngine;

/// <summary> 
/// Utilidades de Sincronización: Provee métodos de extensión para corrutinas que permiten "congelar" 
/// el paso del tiempo o las esperas lógicas mientras el juego está en estado de pausa. 
/// </summary>

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
                yield return WaitWhilePaused(isPaused); 
            t += Time.deltaTime;
            yield return null;
        }
    }
}