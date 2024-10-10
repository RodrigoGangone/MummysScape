using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.UI;
using UnityEngine.UIElements;

public class FallingSand : MonoBehaviour
{
    private Player _player;

    [SerializeField] private Transform _view;

    [SerializeField] private Transform _invisiblePlatform;
    [SerializeField] private Transform _base;

    private bool _isPaused;

    [Header("SPEED")] public float speed = 1;
    public float stopTime = 0.5f;

    [Header("WAYPOINTS")] [SerializeField] private Transform[] waypoints; // Lista puntos a los que se mueve la platform
    private int _currentWaypointIndex = 0;

    private bool _upInvisiblePlatform;

    private void Start()
    {
        _player = FindObjectOfType<Player>();

        // _invisiblePlatform = transform.GetChild(0);
    }

    //TODO: CUANDO GOLPEA AL PLAYER, TIENE QUE SACARLO DEL ESTADO DE HOOK
    //TODO: HAY QUE HACER POR UPDATE //O CORRUTINA// UN SINE PARA QUE LA VARIABLE VAYA DE 0 A 1.
    //TODO: SI TRIGGEREA AL PLAYER CUANDO ESTA EN 1, TIENE QUE TIRARLO DEL HOOK [CHANGE STATE A FALL]


    private void Update()
    {
        MoveTowardsWaypoint();

        if (_upInvisiblePlatform)
            UpInvisiblePlatform();
    }

    private void MoveTowardsWaypoint()
    {
        if (_isPaused) return;

        // Calcula la direcc y mueve la plataform hacia el waypoint actual
        Transform targetWaypoint = waypoints[_currentWaypointIndex];
        float step = speed * Time.deltaTime;

        _view.transform.position = Vector3.MoveTowards(_view.transform.position, targetWaypoint.position, step);

        // Pausa al llegar a un punto
        if (Vector3.Distance(_view.transform.position, targetWaypoint.position) == 0)
        {
            StartCoroutine(PauseAtWaypoint());
        }
    }

    private IEnumerator PauseAtWaypoint()
    {
        _isPaused = true;

        yield return new WaitForSeconds(stopTime);

        _currentWaypointIndex = (_currentWaypointIndex + 1) % waypoints.Length;

        _isPaused = false;
    }

    private void UpInvisiblePlatform()
    {
        float step = speed * Time.deltaTime;

        _invisiblePlatform.position = Vector3.MoveTowards(_invisiblePlatform.position, _base.position, step);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.gameObject.CompareTag("PlayerFather")) return;

        other.transform.SetParent(transform);

        _upInvisiblePlatform = true;

        if (_player.CurrentPlayerSize != PlayerSize.Head)
            _player._modelPlayer.CountBandage(-_player.CurrentBandageStock);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.gameObject.CompareTag("PlayerFather")) return;

        _upInvisiblePlatform = false;

        _invisiblePlatform.position = waypoints[0].position;

        other.transform.SetParent(null);
    }
}