using UnityEngine;
using static PlayerEnum;
using static PlayerEnum.PlayerSize;

/// <summary> 
/// Configuración de Movimiento: Define los parámetros de diseño (velocidad y rotación) para 
/// cada tamaño del jugador, permitiendo el balanceo de la movilidad desde el inspector. 
/// </summary>

[CreateAssetMenu(menuName = "MummysScape/MovementBySize")]
public sealed class MovementBySizeConfig : ScriptableObject
{
    [Header("Normal")] 
    public float normalMove = 5f;
    public float normalTurn = 25f;

    [Header("Small")]
    public float smallMove = 8f;
    public float smallTurn = 20f;

    [Header("Head")] 
    public float headMove = 5f;
    public float headTurn = 20f;   
    
    [Header("Empowered")] 
    public float empoweredMove = 1f;
    public float empoweredTurn = 5f;

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
            case Empowered:
                move = empoweredMove;
                turn = empoweredTurn;
                break;
            default:
                move = normalMove;
                turn = normalTurn;
                break;
        }
    }
}