/// <summary>
/// Define los estados efectivos posibles del botón de presión según el peso acumulado.
/// </summary>
public enum PressureButtonState
{
    Released,
    HalfPressed,
    FullyPressed
}

/// <summary>
/// Define las posiciones estables que puede solicitarse a una trampa de lanzas.
/// </summary>
public enum SpikeTrapState
{
    Raised,
    HalfRaised,
    Lowered
}
