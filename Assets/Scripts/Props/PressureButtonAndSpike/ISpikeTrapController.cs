/// <summary>
/// Define el contrato mínimo que utiliza un botón para solicitar un estado de trampa
/// sin conocer cómo se resuelven su movimiento, vibración o físicas.
/// </summary>
public interface ISpikeTrapController
{
    void SetState(SpikeTrapState targetState);
}
