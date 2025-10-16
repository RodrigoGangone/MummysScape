using System;

/// <summary>
/// IStateTransitionGuard
/// Contrato para validar si puede ocurrir una transición de estado A -> B.
/// </summary>
public interface IStateTransitionGuard
{
    bool Can(Enum from, Enum to);
}