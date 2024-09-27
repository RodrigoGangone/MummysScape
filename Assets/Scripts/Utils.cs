//Clase con variables y constantes que vamos a usar de forma repetida

public static class Utils
{
    public const string PLAYER_TAG = "PlayerFather";

    #region PlayerStates

        public const string STATE_WALK = "SM_Walk";
        public const string STATE_WALK_SAND = "SM_WalkSand";
        public const string STATE_IDLE = "SM_Idle";
        public const string STATE_SHOOT = "SM_Shoot";
        public const string STATE_PUSH = "SM_Push";
        public const string STATE_PULL = "SM_Pull";
        public const string STATE_FALL = "SM_Fall";
        public const string STATE_HOOK = "SM_Hook";
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

    #region PlayerPrefs

    public const string LEVEL_AT = "levelAt"; // Hasta que lvl llegue
    public const int LEVEL_FIRST = 1; // Num del 1er lvl en SceneBuild
    
    public const int FAKE_LOADING_TIME_SCENE = 3; //segs fake precarga del lvl seleccionado

    #endregion

}