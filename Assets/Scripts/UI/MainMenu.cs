using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
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

    private static List<string> FrameRateText => new(FPS.Keys);

    [SerializeField] private Button _btnBackToMain;

    private SceneTransitionManager Transition => GetComponent<SceneTransitionManager>();

    private DepthOfField _blur;
    private Volume _postProcess;

    private void Awake()
    {
        AddButtonProps(_btnPlay, () => Transition.FadeInAndLoadScene(1));
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
        _postProcess = FindObjectOfType<Volume>();

        if (_postProcess.profile.TryGet(out _blur))
            _blur.active = !_blur.active;

        CheckOptions();
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
    }

    private void OnDropdownValueChanged(TMP_Dropdown dropdown)
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