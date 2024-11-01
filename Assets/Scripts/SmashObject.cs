using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class SmashObject : MonoBehaviour
{
    [SerializeField] private Transform _wayPoint;

    [SerializeField] private List<GameObject> _tables;

    private const float _velocity = 3;

    [SerializeField] private MeshRenderer _fatherView;
    [SerializeField] private Collider _trigegerCollider;
    [SerializeField] private Collider _fatherCollider;

    [SerializeField] private bool _issueWood;

    [SerializeField] private ParticleSystem _puffFx;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Smash")) return;

        _puffFx.Play();

        _fatherView.enabled = false;
        _fatherCollider.enabled = false;
        _trigegerCollider.enabled = false;

        if (_issueWood)
        {
            StartCoroutine(MoveTablesWithDelay());
        }
    }

    private IEnumerator MoveTablesWithDelay()
    {
        foreach (var table in _tables)
        {
            table.SetActive(true);
        }

        for (int i = 0; i < _tables.Count; i++)
        {
            StartCoroutine(MoveToWaypoint(_tables[i], _wayPoint.position));
            yield return new WaitForSeconds(0.5f);
        }

        foreach (var table in _tables)
        {
            table.GetComponent<MeshCollider>().enabled = true;
        }
    }

    private IEnumerator MoveToWaypoint(GameObject table, Vector3 targetPosition)
    {
        while (Vector3.Distance(table.transform.position, targetPosition) > 0.01f)
        {
            table.transform.position =
                Vector3.MoveTowards(table.transform.position, targetPosition, _velocity * Time.deltaTime);
            yield return null;
        }
    }
}