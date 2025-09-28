using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Asigná manualmente (en escena) los puntos de géiser, en el orden que quieras.
/// </summary>
public class GeyserPointProvider : MonoBehaviour
{
    [Tooltip("Arrastrá aquí los transforms de los puntos (en escena), en el orden deseado.")]
    public List<Transform> points = new();
}