//Clase con variables y constantes que vamos a usar de forma repetida

using System.Collections.Generic;

public static class Utils
{
    public const string PLAYER_TAG = "PlayerFather";
    public const string PLAYER_SMASH_TAG = "Smash";

    #region PlayerStates

    public const string STATE_WALK = "SM_Walk";
    public const string STATE_WALK_SAND = "SM_WalkSand";
    public const string STATE_IDLE = "SM_Idle";
    public const string STATE_SHOOT = "SM_Shoot";
    public const string STATE_PUSH = "SM_Push";
    public const string STATE_PULL = "SM_Pull";
    public const string STATE_FALL = "SM_Fall";
    public const string STATE_HOOK = "SM_Hook";
    public const string STATE_SMASH = "SM_Smash";
    public const string STATE_WIN = "SM_Win";
    public const string STATE_DEAD = "SM_Dead";
    public const string STATE_DAMAGE = "SM_Damage";
    public const string STATE_DROP = "SM_Drop";
    public const string NO_STATE = "No hay estado";

    #endregion

    #region Box

    public const string BOX_SIDE_FORWARD = "Forward";
    public const string BOX_SIDE_BACKWARD = "Backward";
    public const string BOX_SIDE_LEFT = "Left";
    public const string BOX_SIDE_RIGHT = "Right";

    #endregion

    #region Levels

    public const int MAX_LVLS = 6; //Cantidad Maxima de lvls actualmente
    public const string LEVEL_AT = "levelAt"; // Hasta que lvl llegue
    public const int LEVEL_FIRST = 1; // Num del 1er lvl en SceneBuild
    public const int FAKE_LOADING_TIME_SCENE = 3; //segs fake precarga del lvl seleccionado

    #endregion

    #region Options

    private const int FPS_5_VALUE = 5; //TODO: PARA TEST, DESPUES BORRARLO
    private const int FPS_30_VALUE = 30;
    private const int FPS_60_VALUE = 60;
    private const int FPS_75_VALUE = 75;
    private const int FPS_120_VALUE = 120;
    private const int FPS_144_VALUE = 144;

    private const string FPS_5_KEY = "5 FPS"; //TODO: PARA TEST, DESPUES BORRARLO
    private const string FPS_30_KEY = "30 FPS";
    private const string FPS_60_KEY = "60 FPS";
    private const string FPS_75_KEY = "75 FPS";
    private const string FPS_120_KEY = "120 FPS";
    private const string FPS_144_KEY = "144 FPS";

    public static readonly Dictionary<string, int> FPS = new()
    {
        { FPS_5_KEY, FPS_5_VALUE }, //TODO: PARA TEST, DESPUES BORRARLO
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
}