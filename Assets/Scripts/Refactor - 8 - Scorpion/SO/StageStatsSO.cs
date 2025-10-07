using UnityEngine;

/// <summary>
/// Stats por stage. Mantiene multiplicadores y parámetros que afectan a todo el boss
/// y a las habilidades (p.ej. multiplicador de cooldown).
/// </summary>
/// 


[CreateAssetMenu(fileName = "StageStats", menuName = "Boss/Stage Stats")]
public class StageStatsSO : ScriptableObject
{
    [Header("Multiplicadores")]
    [Range(0.1f, 5f)] public float speedMultiplier = 1f;
    [Range(0.1f, 5f)] public float cooldownMultiplier = 1f;
}
