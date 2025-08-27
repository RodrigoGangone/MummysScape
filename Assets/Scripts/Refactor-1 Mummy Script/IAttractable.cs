/// <summary>
/// IAttractable
/// Contrato para objetos que pueden ser atraidos hacia un punto objetivo.
/// </summary>
public interface IAttractable
{
    /// <summary>
    /// Aplica una atracción hacia targetPosition. La implementación decide si usa fuerzas o MovePosition.
    /// Debe respetar límites de velocidad y ejes si corresponde. Devuelve true si se aplicó atracción este frame.
    /// </summary>
    bool PullTowards(UnityEngine.Vector3 targetPosition, float strength, float maxSpeed);
}