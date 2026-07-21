using UnityEngine;
using System.Linq;

public class SelectorNavigationHandler : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Transform playerTransform;

    private void Start() => TryRepositionPlayer();
    
    private void TryRepositionPlayer()
    {
        int lastIndex = Save.GetLastLevelPlayed();
        if (lastIndex == -1) return;

        LevelTile[] sceneTiles = FindObjectsOfType<LevelTile>();

        LevelTile targetTile = sceneTiles.FirstOrDefault(t => t.BuildIndex == lastIndex);

        if (targetTile != null)
        {
            playerTransform.position = targetTile.PlayerPos.transform.position;
            playerTransform.forward = targetTile.PlayerPos.transform.forward;
            
            Debug.Log($"[Navigation] Player reposicionado en Nivel {lastIndex}");
        }
    }
}