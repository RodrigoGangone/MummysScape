using System;
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
        public GameEvent OnBandagesCountChanged;
        public GameEvent OnSizeChanged;
    }

    [Serializable]
    public struct LevelEvents
    {
        public GameEvent OnWin;
        public GameEvent OnDeath;
        public GameEvent OnPauseChanged; // bool: true = pausa, false = resume
        public GameEvent OnRespawn;
        public GameEvent OnPickedGem;
    }

    [Header("Boss Events")] [SerializeField]
    public BossEvents bossEvents;

    [Header("Player Events")] [SerializeField]
    public PlayerEvents playerEvents;

    [Header("Level Events")] [SerializeField]
    public LevelEvents levelEvents;

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