using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [SerializeField] private Text _textTimeDeath;
    private Player _player;
    private LevelManager _levelManager;

    void Start()
    {
        _levelManager = FindObjectOfType<LevelManager>();
        _player = FindObjectOfType<Player>();
    }

    public void UISetTimerDeath(float time)
    {
        _textTimeDeath.text = "TIEMPO HASTA MORIRSE" + time;
    }
}