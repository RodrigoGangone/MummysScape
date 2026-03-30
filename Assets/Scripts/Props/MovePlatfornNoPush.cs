using UnityEngine;
using static Tags;

/// <summary> 
/// Sensor de Obstrucción: Detecta si el jugador bloquea el camino de una plataforma móvil, 
/// forzando el retorno de la misma a su posición anterior para evitar el "aplastamiento". 
/// </summary>

public class MovePlatfornNoPush : MonoBehaviour
{
    private MoveHorizontalPlatform _moveHorizontalPlatform;

    private void Start()
    {
        _moveHorizontalPlatform = GetComponentInParent<MoveHorizontalPlatform>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(PLAYER_TAG))
        {
            Debug.Log("COLISIONO CON PLAYER");
            _moveHorizontalPlatform.ReturnToPrevious();
        }
    }
}