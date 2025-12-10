using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class Save
{
    // =========================
    // TIER A — GENÉRICO (mínimo)
    // =========================
    private static void Set(string key, int v)        => PlayerPrefsManager.Set(key, v);
    private static void Set(string key, float v)      => PlayerPrefsManager.Set(key, v);
    private static void Set(string key, bool v)       => PlayerPrefsManager.SetBool(key, v);
    private static T    Get<T>(string key, T def = default) => PlayerPrefsManager.Get(key, def);

    // =========================
    // TIER B — SEMÁNTICO (esencial)
    // =========================
    // ---- Gemas (escena actual) ----
    public static void MarkGemPicked(int gemNum)
    {
        string key = PrefKeys.Gem(gemNum);
        
        if (Get(key, 0) != 0) return; // idempotente
        
        Set(key, true);
        Set(PrefKeys.GemTotal(), Get(PrefKeys.GemTotal(), 0) + 1);
    }
    
    public static bool WasGemPicked(int gemNum) => Get(PrefKeys.Gem(gemNum), 0) > 0;
    
    // ---- Niveles ----
    // Aviso
    public static void CompleteLevel(int buildIndex)
        => PlayerPrefsManager.SetBool(PrefKeys.LevelByIndex(buildIndex), true);

    // Consulto
    public static bool IsLevelCompleted(int buildIndex)
        => PlayerPrefsManager.GetBool(PrefKeys.LevelByIndex(buildIndex));
    
    // ---- Selector: progreso de OTRA escena por índice ----
    public static bool WasGemPickedInLevel(int gemNum, int buildIndex)
        => Get(PrefKeys.Gem(gemNum, SceneName(buildIndex)), 0) > 0;

    public static int GetGemTotalInLevel(int buildIndex)
        => Get(PrefKeys.GemTotal(SceneName(buildIndex)), 0);

    // ---- Volumen ----
    public static void  SetVolume(VolumeSoundId id, float v01) => Set(PrefKeys.VolumeSoundKey(id), v01);
    public static float GetVolume(VolumeSoundId id, float def = 0.8f) => Get(PrefKeys.VolumeSoundKey(id), def);

    public static void  SetVolume(VolumeFxId id, float v01) => Set(PrefKeys.VolumeFxKey(id), v01);
    public static float GetVolume(VolumeFxId id, float def = 0.8f) => Get(PrefKeys.VolumeFxKey(id), def);
    
    // ---- Mute ----
    public static void SetMuted(VolumeSoundId id, bool muted)
        => Set(PrefKeys.MuteSoundKey(id), muted);
    public static bool GetMuted(VolumeSoundId id, bool def = false)
        => Get(PrefKeys.MuteSoundKey(id), def);

    public static void SetMuted(VolumeFxId id, bool muted)
        => Set(PrefKeys.MuteFxKey(id), muted);
    public static bool GetMuted(VolumeFxId id, bool def = false)
        => Get(PrefKeys.MuteFxKey(id), def);


    // ---- Tiempos ----
    //public static void  SetTime(TimeId id, float seconds) => Set(PrefKeys.TimeKey(id), seconds);
    //public static float GetTime(TimeId id, float def = float.MaxValue) => Get(PrefKeys.TimeKey(id), def);

    // ---- Util interno ----
    private static string SceneName(int buildIndex)
    {
        var path = SceneUtility.GetScenePathByBuildIndex(buildIndex);
        return string.IsNullOrEmpty(path) ? "" : Path.GetFileNameWithoutExtension(path);
    }
}