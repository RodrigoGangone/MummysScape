using UnityEngine;
using static PlayerEnum;
using static PlayerEnum.PlayerSize;

/// <summary>
/// MovementBySizeConfig (SO)
/// Tabla de velocidades por tamaño (diseño).
/// </summary>
[CreateAssetMenu(menuName = "MummysScape/MovementBySize")]
public sealed class MovementBySizeConfig : ScriptableObject
{
    [Header("Normal")] 
    public float normalMove = 5f;
    public float normalTurn = 14f;

    [Header("Small")]
    public float smallMove = 7f;
    public float smallTurn = 16f;

    [Header("Head")] 
    public float headMove = 5f;
    public float headTurn = 14f;

    public void Get(PlayerSize size, out float move, out float turn)
    {
        switch (size)
        {
            case Small:
                move = smallMove;
                turn = smallTurn;
                break;
            case Head:
                move = headMove;
                turn = headTurn;
                break;
            default:
                move = normalMove;
                turn = normalTurn;
                break;
        }
    }
}