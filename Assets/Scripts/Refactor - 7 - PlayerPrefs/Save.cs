using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class Save
{
    // =========================
    // TIER A — GENÉRICO (mínimo)
    // =========================
    public static void Set(string key, int v)        => PlayerPrefsManager.Set(key, v);
    public static void Set(string key, float v)      => PlayerPrefsManager.Set(key, v);
    public static void Set(string key, bool v)       => PlayerPrefsManager.SetBool(key, v);
    public static T    Get<T>(string key, T def = default) => PlayerPrefsManager.Get(key, def);

    // =========================
    // TIER B — SEMÁNTICO (esencial)
    // =========================
    // ---- Gemas (escena actual) ----
    public static void MarkGemPicked(int gemNum)
    {
        string key = PrefKeys.Gem(gemNum);
        if (Get(key, 0) == 0) // idempotente
        {
            Set(key, true);
            Set(PrefKeys.GemTotal(), Get(PrefKeys.GemTotal(), 0) + 1);
        }
    }
    public static bool WasGemPicked(int gemNum) => Get(PrefKeys.Gem(gemNum), 0) > 0;
    public static int  GetSceneGemTotal()       => Get(PrefKeys.GemTotal(), 0);

    // ---- Niveles (por índice) ----
    public static void MarkLevelComplete(int buildIndex) => Set(PrefKeys.LevelByIndex(buildIndex), true);
    public static bool IsLevelDoneByIndex(int buildIndex) => Get(PrefKeys.LevelByIndex(buildIndex), false);

    // Desbloqueo simple: 0 desbloqueado; resto si el anterior está completo o él mismo.
    public static bool IsLevelUnlockedByIndex(int buildIndex)
        => buildIndex <= 0 || IsLevelDoneByIndex(buildIndex - 1) || IsLevelDoneByIndex(buildIndex);

    // ---- Selector: progreso de OTRA escena por índice ----
    public static bool WasGemPickedInLevel(int gemNum, int buildIndex)
        => Get(PrefKeys.Gem(gemNum, SceneName(buildIndex)), 0) > 0;

    public static int GetGemTotalInLevel(int buildIndex)
        => Get(PrefKeys.GemTotal(SceneName(buildIndex)), 0);

    // ---- Volumen (tipado con enums) ----
    public static void  SetVolume(VolumeSoundId id, float v01) => Set(PrefKeys.VolumeSoundKey(id), v01);
    public static float GetVolume(VolumeSoundId id, float def = 0.8f) => Get(PrefKeys.VolumeSoundKey(id), def);

    public static void  SetVolume(VolumeFxId id, float v01) => Set(PrefKeys.VolumeFxKey(id), v01);
    public static float GetVolume(VolumeFxId id, float def = 0.8f) => Get(PrefKeys.VolumeFxKey(id), def);

    // ---- Tiempos ----
    public static void  SetTime(TimeId id, float seconds) => Set(PrefKeys.TimeKey(id), seconds);
    public static float GetTime(TimeId id, float def = float.MaxValue) => Get(PrefKeys.TimeKey(id), def);

    // util interno
    static string SceneName(int buildIndex)
    {
        var path = SceneUtility.GetScenePathByBuildIndex(buildIndex);
        if (string.IsNullOrEmpty(path)) return "";
        return Path.GetFileNameWithoutExtension(path);
    }
}
