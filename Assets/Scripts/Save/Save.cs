using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class Save
{
    private const string GLOBAL_ID = "Global";

    #region TIER A: Core Wrappers (Genérico)
    // Envolturas privadas para unificar el acceso a PlayerPrefsManager
    private static void Set(string key, int value)      => PlayerPrefsManager.Set(key, value);
    private static void Set(string key, float value)    => PlayerPrefsManager.Set(key, value);
    private static void Set(string key, bool value)     => PlayerPrefsManager.SetBool(key, value);
    private static T    Get<T>(string key, T def = default) => PlayerPrefsManager.Get(key, def);
    #endregion

    #region TIER B: Lógica de Gemas
    
    // --- Escritura ---
    public static void MarkGemPicked(int gemNum)
    {
        // 1. Verificar si ya se recogió para evitar duplicados
        string gemKey = PrefKeys.Gem(gemNum);
        if (Get(gemKey, 0) != 0) return; 
        
        // 2. Marcar gema individual
        Set(gemKey, true);
        
        // 3. Sumar al total del NIVEL ACTUAL
        string levelKey = PrefKeys.GemTotal(); 
        Set(levelKey, Get(levelKey, 0) + 1);

        // 4. Sumar al total GLOBAL
        string globalKey = PrefKeys.GemTotal(GLOBAL_ID);
        Set(globalKey, Get(globalKey, 0) + 1);
    }

    // --- Lectura ---
    public static bool WasGemPicked(int gemNum) 
        => Get(PrefKeys.Gem(gemNum), 0) > 0;
    
    public static int GetGlobalGemCount() 
        => Get(PrefKeys.GemTotal(GLOBAL_ID), 0);

    // --- Consultas Cruzadas (Otros Niveles) ---
    public static bool WasGemPickedInLevel(int gemNum, int buildIndex)
        => Get(PrefKeys.Gem(gemNum, SceneName(buildIndex)), 0) > 0;

    public static int GetGemTotalInLevel(int buildIndex)
        => Get(PrefKeys.GemTotal(SceneName(buildIndex)), 0);
    
    #endregion

    #region TIER B: Progresión de Niveles

    public static void CompleteLevel(int buildIndex)
        => Set(PrefKeys.LevelByIndex(buildIndex), true);

    public static bool IsLevelCompleted(int buildIndex)
        => Get<bool>(PrefKeys.LevelByIndex(buildIndex)); // El Get genérico maneja el bool

    #endregion

    #region TIER B: Configuración de Audio (Volumen & Mute)

    // ---- Volumen ----
    public static void SetVolume(VolumeSoundId id, float v01) => Set(PrefKeys.VolumeSoundKey(id), v01);
    public static float GetVolume(VolumeSoundId id, float def = 0.8f) => Get(PrefKeys.VolumeSoundKey(id), def);

    public static void SetVolume(VolumeFxId id, float v01) => Set(PrefKeys.VolumeFxKey(id), v01);
    public static float GetVolume(VolumeFxId id, float def = 0.8f) => Get(PrefKeys.VolumeFxKey(id), def);

    // ---- Mute ----
    // Nota: Usamos Set/Get genéricos que ya derivan a PlayerPrefsManager.SetBool/GetBool internamente
    public static void SetMuted(VolumeSoundId id, bool muted) => Set(PrefKeys.MuteSoundKey(id), muted);
    public static bool GetMuted(VolumeSoundId id, bool def = false) => Get(PrefKeys.MuteSoundKey(id), def);

    public static void SetMuted(VolumeFxId id, bool muted) => Set(PrefKeys.MuteFxKey(id), muted);
    public static bool GetMuted(VolumeFxId id, bool def = false) => Get(PrefKeys.MuteFxKey(id), def);

    #endregion
    
    #region TIER C: Estado 'Seen' (Tutoriales y Revelaciones)

    public static void MarkAsSeen(string key) => Set(key, 1); // Guardamos como 1 (true)
    public static bool IsSeen(string key) => Get(key, 0) == 1;

    // Wrappers específicos
    public static void MarkTutorialSeen(string tutorialId) => MarkAsSeen(PrefKeys.SeenTutorial(tutorialId));
    public static bool IsTutorialSeen(string tutorialId)   => IsSeen(PrefKeys.SeenTutorial(tutorialId));

    public static void MarkLevelRevealSeen(int index) => MarkAsSeen(PrefKeys.SeenLevelReveal(index));
    public static bool IsLevelRevealSeen(int index)   => IsSeen(PrefKeys.SeenLevelReveal(index));
    public static void MarkZoneRevealSeen(int index) => MarkAsSeen(PrefKeys.SeenZoneReveal(index));
    public static bool IsZoneRevealSeen(int index)   => IsSeen(PrefKeys.SeenZoneReveal(index));
    public static int GetSeenGemsCount() => Get(PrefKeys.SeenGemsCount(), 0);
    public static void UpdateSeenGemsCount(int count) => Set(PrefKeys.SeenGemsCount(), count);

    #endregion

    #region Helpers Internos

    private static string SceneName(int buildIndex)
    {
        var path = SceneUtility.GetScenePathByBuildIndex(buildIndex);
        return string.IsNullOrEmpty(path) ? "" : Path.GetFileNameWithoutExtension(path);
    }

    #endregion
}