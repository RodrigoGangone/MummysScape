using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class SeesawController : MonoBehaviour
{
    [Header("Configuración de Rotación (Offsets)")]
    [Tooltip("Velocidad a la que se inclina la tabla")]
    [SerializeField] private float rotationSpeed = 5f;
    [Tooltip("Cuánto se suma/resta al X original cuando la izquierda baja")]
    [SerializeField] private float leftAngle = 15f;   
    [Tooltip("Cuánto se suma/resta al X original cuando la derecha baja")]
    [SerializeField] private float rightAngle = -15f; 
    [Tooltip("Offset inicial respecto al X original")]
    [SerializeField] private float neutralAngle = 0f;
    [Tooltip("Si es true, la tabla vuelve al centro cuando no hay nadie. Si es false, se queda inclinada.")]
    [SerializeField] private bool returnToNeutral = false;

    [Header("Configuración de Detección (Box)")]
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private Vector3 boxExtents = new Vector3(1f, 0.5f, 1f); 
    [SerializeField] private Vector3 leftBoxCenter = new Vector3(-2f, 0.5f, 0f); 
    [SerializeField] private Vector3 rightBoxCenter = new Vector3(2f, 0.5f, 0f); 

    private Rigidbody rb;
    private float currentAngleOffset; 
    private float targetAngleOffset; 
    
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
        
        // Guardamos los ángulos iniciales
        originalX = transform.rotation.eulerAngles.x;
        originalY = transform.rotation.eulerAngles.y;
        originalZ = transform.rotation.eulerAngles.z;

        currentAngleOffset = neutralAngle;
        targetAngleOffset = neutralAngle;
    }

    private void FixedUpdate()
    {
        // Revisamos en tiempo real. Si cambia de Size, esto devuelve false automáticamente.
        bool isLeftOccupied = CheckSide(leftBoxCenter);
        bool isRightOccupied = CheckSide(rightBoxCenter);

        if (isLeftOccupied && !isRightOccupied)
        {
            targetAngleOffset = leftAngle;
        }
        else if (isRightOccupied && !isLeftOccupied)
        {
            targetAngleOffset = rightAngle;
        }
        else 
        {
            // CASO: La momia se bajó de la tabla O cambió a un Size que no activa la tabla.
            if (returnToNeutral) 
            {
                targetAngleOffset = neutralAngle;
            }
            else
            {
                // INTERRUPCIÓN: Igualamos el target a la posición actual. 
                // Esto congela la rotación en el acto, simulando tu antiguo 'break;'.
                targetAngleOffset = currentAngleOffset; 
            }
        }

        currentAngleOffset = Mathf.Lerp(currentAngleOffset, targetAngleOffset, Time.fixedDeltaTime * rotationSpeed);
        
        float finalXAngle = originalX + currentAngleOffset;
        Quaternion targetRotation = Quaternion.Euler(finalXAngle, originalY, originalZ);
        rb.MoveRotation(targetRotation);
    }

    private bool CheckSide(Vector3 localCenter)
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
            
            // Validamos que sea la momia y tenga el tamaño correcto
            if (mummy != null && mummy.Ctx.Model.Size == PlayerEnum.PlayerSize.Normal)
            {
                return true;
            }
        }
        
        return false;
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