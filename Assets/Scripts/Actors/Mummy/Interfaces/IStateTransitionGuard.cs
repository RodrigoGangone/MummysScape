using System;

/// <summary> 
/// Validador de Transiciones: Define el contrato para implementar lógica de seguridad que 
/// permita o deniegue el cambio entre estados específicos de la máquina de estados.
/// </summary>

public interface IStateTransitionGuard
{
    bool Can(Enum from, Enum to);
}