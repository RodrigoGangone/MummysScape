using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Gestiona las solicitudes de bloqueo. 
/// Prioridad de ejecución configurada: Después de GameEventManager.
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
            
            // Suscripción al evento de carga de escena para limpieza total
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // Se ejecuta apenas la nueva escena está lista
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ClearAllLocks();
        Debug.Log($"[PlayerLock] Escena '{scene.name}' limpia. HashSet reseteado.");
    }

    private void OnEnable()
    {
        // Gracias a la prioridad de ejecución, GameEventManager.Instance ya es válido aquí
        GameEventManager.Instance.playerEvents.OnLockRequested.Register<string, bool>(HandleLockRequest);
    }

    private void OnDisable()
    {
        if (GameEventManager.Instance != null)
            GameEventManager.Instance.playerEvents.OnLockRequested.Unregister<string, bool>(HandleLockRequest);
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
}