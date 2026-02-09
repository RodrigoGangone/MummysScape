using System;
using UnityEngine;
using UnityEngine.Serialization;

public class GameEventManager : MonoBehaviour
{
    public static GameEventManager Instance;

    [Serializable]
    public struct BossEvents
    {
        public GameEvent OnStageChanged;
        public GameEvent OnDeath;
    }

    [Serializable]
    public struct PlayerEvents
    {
        public GameEvent OnBandagesCountChanged;
        public GameEvent OnSizeChanged;
        public GameEvent OnShoot;
        public GameEvent OnHit;
        public GameEvent OnLocked;
        public GameEvent OnLockRequested;
    }

    [Serializable]
    public struct LevelEvents
    {
        public GameEvent OnWin;
        public GameEvent OnDeath;
        public GameEvent OnPrompt;
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