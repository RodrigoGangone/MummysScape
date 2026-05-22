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
        SetAlpha(1f);
    }
    
    private void Start()
    {
        TriggerFadeOut(null);
    }

    private void Update()
    {
        // Detectar si se presionó cualquier tecla del Numpad
        if (Input.anyKeyDown)
        {
            CheckNumpadInput();
        }
    }

    // --- Métodos de Debug / Selección de Nivel ---

    private void CheckNumpadInput()
    {
        int targetSceneIndex = -1;

        // Evaluamos con KeyCode uno por uno
        if (Input.GetKeyDown(KeyCode.Keypad0))      targetSceneIndex = 2;  // Intro
        else if (Input.GetKeyDown(KeyCode.Keypad1)) targetSceneIndex = 3;  // Zone 1 - 1
        else if (Input.GetKeyDown(KeyCode.Keypad2)) targetSceneIndex = 4;  // Zone 1 - 2
        else if (Input.GetKeyDown(KeyCode.Keypad3)) targetSceneIndex = 5;  // Zone 1 - 3
        else if (Input.GetKeyDown(KeyCode.Keypad4)) targetSceneIndex = 6;  // Zone 1 - 4
        else if (Input.GetKeyDown(KeyCode.Keypad5)) targetSceneIndex = 7;  // Zone 1 - 5
        else if (Input.GetKeyDown(KeyCode.Keypad6)) targetSceneIndex = 8;  // Zone 1 - Boss
        else if (Input.GetKeyDown(KeyCode.Keypad7)) targetSceneIndex = 9;  // Selector 2
        else if (Input.GetKeyDown(KeyCode.Keypad8)) targetSceneIndex = 10; // Zone 2 - 1
        else if (Input.GetKeyDown(KeyCode.Keypad9)) targetSceneIndex = 11; // Zone 2 - 2

        // Si se presionó alguno, transicionamos
        if (targetSceneIndex != -1)
        {
            FadeInAndLoadScene(targetSceneIndex);
        }
    }

    // --- Métodos Públicos ---

    public void FadeInAndLoadScene(int sceneIndex)
    {
        GameEventManager.Instance.playerEvents.OnLockRequested.Raise("SceneTransition", true);
        StartCoroutine(FadeInRoutine(sceneIndex));
    }
    
    public Coroutine TriggerFadeIn(Action onComplete)
    {
        return StartCoroutine(Fade(1f, onComplete));
    }

    public Coroutine TriggerFadeOut(Action onComplete)
    {
        return StartCoroutine(Fade(0f, onComplete));
    }

    // --- Corutinas Internas ---
    
    private IEnumerator FadeInRoutine(int sceneIndex)
    {
        yield return StartCoroutine(Fade(1f, null));
        SceneManager.LoadScene(sceneIndex);
    }

    private IEnumerator Fade(float targetAlpha, Action onComplete)
    {
        Color color = fadeImage.color;
        float startAlpha = color.a;
        float time = 0f;

        while (time < fadeDuration)
        {
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