using System.Collections.Generic;
using UnityEngine;

public class LevelRevealManager : MonoBehaviour
{
    public static LevelRevealManager Instance { get; private set; }

    [Header("Configuración de Niveles")]
    [Tooltip("Arrastrá acá los LevelTiles EN ORDEN (Nivel 1, Nivel 2, etc.)")]
    [SerializeField] private LevelTile[] allTiles;

    private Queue<LevelTile> _revealQueue = new Queue<LevelTile>();
    private bool _isPlaying = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        // El Manager toma el control: evalúa todos los tiles en orden estricto
        InitializeAllTiles();
    }

    private void InitializeAllTiles()
    {
        if (allTiles == null || allTiles.Length == 0)
        {
            Debug.LogWarning("[LevelRevealManager] No hay tiles asignados en el array.");
            return;
        }

        foreach (var tile in allTiles)
        {
            // Si el tile responde que necesita revelar su cinemática, lo encolamos
            if (tile.EvaluateStateAndCheckReveal())
            {
                _revealQueue.Enqueue(tile);
            }
        }

        // Si hay elementos en la cola, arrancamos la secuencia
        if (_revealQueue.Count > 0 && !_isPlaying)
        {
            ProcessNextReveal();
        }
    }

    private void ProcessNextReveal()
    {
        if (_revealQueue.Count == 0)
        {
            _isPlaying = false;
            return; 
        }

        _isPlaying = true;
        LevelTile nextTile = _revealQueue.Dequeue();
        
        nextTile.PlayRevealSequence(() => 
        {
            ProcessNextReveal();
        });
    }
}