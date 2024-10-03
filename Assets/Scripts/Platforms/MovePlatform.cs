using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UIElements.Experimental;
using static Utils;

public class MovePlatform : MonoBehaviour
{
    [Header("PLAY ON AWAKE")]
    [SerializeField] private bool isMoving;
    
    [Header("SPEED")]
    public float speed = 1;
    public float stopTime = 0.5f;

    [Header("WAYPOINTS")] 
    [SerializeField] private Transform[] waypoints; // Lista puntos a los que se mueve la platform

    [FormerlySerializedAs("sandMound")]
    [Header("EFFECTS")] 
    [SerializeField] private Transform sandMoundForward;
    [SerializeField] private Transform sandMoundBackward;
    [SerializeField] private Transform[] sandMoundsWaypoints;
    [SerializeField] private Transform[] sandMoundBackwardWaypoints;

    private bool isPaused;
    private bool isMovingToFirstWaypoint;
    
    private int currentWaypointIndex = 0;
    private int currentSandMoundWaypointIndex = 0;

    private void Start()
    {
        if (waypoints.Length > 0 && isMoving)
        {
            // Si la plataforma no está en la posicion del primer waypoint
            if (Vector3.Distance(transform.position, waypoints[0].position) == 0)
                isMovingToFirstWaypoint = true;
            else
                transform.position = waypoints[0].position;
        }

        if (sandMoundsWaypoints.Length <= 0) return;
        sandMoundForward.position = sandMoundsWaypoints[0].position;
        sandMoundBackward.position = sandMoundsWaypoints[sandMoundsWaypoints.Length - 1].position;
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
        if (Vector3.Distance(transform.position, firstWaypoint.position) == 0)
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
        if (Vector3.Distance(transform.position, targetWaypoint.position) == 0)
        { 
            StartCoroutine(PauseAtWaypoint());
        }
        
        MoveSandMound();
    }

    private IEnumerator PauseAtWaypoint()
    {
        isPaused = true;

        yield return new WaitForSeconds(stopTime);

        // Avanza al siguiente waypoint
        currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
        isPaused = false;
    }

    public void StartAction()
    {
        isMoving = !isMoving;
    }

    public void ReturnToPrevious()
    {
        if (currentWaypointIndex > 0)
            currentWaypointIndex--;
        else
            currentWaypointIndex++;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag(PLAYER_TAG))
        {
            other.transform.SetParent(transform);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.CompareTag(PLAYER_TAG))
        {
            other.transform.SetParent(null);
        }
    }

    private void MoveSandMound()
    {
        if (sandMoundsWaypoints.Length <= 0) return;
        Transform targetWaypoint = sandMoundsWaypoints[currentWaypointIndex];
        sandMoundForward.position = Vector3.MoveTowards(sandMoundForward.position, targetWaypoint.position, speed * Time.deltaTime);
        
        if (Vector3.Distance(sandMoundForward.position, targetWaypoint.position) == 0)
        {
            currentSandMoundWaypointIndex = (currentSandMoundWaypointIndex + 1) % sandMoundsWaypoints.Length;
        }
    }
}