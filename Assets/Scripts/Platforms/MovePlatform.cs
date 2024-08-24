using System.Collections;
using UnityEngine;

public class MovePlatform : MonoBehaviour
{
    [Header("TYPE OF PLATFORM")] 
    [SerializeField] private TypeOfPlatform _type;

    [Header("SPEED")] public float speed;

    [Header("WAYPOINTS")] 
    [SerializeField] private Transform[] waypoints; // Lista puntos a los que se mueve la platform

    private int currentWaypointIndex = 0;
    private bool isPaused;
    private bool isMoving;
    private bool isMovingToFirstWaypoint;

    private void Start()
    {
        if (waypoints.Length > 0 && isMoving)
        {
            // Si la plataforma no está en la posicion del primer waypoint
            if (Vector3.Distance(transform.position, waypoints[0].position) > 0.1f)
            {
                isMovingToFirstWaypoint = true;
            }
            else
            {
                transform.position = waypoints[0].position;
            }
        }
    }

    private void Update()
    {
        if (isMovingToFirstWaypoint)
        {
            MoveToFirstWaypoint();
        }
        else if (isMoving && waypoints.Length > 0)
        {
            MoveTowardsWaypoint();
        }
    }
    
    private void MoveToFirstWaypoint()
    {
        // Mueve la plataforma hacia el primer waypoint
        Transform firstWaypoint = waypoints[0];
        float step = speed * Time.deltaTime;

        transform.position = Vector3.MoveTowards(transform.position, firstWaypoint.position, step);

        // Si la plataforma ha alcanzado el primer waypoint
        if (Vector3.Distance(transform.position, firstWaypoint.position) < 0.1f)
        {
            isMovingToFirstWaypoint = false;
            currentWaypointIndex = 1; // Empieza a moverse hacia el segundo waypoint
        }
    }

    private void MoveTowardsWaypoint()
    {
        if (isPaused) return;

        // Calcula la direcc y mueve la plataform hacia el waypoint actual
        Transform targetWaypoint = waypoints[currentWaypointIndex];
        float step = speed * Time.deltaTime;

        transform.position = Vector3.MoveTowards(transform.position, targetWaypoint.position, step);

        // Pausa al llegar a un punto
        if (Vector3.Distance(transform.position, targetWaypoint.position) < 0.1f)
        {
            StartCoroutine(PauseAtWaypoint());
        }
    }

    private IEnumerator PauseAtWaypoint()
    {
        isPaused = true;

        yield return new WaitForSeconds(0.25f);

        // Avanza al siguiente waypoint
        currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
        isPaused = false;
    }

    public void StartAction()
    {
        isMoving = !isMoving;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("PlayerFather"))
        {
            other.transform.SetParent(transform);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.CompareTag("PlayerFather"))
        {
            other.transform.SetParent(null);
        }
    }
}

public enum TypeOfPlatform
{
    MoveAxis,
    RotateConstant,
    Rotate90,
    MoveWithoutAct
}