using UnityEngine;

public class FlameHead : MonoBehaviour
{
    [Header("Bones (Mid, Upper, Tip)")]
    public Transform[] bones; 
    
    [Header("Wobble Settings")]
    public float lagSpeed = 10f;       // Velocidad a la que vuelve a la posición original
    public float maxDragAngle = 45f;   // Ángulo máximo de inclinación
    public float effectMultiplier = 2f; // Fuerza del efecto según la velocidad

    private Vector3 lastPosition;
    private Quaternion[] initialLocalRotations;
 
    void Start()
    {
        lastPosition = transform.position;
        initialLocalRotations = new Quaternion[bones.Length];
        
        // Guardamos la rotación inicial (la posición de reposo)
        for (int i = 0; i < bones.Length; i++)
        {
            initialLocalRotations[i] = bones[i].localRotation;
        }
    }

    void LateUpdate()
    {
        // 1. Calculamos la velocidad actual del objeto
        Vector3 velocity = (transform.position - lastPosition) / Time.deltaTime;
        lastPosition = transform.position;

        // 2. Convertimos la velocidad al espacio local
        Vector3 localVelocity = transform.InverseTransformDirection(velocity);

        // 3. Aplicamos el efecto a cada hueso
        for (int i = 0; i < bones.Length; i++)
        {
            Quaternion targetRot = initialLocalRotations[i];

            // Si hay movimiento, calculamos la rotación de "arrastre"
            if (velocity.magnitude > 0.1f)
            {
                // El eje de rotación es perpendicular a la dirección del movimiento
                Vector3 tiltAxis = Vector3.Cross(Vector3.up, localVelocity.normalized);
                
                // Mientras más alto sea el hueso en el array, más se dobla (efecto soga)
                float hierarchyFactor = (i + 1) * 0.5f; 
                
                float tiltAngle = Mathf.Clamp(velocity.magnitude * effectMultiplier * hierarchyFactor, 0, maxDragAngle);
                
                // Calculamos la rotación extra y se la sumamos a la inicial
                Quaternion dragRot = Quaternion.AngleAxis(tiltAngle, tiltAxis);
                targetRot = initialLocalRotations[i] * dragRot;
            }

            // Interpolamos suavemente hacia la rotación objetivo (ya sea arrastre o reposo)
            bones[i].localRotation = Quaternion.Lerp(bones[i].localRotation, targetRot, Time.deltaTime * lagSpeed);
        }
    }
}