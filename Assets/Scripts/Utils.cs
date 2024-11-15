//Clase con variables y constantes que vamos a usar de forma repetida

using System.Collections.Generic;

public static class Utils
{
    internal const string PLAYER_TAG = "PlayerFather";
    internal const string PLAYER_SMASH_TAG = "Smash";

    #region PlayerStates

    internal const string STATE_WALK = "SM_Walk";
    internal const string STATE_WALK_SAND = "SM_WalkSand";
    internal const string STATE_IDLE = "SM_Idle";
    internal const string STATE_SHOOT = "SM_Shoot";
    internal const string STATE_PUSH = "SM_Push";
    internal const string STATE_PULL = "SM_Pull";
    internal const string STATE_FALL = "SM_Fall";
    internal const string STATE_HOOK = "SM_Hook";
    internal const string STATE_SMASH = "SM_Smash";
    internal const string STATE_WIN = "SM_Win";
    internal const string STATE_DEAD = "SM_Dead";
    internal const string STATE_DAMAGE = "SM_Damage";
    internal const string STATE_DROP = "SM_Drop";
    internal const string NO_STATE = "No hay estado";

    #endregion

    #region Box

    internal const string BOX_SIDE_FORWARD = "Forward";
    internal const string BOX_SIDE_BACKWARD = "Backward";
    internal const string BOX_SIDE_LEFT = "Left";
    internal const string BOX_SIDE_RIGHT = "Right";

    #endregion

    #region Levels

    internal const int MAX_LVLS = 6; //Cantidad Maxima de lvls actualmente
    internal const string LEVEL_AT = "levelAt"; // Hasta que lvl llegue
    internal const int LEVEL_FIRST = 1; // Num del 1er lvl en SceneBuild
    internal const int FAKE_LOADING_TIME_SCENE = 3; //segs fake precarga del lvl seleccionado

    #endregion

    #region Options

    internal const string SELECTED_FPS_KEY = "SelectedFPS";
    
    private const int FPS_30_VALUE = 30;
    private const int FPS_60_VALUE = 60;
    private const int FPS_75_VALUE = 75;
    private const int FPS_120_VALUE = 120;
    private const int FPS_144_VALUE = 144;

    private const string FPS_30_KEY = "30 FPS";
    private const string FPS_60_KEY = "60 FPS";
    private const string FPS_75_KEY = "75 FPS";
    private const string FPS_120_KEY = "120 FPS";
    private const string FPS_144_KEY = "144 FPS";

    internal static readonly Dictionary<string, int> FPS = new()
    {
        { FPS_30_KEY, FPS_30_VALUE },
        { FPS_60_KEY, FPS_60_VALUE },
        { FPS_75_KEY, FPS_75_VALUE },
        { FPS_120_KEY, FPS_120_VALUE },
        { FPS_144_KEY, FPS_144_VALUE }
    };

    #endregion

    #region ShadersVar

    #region Bandage

    public const string RIGHT_THRESHOLD = "_rightThreshold";

    #endregion

    #endregion

    #region Sounds

    //Const para PlayerPref
    internal const string MUSIC_VOLUME = "MusicVolume";
    internal const string SFX_VOLUME = "SFXVolume";
    
    //Audio Mixer public vars
    internal const string AUDIO_MIXER_MUSIC = "Music";
    internal const string AUDIO_MIXER_SFX = "SFX";

    #endregion
}