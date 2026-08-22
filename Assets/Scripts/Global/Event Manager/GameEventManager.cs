using System;
using UnityEngine;

/// <summary> 
/// Repositorio Global de Eventos: Singleton persistente que centraliza todas las instancias de 
/// GameEvents del proyecto, organizándolas en categorías lógicas (Boss, Player, Level). 
/// </summary>

public class GameEventManager : MonoBehaviour
{
    public static GameEventManager Instance;

    [Serializable]
    public struct BossEvents
    {
        public GameEvent OnDamaged;
        public GameEvent OnDeath;
        public GameEvent OnStageCompleted;
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
        public GameEvent OnWin;
    }

    [Serializable]
    public struct LevelEvents
    {
        public GameEvent OnWin;
        public GameEvent OnDeath;
        public GameEvent OnRequestBandageSpawn;
        public GameEvent OnContextUIChanged;
        public GameEvent OnPauseChanged;
        public GameEvent OnRespawn;
        public GameEvent OnPickedGem;
        public GameEvent OnRumbleLow;
        public GameEvent OnRumbleHigh;
        public GameEvent OnCinematicToggled;
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