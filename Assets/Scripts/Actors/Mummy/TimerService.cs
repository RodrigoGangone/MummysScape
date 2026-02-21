using System;
using System.Collections;
using UnityEngine;

/// <summary> 
/// Servicio de Tiempo: Provee una infraestructura ligera de temporizadores basados en corrutinas 
/// para gestionar eventos con duración, como el tiempo límite del estado "Head". 
/// </summary>

public sealed class TimerService : MonoBehaviour
{
    public sealed class Handle { internal Coroutine Co; public bool IsActive => Co != null; }

    public Handle StartTimer(float seconds, Action<float> onTick, Action onComplete)
    {
        var h = new Handle();
        h.Co = StartCoroutine(Run(seconds, onTick, onComplete, h));
        return h;
    }

    public void Cancel(Handle handle)
    {
        if (handle?.Co != null) { StopCoroutine(handle.Co); handle.Co = null; }
    }

    private static IEnumerator Run(float seconds, Action<float> onTick, Action onComplete, Handle h)
    {
        float t = seconds;
        while (t > 0f)
        {
            t -= Time.deltaTime;
            onTick?.Invoke(Mathf.Max(0f, t));
            yield return null;
        }
        onComplete?.Invoke();
        h.Co = null;
    }
}