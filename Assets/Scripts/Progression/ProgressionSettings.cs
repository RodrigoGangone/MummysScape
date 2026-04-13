using System;
using System.Collections.Generic;
using UnityEngine;
using static PlayerEnum;

[CreateAssetMenu(menuName = "Progression/Settings")]
public class ProgressionSettings : ScriptableObject
{
    [Serializable]
    public struct AbilityRequirement
    {
        public PlayerStateId state;
        public TutorialID tutorial; // <--- CAMBIO: Ahora es el Enum generado
    }

    [SerializeField] private List<AbilityRequirement> requirements;

    public bool IsUnlocked(PlayerStateId state)
    {
        var req = requirements.Find(r => r.state == state);
        
        // Si el requisito es 'None' o no está en la lista, la habilidad es básica
        if (req.tutorial == TutorialID.None) return true;

        // Consultamos al sistema de Save usando la sobrecarga del Enum
        return Save.IsTutorialSeen(req.tutorial);
    }
}