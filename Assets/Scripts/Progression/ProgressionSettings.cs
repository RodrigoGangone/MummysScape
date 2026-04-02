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
        public string tutorialId; // El ID que usás en TutorialFocusPoint y Save
    }

    [SerializeField] private List<AbilityRequirement> requirements;

    public bool IsUnlocked(PlayerStateId state)
    {
        var req = requirements.Find(r => r.state == state);
        
        // Si no hay requisito definido, la habilidad es básica
        if (string.IsNullOrEmpty(req.tutorialId)) return true;

        // Consultamos a tu sistema de persistencia
        return Save.IsTutorialSeen(req.tutorialId);
    }
}