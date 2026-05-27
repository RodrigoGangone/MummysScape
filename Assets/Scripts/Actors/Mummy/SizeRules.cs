using static PlayerEnum;
using static PlayerEnum.PlayerStateId;
using static PlayerEnum.PlayerSize;

/// <summary> 
/// Matriz de Habilidades: Define de forma estática y centralizada qué estados de la FSM son 
/// válidos para cada tamaño del personaje (Normal, Small, Head). 
/// </summary>

public class SizeRules
{
    public static bool Can(PlayerSize s, PlayerStateId  a) => s switch
    {
        Normal => a is Idle or Walk or Aim or Shoot or DropBandage or Push or Attract or Fall or KnockBack or Fake or Dead or Win,
        Small  => a is Idle or Walk or Aim or Shoot or DropBandage or Swing or Fall or KnockBack or Fake or Dead or Win,
        Head   => a is Idle or Walk or Smash or QuickTravel or Fall or KnockBack or Fake or Dead or Win,
        _ => false
    };
}