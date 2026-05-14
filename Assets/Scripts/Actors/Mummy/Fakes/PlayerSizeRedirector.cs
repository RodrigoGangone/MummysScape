using System;
using static PlayerEnum;

public class PlayerSizeRedirector : IStateRedirector
{
    private readonly PlayerContext _ctx;

    public PlayerSizeRedirector(PlayerContext ctx) => _ctx = ctx;

    public Enum RedirectionCheck(Enum requestedState, Enum currentState)
    {
        // Si el estado solicitado es el mismo en el que ya estamos, 
        // no procesamos fakes ni cambiamos el AttemptedState.
        if (Equals(requestedState, currentState)) return requestedState;

        if (requestedState is PlayerStateId targetId)
        {
            // Solo evaluamos reglas de tamaño si es un cambio de estado real
            if (ShouldRedirectToFake(targetId))
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

    private bool ShouldRedirectToFake(PlayerStateId targetId)
    {
        var size = _ctx.Model.Size;

        return _ctx.Model.CanUseAbility(targetId)
               && !SizeRules.Can(size, targetId)
               && _ctx.Feedback != null
               && _ctx.Feedback.HasFeedback(targetId, size);
    }
}
