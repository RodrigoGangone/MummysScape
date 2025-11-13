using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Android;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;
using static Utils;

public class MainMenu : MonoBehaviour
{
    //TODO: FALTA IMPLEMENTAR NUEVO SELECTOR DE NIVELES 
    //...

    [Header("PANEL MAIN MENU")] [SerializeField]
    private GameObject _mainMenuPanel;

    [SerializeField] private Button _btnPlay;
    [SerializeField] private Button _btnOptions;
    [SerializeField] private Button _btnExit;

    [Header("PANEL OPTIONS")] [SerializeField]
    private GameObject _optionsPanel;

    [SerializeField] private Button _btnDeletePrefs;
    [SerializeField] private TMP_Dropdown _frameRateSpinner;

    private static List<string> FrameRateText => new(FPS.Keys);

    [SerializeField] private Button[] _btnsLvls;

    [Header("PANEL CHARGE LEVEL")] [SerializeField]
    private GameObject _chargeLvlSelected;

    [SerializeField] private Button _btnBackToMain;

    private SceneTransitionManager Transition => GetComponent<SceneTransitionManager>();

    private DepthOfField _blur;
    private Volume _postProcess;

    private void Awake()
    {
        //Buttons Main//
        // MODIFICADO: Llama al script local _transition
        AddButtonProps(_btnPlay, () => Transition.FadeInAndLoadScene(1));
        AddButtonProps(_btnOptions, ShowOptions);
        AddButtonProps(_btnExit, QuitGame);

        //Buttons Options//
        _frameRateSpinner.AddOptions(FrameRateText);
        _frameRateSpinner.onValueChanged.AddListener(delegate { OnDropdownValueChanged(_frameRateSpinner); });

        //...

        AddButtonProps(_btnBackToMain, ShowMain);

        SetLevelsInButtons();
    }

    private void AddButtonProps(Button button, Action mainAction, params Action[] additionalActions)
    {
        button.onClick.AddListener(() =>
        {
            //AudioManager.Instance.PlaySFX(NameSounds.SFX_Click);

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
        //Activar Blur en la scene
        _postProcess = FindObjectOfType<Volume>();

        if (_postProcess.profile.TryGet(out _blur))
            _blur.active = !_blur.active;

        CheckOptions();

        // ELIMINADO: El SceneTransitionManager.Instance.TriggerFadeOut(null);
        // El script local SceneTransitionLocal lo hace solo en su propio Start().
    }

    private void CheckOptions()
    {
        _frameRateSpinner.value =
            _frameRateSpinner.options.FindIndex(option =>
                option.text == PlayerPrefs.GetString(SELECTED_FPS_KEY, "60 FPS"));
    }

    private void SetLevelsInButtons()
    {
        for (int i = 0; i < _btnsLvls.Length; i++)
        {
            int levelIndex = i + LEVEL_FIRST;
            _btnsLvls[i].onClick.AddListener(() => WhereGoLevelButtons(levelIndex));
        }
    }

    private void WhereGoLevelButtons(int levelIndex)
    {
        // MODIFICADO: Ya no mostramos el panel de carga fake
        //_chargeLvlSelected.SetActive(true); 
        _mainMenuPanel.SetActive(false);
        //_btnBackToMain.gameObject.SetActive(false);

        // MODIFICADO: Simplemente llamamos a la transición.
        // El 'FAKE_LOADING_TIME_SCENE' ya no es necesario.

        Transition.FadeInAndLoadScene(levelIndex);

        // ELIMINADO: StartCoroutine(LoadLevelWithPanel(levelIndex));
    }
    
    // ELIMINADO: LoadLevelAfterDelay

    private void ShowMain()
    {
        _mainMenuPanel.SetActive(true);
        _optionsPanel.SetActive(false);

        _btnBackToMain.gameObject.SetActive(false);
    }

    public void OnDropdownValueChanged(TMP_Dropdown dropdown)
    {
        string selectedFPSKey = dropdown.options[dropdown.value].text;
        Application.targetFrameRate = FPS[selectedFPSKey];
        Debug.Log("FPS SELECCIONADO " + selectedFPSKey);
    }

    private void ShowOptions()
    {
        _mainMenuPanel.SetActive(false);
        _optionsPanel.SetActive(true);

        _btnBackToMain.gameObject.SetActive(true);
    }

    private void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}