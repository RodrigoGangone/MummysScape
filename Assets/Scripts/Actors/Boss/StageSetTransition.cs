using UnityEngine;

/// <summary>
/// Gestor de Entorno: Simplificado.
/// Actualmente solo gestiona la activación del objeto especial (venda) al iniciar el evento correspondiente.
/// </summary>
[DisallowMultipleComponent]
public class StageSetTransition : MonoBehaviour
{
    [Header("Objetos Especiales")]
    [SerializeField] private GameObject specialObjectToActivate;

    private void OnEnable()
    {
        // Solo nos interesa escuchar cuando el jugador activa el estado para encender el objeto
        GameEventManager.Instance.playerEvents.OnEmpoweredBegin.Register(ActivateSpecialObject);
    }

    private void OnDisable()
    {
        GameEventManager.Instance.playerEvents.OnEmpoweredBegin.Unregister(ActivateSpecialObject);
    }

    private void ActivateSpecialObject()
    {
        if (specialObjectToActivate != null)
        {
            specialObjectToActivate.SetActive(true);
        }
    }
}