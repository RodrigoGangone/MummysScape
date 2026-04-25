using UnityEngine;

public class FlyingObject : MonoBehaviour
{
    [Header("Configuración de Vuelo")]
    [SerializeField] private Vector3 direccionVuelo = Vector3.up;
    [SerializeField] private float velocidad = 5f;

    private Vector3 posicionOriginal;
    private bool estaVolando = false;

    private void Awake()
    {
        // Guardamos la posición exacta donde empieza el objeto
        posicionOriginal = transform.position;
    }

    private void Update()
    {
        if (estaVolando) 
        {
            // Mueve el objeto de forma constante en la dirección elegida
            transform.Translate(direccionVuelo.normalized * velocidad * Time.deltaTime, Space.World);
        }
    }

    // Método para conectar al OnActivated del Switch
    public void ActivarVuelo()
    {
        estaVolando = true;
    }

    // Método para conectar al OnDeactivated del Switch
    public void ResetearPosicion()
    {
        estaVolando = false;
        transform.position = posicionOriginal;
    }
}