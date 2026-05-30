using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class SeesawController : MonoBehaviour
{
    [Header("Configuración de Rotación (Offsets)")]
    [SerializeField] private float rotationSpeed = 2f;
    [Tooltip("Curva que dicta la aceleración/desaceleración del movimiento")]
    [SerializeField] private AnimationCurve rotationCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [Tooltip("Cuánto se suma/resta al X original cuando la izquierda baja")]
    [SerializeField] private float leftAngle = 15f;   
    [Tooltip("Cuánto se suma/resta al X original cuando la derecha baja")]
    [SerializeField] private float rightAngle = -15f; 
    [Tooltip("Offset inicial respecto al X original (usualmente 0)")]
    [SerializeField] private float neutralAngle = 0f; 

    [Header("Configuración de Detección (Box)")]
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private Vector3 boxExtents = new Vector3(1f, 0.5f, 1f); 
    [SerializeField] private Vector3 leftBoxCenter = new Vector3(-2f, 0.5f, 0f); 
    [SerializeField] private Vector3 rightBoxCenter = new Vector3(2f, 0.5f, 0f); 

    private Rigidbody rb;
    private bool isRotating = false;
    private float currentAngleOffset; 
    
    // Variables para guardar la rotación original exacta del objeto en la escena
    private float originalX;
    private float originalY;
    private float originalZ;

    // Buffer para OverlapBoxNonAlloc
    private Collider[] hitColliders = new Collider[1];

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        currentAngleOffset = neutralAngle;
        
        // Guardamos los ángulos iniciales
        originalX = transform.rotation.eulerAngles.x;
        originalY = transform.rotation.eulerAngles.y;
        originalZ = transform.rotation.eulerAngles.z;
    }

    private void FixedUpdate()
    {
        if (isRotating) return;

        CheckSide(leftBoxCenter, leftAngle);
        CheckSide(rightBoxCenter, rightAngle);
    }

    private void CheckSide(Vector3 localCenter, float targetOffset)
    {
        Vector3 worldCenter = transform.TransformPoint(localCenter);
        
        int hits = Physics.OverlapBoxNonAlloc(
            worldCenter, 
            boxExtents, 
            hitColliders, 
            transform.rotation, 
            playerLayer
        );

        if (hits > 0)
        {
            PlayerController mummy = hitColliders[0].GetComponentInParent<PlayerController>();
            
            // NOTA: Sigo usando PlayerSize.Normal como estaba en tu código. 
            // Cámbialo al tamaño "Grande" si esa es la regla de diseño final.
            if (mummy != null && mummy.Ctx.Model.Size == PlayerEnum.PlayerSize.Normal)
            {
                if (!Mathf.Approximately(currentAngleOffset, targetOffset))
                {
                    // Pasamos la referencia de la momia a la corrutina
                    StartCoroutine(RotateToAngle(targetOffset, mummy));
                }
            }
        }
    }

    private IEnumerator RotateToAngle(float targetOffset, PlayerController mummy)
    {
        isRotating = true;
        currentAngleOffset = targetOffset;

        Quaternion startRotation = rb.rotation;
        
        float finalXAngle = originalX + targetOffset;
        Quaternion targetRotation = Quaternion.Euler(finalXAngle, originalY, originalZ);
        
        float t = 0;
        bool wasInterrupted = false;

        while (t < 1f)
        {
            // Validamos si la momia se destruyó o si cambió de tamaño a uno que no activa la tabla
            if (mummy == null || mummy.Ctx.Model.Size != PlayerEnum.PlayerSize.Normal)
            {
                wasInterrupted = true;
                break; // Rompemos el ciclo while para detener el movimiento instantáneamente
            }

            t += Time.fixedDeltaTime * rotationSpeed;
            
            float curveValue = rotationCurve.Evaluate(t);
            rb.MoveRotation(Quaternion.Lerp(startRotation, targetRotation, curveValue));
            
            yield return new WaitForFixedUpdate(); 
        }

        if (!wasInterrupted)
        {
            // Si terminó naturalmente, aseguramos la rotación final exacta
            rb.MoveRotation(targetRotation);
        }
        else
        {
            // Si fue interrumpido, liberamos el target actual. 
            // Esto permite que el CheckSide vuelva a disparar la corrutina hacia el mismo lado
            // una vez que la momia recupere el tamaño adecuado.
            currentAngleOffset = -999f; 
        }

        isRotating = false;
    }

    private void OnDrawGizmos()
    {
        Matrix4x4 oldMatrix = Gizmos.matrix;
        Gizmos.matrix = transform.localToWorldMatrix;

        Gizmos.color = new Color(0, 1, 0, 0.4f);
        Gizmos.DrawCube(leftBoxCenter, boxExtents * 2);
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(leftBoxCenter, boxExtents * 2);

        Gizmos.color = new Color(1, 0, 0, 0.4f);
        Gizmos.DrawCube(rightBoxCenter, boxExtents * 2);
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(rightBoxCenter, boxExtents * 2);

        Gizmos.matrix = oldMatrix;
    }
}