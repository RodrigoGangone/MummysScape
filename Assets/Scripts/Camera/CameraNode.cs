using System.Collections.Generic;
using UnityEngine;

public class CameraNode : MonoBehaviour
{
    [Tooltip("Nodos a los que este nodo puede conectarse.")]
    public List<CameraNode> connectedNodes;  // Lista de nodos con los que puede conectar

    public Vector3 Position => transform.position;  // La posición del nodo en la escena
}