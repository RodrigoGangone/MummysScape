using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;

public class SmashObject : MonoBehaviour
{
    [SerializeField] private GameObject _bubbles;
    [SerializeField] private bool _inWater;
    [SerializeField] private StandingTable _standingTable;

    [SerializeField] private Transform _wayPoint;

    [SerializeField] private List<GameObject> _tables;

    private const float _velocity = 3;

    [SerializeField] private MeshRenderer _fatherView;
    [SerializeField] private Collider _triggerCollider;
    [SerializeField] private Collider _fatherCollider;

    [SerializeField] private ParticleSystem _destroyFx;

    private void Start()
    {
        _bubbles.SetActive(_inWater);

        _standingTable.Tables = _tables;

        _standingTable.ArrangeTables += () =>
        {
            _standingTable.replacementTables = StartCoroutine(MoveTablesWithDelay());
        };
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Smash")) return;

        _destroyFx.Play();

        _fatherView.enabled = false;
        _fatherCollider.enabled = false;
        _triggerCollider.enabled = false;
        
        _bubbles.GetComponent<ParticleSystem>().Stop();

        if (_inWater)
        {
            ActivateTables();

            StartCoroutine(MoveTablesWithDelay());
        }
    }

    private IEnumerator MoveTablesWithDelay()
    {
        for (int i = 0; i < _tables.Count; i++)
        {
            StartCoroutine(MoveToWaypoint(_tables[i], _wayPoint));
            yield return new WaitForSeconds(0.5f);
        }

        foreach (var table in _tables)
        {
            table.GetComponent<MeshCollider>().enabled = true;
        }
    }

    private void ActivateTables()
    {
        foreach (var table in _tables)
        {
            table.SetActive(true);
        }
    }

    private IEnumerator MoveToWaypoint(GameObject table, Transform targetPosition)
    {
        while (Vector3.Distance(table.transform.position, targetPosition.position) > 0.01f ||
               table.transform.rotation != targetPosition.rotation)
        {
            table.transform.position =
                Vector3.MoveTowards(table.transform.position, targetPosition.position, _velocity * Time.deltaTime);

            table.transform.rotation = Quaternion.Slerp(
                table.transform.rotation, targetPosition.rotation, _velocity * Time.deltaTime);
            yield return null;
        }

        table.transform.rotation = targetPosition.rotation;
    }
}