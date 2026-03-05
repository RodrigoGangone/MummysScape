using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Playables;
using UnityEngine.UI;
using static Utils;

public class MainMenu : MonoBehaviour
{
    [Header("TITLE")]
    [SerializeField] private Image _titleImage;
    private Coroutine _fadeTitleRoutine;
    private const float DEFAULT_TITLE_FADE_DURATION = 1f;
    
    [Header("PANEL MAIN MENU")] [SerializeField]
    private GameObject _mainMenuPanel;

    [SerializeField] private Material _mainMaterial;
    [SerializeField] private Button _btnPlay;
    [SerializeField] private Button _btnOptions;
    [SerializeField] private Button _btnExit;

    [Header("PANEL OPTIONS")] [SerializeField]
    private GameObject _optionsPanel;

    [SerializeField] private Material _optionsMaterial;
    [SerializeField] private Button _btnDeletePrefs;
    [SerializeField] private TMP_Dropdown _frameRateSpinner;
    [SerializeField] private Button _btnBackToMain;

    [Header("FIRST SELECTED")] [SerializeField]
    private Selectable _mainFirstSelected;

    [SerializeField] private Selectable _optionsFirstSelected;

    [Header("UI ROOT")] [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private PlayableDirector director;

    [Header("FEEDBACKS")] [SerializeField] private Animator sarcofagusAnim;

    private const string MATERIAL_POWER_PARAM = "_Power";
    private bool _isTransitioning;
    private static List<string> FrameRateText => new(FPS.Keys);

    private SceneTransitionManager Transition => GetComponent<SceneTransitionManager>();

    private void Awake()
    {
        AddButtonProps(_btnPlay, OnPlayClicked);
        AddButtonProps(_btnOptions, ShowOptions);
        AddButtonProps(_btnDeletePrefs, PlayerPrefsManager.ClearAll);
        AddButtonProps(_btnExit, QuitGame);
        AddButtonProps(_btnBackToMain, ShowMain);

        _frameRateSpinner.ClearOptions();
        _frameRateSpinner.AddOptions(FrameRateText);
        _frameRateSpinner.onValueChanged.AddListener(delegate { OnDropdownValueChanged(_frameRateSpinner); });
    }

    private void Start()
    {
        CheckOptions();

        // 1 = Abierto, 0 = Cerrado
        _mainMaterial.SetFloat(MATERIAL_POWER_PARAM, 1f); // Inicia visible
        _optionsMaterial.SetFloat(MATERIAL_POWER_PARAM, 0f); // Inicia oculto

        _mainMenuPanel.SetActive(true);
        _optionsPanel.SetActive(false);

        SetSelected(_mainFirstSelected ?? _btnPlay);
        SetMenuInteractable(true);
    }
    
    private void StartFadeTitle(float start, float end, float duration = DEFAULT_TITLE_FADE_DURATION)
    {
        // Si no está seteado en inspector, no hacemos nada pero dejamos huella (solo una vez)
        if (_titleImage == null)
        {
            Debug.LogWarning($"{nameof(MainMenu)}: _titleImage es null. Asigná el Image 'Title' en el Inspector.", this);
            return;
        }

        // Evita que dos corrutinas peleen por el alpha (spam de botones / doble transición)
        if (_fadeTitleRoutine != null)
            StopCoroutine(_fadeTitleRoutine);

        _fadeTitleRoutine = StartCoroutine(FadeTitleSafe(start, end, duration));
    }
    
    private IEnumerator FadeTitleSafe(float start, float end, float duration = 1.0f)
    {
        // Double-check por si se llama desde otro lugar
        if (_titleImage == null)
            yield break;

        // Duración inválida: seteo directo y salgo
        if (duration <= 0f)
        {
            var c = _titleImage.color;
            c.a = end;
            _titleImage.color = c;
            _fadeTitleRoutine = null;
            yield break;
        }

        float elapsed = 0f;

        // Tomamos el color inicial siempre desde el componente real
        Color color = _titleImage.color;

        // Arrancamos desde "start" para que sea determinista aunque el alpha esté en otro valor
        color.a = start;
        _titleImage.color = color;

        // Cacheo por seguridad (si cambia la referencia durante el fade, salimos)
        var imageRef = _titleImage;

        while (elapsed < duration)
        {
            // Si el objeto se desactiva o se destruye, cortamos limpio
            if (!isActiveAndEnabled || imageRef == null)
                break;

            // Si Unity destruyó el componente, la referencia se vuelve "fake null"
            if (imageRef == null)
                break;

            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float alpha = Mathf.Lerp(start, end, t);

            color.a = alpha;
            imageRef.color = color;

            yield return null;
        }

        // Si todavía existe, dejamos el valor final clavado
        if (isActiveAndEnabled && imageRef != null)
        {
            color = imageRef.color;
            color.a = end;
            imageRef.color = color;
        }

        _fadeTitleRoutine = null;
    }

    // --- FLUJOS DE NAVEGACIÓN ---

    private void ShowOptions()
    {
        if (_isTransitioning) return;
        
        StartFadeTitle(1f, 0f);

        // De Main (Visible=1) a Options (Visible=1)
        StartCoroutine(PanelTransitionRoutine(
            _mainMenuPanel, _mainMaterial,
            _optionsPanel, _optionsMaterial,
            _optionsFirstSelected));
    }

    private void ShowMain()
    {
        if (_isTransitioning) return;
        
        StartFadeTitle(0f, 1f);
        
        // De Options (Visible=1) a Main (Visible=1)
        StartCoroutine(PanelTransitionRoutine(
            _optionsPanel, _optionsMaterial,
            _mainMenuPanel, _mainMaterial,
            _mainFirstSelected));
    }

    private void OnPlayClicked()
    {
        if (_isTransitioning) return;
        SetMenuInteractable(false);
        StartFadeTitle(1f, 0f, 3f);

        GameEventManager.Instance.levelEvents.OnRumbleHigh.Raise(0.5f, 2f);
        GameEventManager.Instance.levelEvents.OnRumbleLow.Raise(0.5f, 2f);

        // Cerramos el menú (1 -> 0) y disparamos el director
        StartCoroutine(PanelTransitionRoutine(
            _mainMenuPanel, _mainMaterial,
            null, null, null,
            () => director.Play()));
    }

    public void InitSelector() => Transition.FadeInAndLoadScene(2);

    // --- CORRUTINA UNIFICADA ---

    private IEnumerator PanelTransitionRoutine(
        GameObject toHide, Material matToHide,
        GameObject toShow, Material matToShow,
        Selectable nextSelect,
        Action midAction = null)
    {
        _isTransitioning = true;

        // 1. CERRAR panel actual (1 -> 0)
        if (toHide != null) toHide.SetActive(false);

        // 2. INTERCAMBIO (Punto ciego: todo está en 0)
        if (matToHide != null) yield return LerpMaterial(matToHide, 1f, 0f);

        midAction?.Invoke();


        // 3. ABRIR panel nuevo (0 -> 1)
        if (toShow != null && matToShow != null)
        {
            yield return LerpMaterial(matToShow, 0f, 1f);
            if (nextSelect != null) SetSelected(nextSelect);
        }

        if (toShow != null) toShow.SetActive(true);

        _isTransitioning = false;
    }

    private IEnumerator LerpMaterial(Material mat, float start, float end)
    {
        float elapsed = 0f;
        float duration = 0.5f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            mat.SetFloat(MATERIAL_POWER_PARAM, Mathf.Lerp(start, end, elapsed / duration));
            yield return null;
        }

        mat.SetFloat(MATERIAL_POWER_PARAM, end);
    }

