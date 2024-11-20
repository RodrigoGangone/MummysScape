using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UIElements.Experimental;
using static Utils;

public class MoveHorizontalPlatform : MonoBehaviour
{
    [Header("PLAY ON AWAKE")] [SerializeField]
    private bool isMoving;

    [Header("SPEED")] public float speed = 1;
    public float stopTime = 0.5f;

    [Header("WAYPOINTS")] [SerializeField] private Transform[] waypoints; // Lista puntos a los que se mueve la platform

    [Header("EFFECTS")] [SerializeField] private float moundEmergenceSpeed = 1;

    [SerializeField] private Transform sandMoundForward;
    [SerializeField] private Transform[] sandMoundForwardWaypoints;

    [SerializeField] private Transform sandMoundBackward;
    [SerializeField] private Transform[] sandMoundBackwardWaypoints;

    [SerializeField] private ParticleSystem[] sandMoundForwardParticles;
    [SerializeField] private ParticleSystem[] sandMoundBackwardParticles;

    [SerializeField] private ParticleSystem activationParticles;
    [SerializeField] private float glowDuration = 2f;
    [SerializeField] private float glowIntensity = 0.15f;
    private Material[] platformMaterials;

    private bool isPaused;
    private bool isMovingToFirstWaypoint;

    private int _currentWaypointIndex = 0;

    private void Start()
    {
        platformMaterials = GetMaterialsFromChildren();

        if (waypoints.Length > 0 && isMoving)
        {
            // Si la plataforma no está en la posicion del primer waypoint
            if (Vector3.Distance(transform.position, waypoints[0].position) == 0)
                isMovingToFirstWaypoint = true;
            else
                transform.position = waypoints[0].position;
        }

        if (sandMoundForward != null && sandMoundBackward != null)
        {
            sandMoundForward.position = sandMoundForwardWaypoints[0].position;
            sandMoundBackward.position = sandMoundBackwardWaypoints[0].position;
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
        if (Vector3.Distance(transform.position, firstWaypoint.position) == 0)
        {
            isMovingToFirstWaypoint = false;
            _currentWaypointIndex = 1; // Empieza a moverse hacia el segundo waypoint
        }
    }

    private void MoveTowardsWaypoint()
    {
        if (isPaused)
        {
            if (sandMoundForward != null && sandMoundBackward != null)
            {
                ResetSandMoundPositions();
            }
        }
        else
        {
            // Calcula la direcc y mueve la plataform hacia el waypoint actual
            Transform targetWaypoint = waypoints[_currentWaypointIndex];
            float step = speed * Time.deltaTime;

            transform.position = Vector3.MoveTowards(transform.position, targetWaypoint.position, step);

            // Pausa al llegar a un punto
            if (Vector3.Distance(transform.position, targetWaypoint.position) == 0)
            {
                StartCoroutine(PauseAtWaypoint());
            }


            if (sandMoundForward != null && sandMoundBackward != null)
            {
                MoveSandMound();
            }
        }
    }

    private IEnumerator PauseAtWaypoint()
    {
        // Avanza al siguiente waypoint
        isPaused = true;

        yield return new WaitForSeconds(stopTime);

        _currentWaypointIndex = (_currentWaypointIndex + 1) % waypoints.Length;

        isPaused = false;
    }

    public void StartAction()
    {
        AudioManager.Instance.PlaySFX(NameSounds.MovingPlatform);
        
        isMoving = !isMoving;
        activationParticles.Play();
        StartCoroutine((GlowEffect(glowDuration)));
    }

    public void ReturnToPrevious()
    {
        if (_currentWaypointIndex > 0)
            _currentWaypointIndex--;
        else
            _currentWaypointIndex++;
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

    private void ResetSandMoundPositions()
    {
        sandMoundForward.position = Vector3.MoveTowards(sandMoundForward.position,
            sandMoundForwardWaypoints[0].position, moundEmergenceSpeed * Time.deltaTime);
        sandMoundBackward.position = Vector3.MoveTowards(sandMoundBackward.position,
            sandMoundBackwardWaypoints[0].position, moundEmergenceSpeed * Time.deltaTime);
    }

    private void MoveSandMound()
    {
        Transform targetWaypoint = sandMoundForwardWaypoints[_currentWaypointIndex];
        int inverseWaypointIndex = (sandMoundForwardWaypoints.Length - 1) - _currentWaypointIndex;
        Transform targetWaypointBackward = sandMoundBackwardWaypoints[inverseWaypointIndex];

        //Cada montículo se mueve en relación al currentWaypointIndex que se fija en los "Target Waypoint"
        sandMoundForward.position = Vector3.MoveTowards(sandMoundForward.position, targetWaypoint.position,
            moundEmergenceSpeed * Time.deltaTime);
        sandMoundBackward.position = Vector3.MoveTowards(sandMoundBackward.position, targetWaypointBackward.position,
            moundEmergenceSpeed * Time.deltaTime);

        //Cuando avanza, se activan las partículas de adelante.
        if (_currentWaypointIndex == 1)
        {
            if (!sandMoundForwardParticles[0].isPlaying || !sandMoundForwardParticles[1].isPlaying)
            {
                sandMoundForwardParticles[1].Play();
                sandMoundForwardParticles[0].Play();
            }

            sandMoundBackwardParticles[1].Stop();
            sandMoundBackwardParticles[0].Stop();
        }
        //Cuando retrocede, activa las de atras
        else if (_currentWaypointIndex == 0)
        {
            if (!sandMoundBackwardParticles[0].isPlaying || !sandMoundBackwardParticles[1].isPlaying)
            {
                sandMoundBackwardParticles[1].Play();
                sandMoundBackwardParticles[0].Play();
            }

            sandMoundForwardParticles[1].Stop();
            sandMoundForwardParticles[0].Stop();
        }
    }

    private Material[] GetMaterialsFromChildren()
    {
        return GetComponentsInChildren<Renderer>()
            .SelectMany(renderer => renderer.materials)
            .ToArray();
    }

    private IEnumerator GlowEffect(float duration)
    {
        StartCoroutine(IncreaseIntensity(duration / 2));
        yield return new WaitForSeconds(duration / 2);
        StartCoroutine(DecreaseIntensity(duration / 2));
    }

    private IEnumerator IncreaseIntensity(float duration)
    {
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float currentIntensity = Mathf.Lerp(0f, glowIntensity, elapsedTime / duration);

            foreach (Material mat in platformMaterials)
            {
                if (mat.HasProperty("_GlowIntensity"))
                {
                    mat.SetFloat("_GlowIntensity", currentIntensity);
                }
            }

            yield return null;
        }
    }

    private IEnumerator DecreaseIntensity(float duration)
    {
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float currentIntensity = Mathf.Lerp(glowIntensity, 0f, elapsedTime / duration);

            foreach (Material mat in platformMaterials)
            {
                if (mat.HasProperty("_GlowIntensity"))
                {
                    mat.SetFloat("_GlowIntensity", currentIntensity);
                }
            }

            yield return null;
        }
    }
}