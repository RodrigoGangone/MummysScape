using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using static Utils;

public class MainMenu : MonoBehaviour
{
    [Header("PANEL MAIN MENU")] 
    [SerializeField] private GameObject _mainMenuPanel;

    [SerializeField] private Button _btnPlay;
    [SerializeField] private Button _btnOptions;
    [SerializeField] private Button _btnExit;

    [Header("PANEL OPTIONS")] 
    [SerializeField] private GameObject _optionsPanel;
    
    [SerializeField] private Button _btnDeletePrefs;
    [SerializeField] private TMP_Dropdown _frameRateSpinner;

    // Selectables iniciales por panel
    [Header("FIRST SELECTED")]
    [SerializeField] private Selectable _mainFirstSelected;
    [SerializeField] private Selectable _optionsFirstSelected;
    
    [Header("UI ROOT")]
    [Tooltip("CanvasGroup del Canvas del main menu")]
    [SerializeField] private CanvasGroup _canvasGroup;
    
    
    private const string INITIAL_TUTORIAL_ID = "mainMenuCinematic";
    //----------------------------------------------------------------------------------------------------
    
    private static List<string> FrameRateText => new(FPS.Keys);
    [SerializeField] private Button _btnBackToMain;
    private SceneTransitionManager Transition => GetComponent<SceneTransitionManager>();
    private DepthOfField _blur;
    private Volume _postProcess;

    private void Awake()
    {
        AddButtonProps(_btnPlay, OnPlayClicked);
        AddButtonProps(_btnOptions, ShowOptions);
        AddButtonProps(_btnDeletePrefs, PlayerPrefsManager.ClearAll);
        AddButtonProps(_btnExit, QuitGame);

        _frameRateSpinner.AddOptions(FrameRateText);
        _frameRateSpinner.onValueChanged.AddListener(delegate { OnDropdownValueChanged(_frameRateSpinner); });

        AddButtonProps(_btnBackToMain, ShowMain);
    }

    private void AddButtonProps(Button button, Action mainAction, params Action[] additionalActions)
    {
        button.onClick.AddListener(() =>
        {
            mainAction?.Invoke();

            if (additionalActions == null) return;
            foreach (var action in additionalActions)
            {
                action?.Invoke();
            }
        });
    }

    private void Start()
    {
        //_postProcess = FindObjectOfType<Volume>();
        //
        //if (_postProcess.profile.TryGet(out _blur))
        //    _blur.active = !_blur.active;

        CheckOptions();
        
        // Al arrancar la escena, asegurar botón inicial del panel principal
        SetSelected(_mainFirstSelected ?? _btnPlay);
        SetMenuInteractable(true);
    }
    
    //Metodo para quitar la interaccion con los botones del menu principal
    private void SetMenuInteractable(bool interactable)
    {
        if (_canvasGroup == null) return;

        _canvasGroup.interactable = interactable;
        _canvasGroup.blocksRaycasts = interactable;
    }

    private void CheckOptions()
    {
        _frameRateSpinner.value =
            _frameRateSpinner.options.FindIndex(option =>
                option.text == PlayerPrefs.GetString(SELECTED_FPS_KEY, "60 FPS"));
    }

    private void ShowMain()
    {
        _mainMenuPanel.SetActive(true);
        _optionsPanel.SetActive(false);

        _btnBackToMain.gameObject.SetActive(false);
        
        // Siempre que vuelvo al main panel, fijo el botón inicial
        SetSelected(_mainFirstSelected ?? _btnPlay);
    }

    private void OnDropdownValueChanged(TMP_Dropdown dropdown)
    {
        string selectedFPSKey = dropdown.options[dropdown.value].text;
        Application.targetFrameRate = FPS[selectedFPSKey];
        Debug.Log("FPS SELECCIONADO " + selectedFPSKey);
    }
    
    private void OnPlayClicked()
    {
        // Bloquea interacción con toda la UI
        SetMenuInteractable(false);

        // Limpia el seleccionado para que no haya navegación posible
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);

        //TODO: ACA IMPLEMENTAR SI DAR PLAY A LA ANIMACION O SOLO PLANO DE CAMARA
        
        //if (!Save.IsTutorialSeen(INITIAL_TUTORIAL_ID))
        
        // Transición de escena
        Transition.FadeInAndLoadScene(2);
    }

    private void ShowOptions()
    {
        _mainMenuPanel.SetActive(false);
        _optionsPanel.SetActive(true);

        _btnBackToMain.gameObject.SetActive(true);
        
        // Siempre que abro Options, fijo el selectable inicial
        SetSelected(_optionsFirstSelected ?? _btnBackToMain);
    }

    private void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
    
    private static void SetSelected(Selectable selectable)
    {
        if (selectable == null) return;
        if (EventSystem.current == null) return;

        EventSystem.current.SetSelectedGameObject(selectable.gameObject);
    }
}