using UnityEngine;

/// <summary>
/// Estadísticas de Fase: Almacena los multiplicadores de dificultad (velocidad y enfriamiento) 
/// que se aplican al Boss y sus habilidades a medida que avanza el combate.
/// </summary>

[CreateAssetMenu(fileName = "StageStats", menuName = "Boss/Stage Stats")]
public class StageStatsSO : ScriptableObject
{
    [Header("Multiplicadores")]
    [Range(0.1f, 5f)] public float speedMultiplier = 1f;
    [Range(0.1f, 5f)] public float cooldownMultiplier = 1f;
}
