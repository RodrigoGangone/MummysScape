using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class FallingSand : MonoBehaviour
{
    private Player _player;

    private BoxCollider _viewCollider;
    [SerializeField] private Transform _view;

    [SerializeField] private Transform _invisiblePlatform;

    [Header("SPEED")] [SerializeField] private float speedSand = 3;
    [SerializeField] private float speedInvisiblePlatform = 5;
    [SerializeField] private float stopTime = 3f;

    [Header("WAYPOINTS")] [SerializeField] private Transform[] waypoints;
    private int _currentWaypointIndex;

    private bool _isPaused;
    private bool _upInvisiblePlatform;

    [SerializeField] private ParticleSystem _preUp1,_preUp2;

    private void Start()
    {
        _player = FindObjectOfType<Player>();
        _viewCollider = GetComponent<BoxCollider>();
    }

    private void Update()
    {
        MoveTowardsWaypoint();

        if (_upInvisiblePlatform)
            UpInvisiblePlatform();
    }

    private void MoveTowardsWaypoint()
    {
        if (_isPaused)
        {
            return;
        }

        // Calcula la direcc y mueve la plataform hacia el waypoint actual
        Transform targetWaypoint = waypoints[_currentWaypointIndex];
        float step = speedSand * Time.deltaTime;

        _view.transform.position = Vector3.MoveTowards(_view.transform.position, targetWaypoint.position, step);
        _viewCollider.center = new Vector3(0, _view.transform.position.y - 3.25f, 0);

        // Pausa al llegar a un punto
        if (Vector3.Distance(_view.transform.position, targetWaypoint.position) == 0)
        {
            StartCoroutine(PauseAtWaypoint());
        }
    }

    private IEnumerator PauseAtWaypoint()
    {
        _isPaused = true;

        if (_currentWaypointIndex == 0)
        {
            yield return new WaitForSeconds(stopTime / 2);
            _preUp1.Play();
            _preUp2.Play();
            yield return new WaitForSeconds(stopTime / 2);
        }
        else
        {
            yield return new WaitForSeconds(stopTime);
            _preUp1.Stop();
            _preUp2.Stop();
        }

        _currentWaypointIndex = (_currentWaypointIndex + 1) % waypoints.Length;

        _isPaused = false;
    }

    private void UpInvisiblePlatform()
    {
        float step = speedInvisiblePlatform * Time.deltaTime;

        _invisiblePlatform.position = Vector3.MoveTowards(_invisiblePlatform.position, _view.position, step);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.gameObject.CompareTag("PlayerFather")) return;

        other.transform.SetParent(_invisiblePlatform);
        _upInvisiblePlatform = true;

        if (_player.CurrentPlayerSize != PlayerSize.Head)
            _player._modelPlayer.CountBandage(-_player.CurrentBandageStock);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.gameObject.CompareTag("PlayerFather")) return;

        other.transform.SetParent(null);
        _upInvisiblePlatform = false;

        _invisiblePlatform.position = waypoints[0].position;
    }
}