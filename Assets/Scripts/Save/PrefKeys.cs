using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine.SceneManagement;
using static PrefFamily;

//

public enum PrefFamily { Gems, GemTotals, LevelCompleted, Time, VolumeSound, VolumeFX, Seen }
public enum VolumeSoundId { Master, Music, Voice, Ambient }
public enum VolumeFxId    { Sfx, UI }
public enum TimeId { Best, Last, Total } // agrega los que necesites

public static class PrefKeys
{
    public static readonly Dictionary<PrefFamily, string> Prefix = new()
    {
        { Gems,           "gem." },
        { GemTotals,      "gemTotal." },
        { LevelCompleted, "level.completed." },
        { Time,           "time." }, 
        { VolumeSound,    "volume.sound." },
        { VolumeFX,       "volume.fx." },
        { Seen,           "seen." } // <--- NUEVO
    };

    static string Scene
    {
        get
        {
            // Usamos el 'path' de la escena activa y extraemos su nombre de archivo
            // Esto es más robusto que '.name'
            var path = SceneManager.GetActiveScene().path;
            return string.IsNullOrEmpty(path) ? "" : Path.GetFileNameWithoutExtension(path);
        }
    }
    
    static string Slug<TEnum>(TEnum e) where TEnum : Enum => e.ToString().ToLowerInvariant();
    // --- Gems / Level ---
    public static string Gem(int gemNum, string scene = null)
        => $"{Prefix[Gems]}{gemNum}.{(scene ?? Scene)}";
    public static string GemTotal(string scene = null)
        => $"{Prefix[GemTotals]}{(scene ?? Scene)}";
    public static string LevelByIndex(int index)
        => $"{Prefix[LevelCompleted]}index.{index}";

    // --- Volumen (tipado, sin strings) ---
    public static string VolumeSoundKey(VolumeSoundId id)
        => $"{Prefix[VolumeSound]}{Slug(id)}";
    public static string VolumeFxKey(VolumeFxId id)
        => $"{Prefix[VolumeFX]}{Slug(id)}";
    
    // --- Mute (booleans) ---
    // reutilizamos los prefijos existentes y les colgamos un sufijo ".mute"
    public static string MuteSoundKey(VolumeSoundId id)
        => $"{Prefix[VolumeSound]}{Slug(id)}.mute";
    public static string MuteFxKey(VolumeFxId id)
        => $"{Prefix[VolumeFX]}{Slug(id)}.mute";
    
    public static string SeenTutorial(string tutorialId) => $"{Prefix[Seen]}tutorial.{tutorialId}";
    public static string SeenLevelReveal(int buildIndex) => $"{Prefix[Seen]}level_reveal.{buildIndex}";
    public static string SeenZoneReveal(int buildIndex)  => $"{Prefix[Seen]}zone_reveal.{buildIndex}";
    public static string SeenGemsCount() => $"{Prefix[Seen]}gems_count";
    //public static string TimeKey(TimeId id) => $"{Prefix[Time]}{id.ToString().ToLowerInvariant()}";
}
