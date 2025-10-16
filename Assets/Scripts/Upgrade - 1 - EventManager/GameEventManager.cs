using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameEventManager : MonoBehaviour
{
    public static GameEventManager Instance;
    
    [Serializable]
    public struct BossEvents
    {
        public GameEvent OnBossDeath;
        public GameEvent OnStageChanged;
    }

    [Serializable]
    public struct PlayerEvents
    {
        public GameEvent OnBandageCount;
        public GameEvent OnDeath;
        public GameEvent OnRespawn;
    }

    [Header("Boss Events")] 
    [SerializeField] public BossEvents bossEvents;

    [Header("Player Events")] 
    [SerializeField] public PlayerEvents playerEvents;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}