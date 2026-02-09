using System.Collections.Generic;
using System.Linq; // Necesario para .ToList()
using UnityEngine;

/// <summary>
/// Gestiona las solicitudes de bloqueo recibiendo (ID, bool).
/// </summary>
public class PlayerLock : MonoBehaviour
{
    // HashSet para la lógica (rápido, sin duplicados)
    private readonly HashSet<string> _activeLocks = new HashSet<string>();

    // VISTA DEBUG: Lista serializada solo para ver en el Inspector
    // [SerializeField] hace que Unity la muestre, aunque sea privada.
    [Header("Debug Info")]
    [SerializeField] private List<string> _inspectorLocks = new List<string>();

    private void OnEnable()
    {
        GameEventManager.Instance.playerEvents.OnLockRequested.Register<string, bool>(HandleLockRequest);
    }

    private void OnDisable()
    {
        if (GameEventManager.Instance != null)
        {
            GameEventManager.Instance.playerEvents.OnLockRequested.Unregister<string, bool>(HandleLockRequest);
        }
    }

    private void HandleLockRequest(string lockID, bool shouldLock)
    {
        bool changed = false; // Solo actualizamos la lista visual si hubo cambios reales

        if (shouldLock)
        {
            // Add devuelve true si el elemento NO existía y se agregó correctamente
            if (_activeLocks.Add(lockID)) 
                changed = true;
        }
        else
        {
            // Remove devuelve true si el elemento existía y se borró
            if (_activeLocks.Remove(lockID)) 
                changed = true;
        }

        // Si la colección cambió, actualizamos la parte visual y notificamos
        if (changed)
        {
            UpdateInspectorList();
            EvaluateAndBroadcast();
        }
    }

    // Sincroniza el HashSet con la Lista del inspector
    private void UpdateInspectorList()
    {
        // Limpiamos la lista vieja y copiamos los valores actuales del HashSet
        _inspectorLocks.Clear();
        _inspectorLocks.AddRange(_activeLocks);
        
        // Opcional: Si quieres verlos ordenados alfabéticamente para leer mejor:
        // _inspectorLocks.Sort(); 
    }

    private void EvaluateAndBroadcast()
    {
        bool isLocked = _activeLocks.Count > 0;
        GameEventManager.Instance.playerEvents.OnLocked.Raise(isLocked);
    }
}