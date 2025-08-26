using static PlayerEnum;
using static PlayerEnum.PlayerActionId;
using static PlayerEnum.PlayerSize;

/// <summary>
/// SizeRules
/// Fuente única de permisos por tamaño.
/// </summary>

public class SizeRules
{
    public static bool Can(PlayerSize s, PlayerActionId a) => s switch
    {
        Normal => a is Shoot or DropBandage or Push or Attract,
        Small  => a is Shoot or DropBandage or Swing,
        Head   => a is Smash,
        _ => false
    };
}
