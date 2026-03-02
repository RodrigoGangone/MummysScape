using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary> 
/// Gestor de Bloqueos: Singleton que administra un conjunto de llaves (Lock IDs) para deshabilitar al 
/// jugador de forma acumulativa, asegurando que el control solo vuelva cuando todos los sistemas lo liberen. 
/// </summary>

public class PlayerLock : MonoBehaviour
{
    public static PlayerLock Instance;
    private readonly HashSet<string> _activeLocks = new HashSet<string>();

    [Header("Debug Info")]
    [SerializeField] private List<string> _inspectorLocks = new List<string>();

    public bool IsLocked => _activeLocks.Count > 0;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
            Destroy(gameObject);
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ClearAllLocks();
        Debug.Log($"[PlayerLock] Escena '{scene.name}' limpia. HashSet reseteado.");
    }
    
    private void HandleLockRequest(string lockID, bool shouldLock)
    {
        bool changed = false;
        if (shouldLock) { if (_activeLocks.Add(lockID)) changed = true; }
        else { if (_activeLocks.Remove(lockID)) changed = true; }

        if (changed)
        {
            UpdateInspectorList();
            EvaluateAndBroadcast();
        }
    }

    public void ClearAllLocks()
    {
        _activeLocks.Clear();
        UpdateInspectorList();
        EvaluateAndBroadcast();
    }

    private void UpdateInspectorList()
    {
        _inspectorLocks.Clear();
        _inspectorLocks.AddRange(_activeLocks);
    }

    private void EvaluateAndBroadcast()
    {
        GameEventManager.Instance.playerEvents.OnLocked.Raise(IsLocked);
    }
    
    private void OnEnable() => GameEventManager.Instance.playerEvents.OnLockRequested.Register<string, bool>(HandleLockRequest);
    private void OnDisable() => GameEventManager.Instance.playerEvents.OnLockRequested.Unregister<string, bool>(HandleLockRequest);

}