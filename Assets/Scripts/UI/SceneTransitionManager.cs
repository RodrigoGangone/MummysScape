// Nuevo script: SceneTransitionLocal.cs
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneTransitionManager : MonoBehaviour
{
    [SerializeField] private Image fadeImage;
    [SerializeField] private float fadeDuration = 1f;

    private void Awake()
    {
        // Asegurarse de que la imagen esté visible (negra) al empezar
        SetAlpha(1f);
    }
    
    private void Start()
    {
        // Al cargar la escena, hacer el fade-out (aclarar) automáticamente
        TriggerFadeOut(null);
    }
    
    // --- Métodos Públicos ---

    /**
     * Llamar para SALIR de la escena actual.
     * Hará un fundido a negro y luego cargará la nueva escena.
     */
    public void FadeInAndLoadScene(int sceneIndex)
    {
        GameEventManager.Instance.playerEvents.OnLockRequested.Raise("SceneTransition", true);
        StartCoroutine(FadeInRoutine(sceneIndex));
    }
    
    /**
     * Llamar para los paneles de Win/Lose (dentro de la misma escena).
     * Hará un fundido a negro (Fade IN).
     */
    public Coroutine TriggerFadeIn(Action onComplete)
    {
        return StartCoroutine(Fade(1f, onComplete));
    }

    /**
     * Llamar para aclarar la pantalla (Fade OUT).
     * (Ya se llama automáticamente en Start).
     */
    public Coroutine TriggerFadeOut(Action onComplete)
    {
        return StartCoroutine(Fade(0f, onComplete));
    }

    // --- Corutinas Internas ---
    
    private IEnumerator FadeInRoutine(int sceneIndex)
    {
        // 1. Fade IN (oscurecer la pantalla)
        yield return StartCoroutine(Fade(1f, null));
        
        // 2. Cargar la escena
        SceneManager.LoadScene(sceneIndex);
    }

    private IEnumerator Fade(float targetAlpha, Action onComplete)
    {
        Color color = fadeImage.color;
        float startAlpha = color.a;
        float time = 0f;

        while (time < fadeDuration)
        {
            // Usar UnscaledDeltaTime por si el juego está en pausa (Time.timeScale = 0)
            time += Time.unscaledDeltaTime; 
            float alpha = Mathf.Lerp(startAlpha, targetAlpha, time / fadeDuration);
            SetAlpha(alpha);
            yield return null;
        }

        SetAlpha(targetAlpha);
        onComplete?.Invoke();
    }
    
    private void SetAlpha(float alpha)
    {
        Color color = fadeImage.color;
        fadeImage.color = new Color(color.r, color.g, color.b, alpha);
    }
}