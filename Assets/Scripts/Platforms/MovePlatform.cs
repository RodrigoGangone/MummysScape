using System.Collections;
using UnityEngine;

public class MovePlatform : MonoBehaviour
{
    [Header("TYPE OF PLATFORM")] 
    [SerializeField] private TypeOfPlatform _type;

    [Header("SPEED")] public float speed;

    [Header("WAYPOINTS")] 
    [SerializeField] private Transform[] waypoints; // Lista de puntos a los que la plataforma se moverá

    private int currentWaypointIndex = 0;
    private bool isPaused;
    private bool isMoving = true;

    private void Start()
    {
        // Mueve la plataforma al primer waypoint al iniciar
        if (waypoints.Length > 0)
        {
            transform.position = waypoints[0].position;
        }
    }

    private void Update()
    {
        if (isMoving && waypoints.Length > 0)
        {
            MoveTowardsWaypoint();
        }
    }

    private void MoveTowardsWaypoint()
    {
        if (isPaused) return;

        // Calcula la dirección y mueve la plataforma hacia el waypoint actual
        Transform targetWaypoint = waypoints[currentWaypointIndex];
        float step = speed * Time.deltaTime;

        transform.position = Vector3.MoveTowards(transform.position, targetWaypoint.position, step);

        // Si la plataforma ha alcanzado el waypoint, inicia la pausa
        if (Vector3.Distance(transform.position, targetWaypoint.position) < 0.1f)
        {
            StartCoroutine(PauseAtWaypoint());
        }
    }

    private IEnumerator PauseAtWaypoint()
    {
        isPaused = true;

        // Espera 0.25 segundos antes de continuar
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