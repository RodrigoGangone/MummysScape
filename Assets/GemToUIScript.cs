using UnityEngine;

public class GemToUIScript : MonoBehaviour
{
    [Header("Configuración de UI")]
    public RectTransform targetUIElement; // Arrastra aquí el icono de la gema de tu Canvas
    public Camera uiCamera;               // Si tu Canvas es 'Screen Space - Camera', arrastra esa cámara aquí. Si es 'Overlay', déjala vacía.

    [Header("Movimiento")]
    public float speed = 10f;
    public float arrivalDistance = 0.5f;

    private bool isMoving = false;

    public void StartFlight()
    {
        isMoving = true;
    }

    void Update()
    {
        if (!isMoving || targetUIElement == null) return;

        // 1. Obtener la posición del icono en la pantalla (píxeles)
        Vector3 screenPos = targetUIElement.position;

        // 2. IMPORTANTE: Definir a qué distancia de la cámara queremos que vuele la gema
        // Usamos una distancia fija (ej: 10 unidades frente a la cámara)
        screenPos.z = 10f; 

        // 3. Convertir de Pantalla a posición en el Mundo 3D
        Vector3 worldTarget = Camera.main.ScreenToWorldPoint(screenPos);

        // 4. Mover la gema hacia ese punto
        transform.position = Vector3.MoveTowards(transform.position, worldTarget, speed * Time.deltaTime);

        // 5. Rotar un poco la gema para que se vea dinámica mientras vuela
        transform.Rotate(Vector3.up * 300f * Time.deltaTime);

        // 6. Detectar si llegó a destino
        if (Vector3.Distance(transform.position, worldTarget) < arrivalDistance)
        {
            OnReachedUI();
        }
    }

    void OnReachedUI()
    {
        isMoving = false;
        // Aquí puedes sumar el punto al contador, activar una partícula en la UI o simplemente:
        Destroy(gameObject); 
    }
}