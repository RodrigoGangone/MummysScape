/// <summary>
/// Enums base del Player:
/// - PlayerSize: tamaño según cantidad de vendas.
/// - PlayerStateId: IDs para tu StateMachine.
/// - PlayerActionId: acciones disponibles (validables por SizeRules).
/// </summary>

public static class PlayerEnum
{
    public enum PlayerSize { Normal, Small, Head }
    public enum PlayerStateId { Idle, Walk, Fall, Aim, Shoot, Smash, DropBandage, Push, Attract, Swing, QuickTravel, KnockBack, Dead, Win }
}
