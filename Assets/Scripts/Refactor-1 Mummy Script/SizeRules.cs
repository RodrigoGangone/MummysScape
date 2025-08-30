using static PlayerEnum;
using static PlayerEnum.PlayerStateId;
using static PlayerEnum.PlayerSize;

/// <summary>
/// SizeRules
/// Fuente única de permisos por tamaño.
/// </summary>

public class SizeRules
{
    public static bool Can(PlayerSize s, PlayerStateId  a) => s switch
    {
        Normal => a is Idle or Walk or Shoot or DropBandage or Push or Attract or Fall or Dead,
        Small  => a is Idle or Walk or Shoot or DropBandage or Swing or Fall or Dead,
        Head   => a is Idle or Walk or Smash or Fall or Dead,
        _ => false
    };
}
