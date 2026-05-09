using System;
using static PlayerEnum;

public class PlayerSizeRedirector : IStateRedirector
{
    private PlayerContext _ctx;

    public PlayerSizeRedirector(PlayerContext ctx) => _ctx = ctx;

    public Enum RedirectionCheck(Enum requestedState, Enum currentState)
    {
        // Si el estado solicitado es el mismo en el que ya estamos, 
        // no procesamos fakes ni cambiamos el AttemptedState.
        if (Equals(requestedState, currentState)) return requestedState;

        if (requestedState is PlayerStateId targetId)
        {
            // Solo evaluamos reglas de tamaño si es un cambio de estado real
            if (!SizeRules.Can(_ctx.Model.Size, targetId))
            {
                // Si ya estamos en Fake y la intención es la misma, no hace falta re-asignar
                if (Equals(currentState, PlayerStateId.Fake) && _ctx.AttemptedState == targetId)
                    return PlayerStateId.Fake;

                _ctx.AttemptedState = targetId;
                return PlayerStateId.Fake;
            }
        }
        return requestedState;
    }
}