    // --- UTILIDADES (Sin cambios significativos) ---

    private void SetMenuInteractable(bool interactable)
    {
        if (_canvasGroup == null) return;
        _canvasGroup.interactable = interactable;
        _canvasGroup.blocksRaycasts = interactable;
    }

    private void CheckOptions()
    {
        string currentFPS = PlayerPrefs.GetString(SELECTED_FPS_KEY, "60 FPS");
        _frameRateSpinner.value = _frameRateSpinner.options.FindIndex(option => option.text == currentFPS);
    }

    private void OnDropdownValueChanged(TMP_Dropdown dropdown)
    {
        string selectedFPSKey = dropdown.options[dropdown.value].text;
        Application.targetFrameRate = FPS[selectedFPSKey];
        PlayerPrefs.SetString(SELECTED_FPS_KEY, selectedFPSKey);
    }

    private void AddButtonProps(Button button, Action action)
    {
        if (button != null) button.onClick.AddListener(() => action?.Invoke());
    }

    private static void SetSelected(Selectable selectable)
    {
        if (selectable == null || EventSystem.current == null) return;
        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(selectable.gameObject);
    }

    public void SelectFeedback()
    {
        if (Save.IsCinematicSeen("mainMenuCinematic"))
            sarcofagusAnim.Play("Reck_L_WakeUp");
    }

    private void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
    
    private void OnDisable()
    {
        if (_fadeTitleRoutine != null)
        {
            StopCoroutine(_fadeTitleRoutine);
            _fadeTitleRoutine = null;
        }
    }
}