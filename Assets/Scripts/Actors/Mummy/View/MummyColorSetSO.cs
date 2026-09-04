using UnityEngine;

[CreateAssetMenu(fileName = "NewMummyColorSet", menuName = "Mummy's Colors/Color Set")]
public class MummyColorSetSO : ScriptableObject
{
    //[Header("Craneo (Standard Material)")]
    //public Color craneoAlbedo = Color.white;

    [Header("Shader Fuego 1 (Standard Colors)")]
    
    public Color fire1Bottom = Color.red;
    public Color fire1Mid = Color.yellow;
    public Color fire1Top = Color.white;

    [Header("Shader Fuego 2 (HDR Colors)")]
    
    [ColorUsage(true, true)] public Color fire2Bottom = Color.red;
    [ColorUsage(true, true)] public Color fire2Mid = Color.yellow;
    [ColorUsage(true, true)] public Color fire2Top = Color.white;
    [ColorUsage(true, true)] public Color fire2Glow = Color.magenta;

    [ColorUsage(false, true)] public Color skull;
}