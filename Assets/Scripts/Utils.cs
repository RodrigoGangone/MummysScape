//Clase con variables y constantes que vamos a usar de forma repetida

using System;
using UnityEngine;

public static class Utils
{
    public const string STATE_WALK = "SM_Walk";
    public const string STATE_IDLE = "SM_Idle";
    public const string STATE_SHOOT = "SM_Shoot";
    public const string STATE_PUSH = "SM_Push";
    public const string STATE_PULL = "SM_Pull";
    public const string STATE_FALL = "SM_Fall";
    public const string STATE_HOOK = "SM_Hook";
    public const string STATE_WIN = "SM_Win";
    public const string STATE_DEAD = "SM_Dead";
    public const string STATE_DAMAGE = "SM_Damage";
    public const string NO_STATE = "No hay estado";

    public const string BOX_SIDE_FORWARD = "Forward";
    public const string BOX_SIDE_BACKWARD = "Backward";
    public const string BOX_SIDE_LEFT = "Left";
    public const string BOX_SIDE_RIGHT = "Right";
}