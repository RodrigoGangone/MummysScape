using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlatformParent : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        // Verificamos si es el player
        if (other.CompareTag("PlayerFather"))
        {
            // Lo hacemos hijo de la plataforma manteniendo su posición global
            other.transform.SetParent(transform, true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // Verificamos si es el player para no desparentar otra cosa
        if (other.CompareTag("PlayerFather"))
        {
            // Lo devolvemos a la raíz de la escena (sin padre)
            other.transform.SetParent(null, true);
        }
    }
}
