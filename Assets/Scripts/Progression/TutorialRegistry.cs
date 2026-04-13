using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Mummy/Progression/Tutorial Registry")]
public class TutorialRegistry : ScriptableObject
{
    [Header("Listado Maestro de Tutoriales")]
    [Tooltip("Agregá acá los nombres internos (ej: Swing, Push, Smash)")]
    public List<string> tutorialIds = new List<string>();
}