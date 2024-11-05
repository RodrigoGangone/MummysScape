using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StandingTable : MonoBehaviour
{
    private Player _player;
    private List<GameObject> _tables = new();

    public List<GameObject> Tables
    {
        set => _tables = value;
    }

    private Coroutine _gravityCoroutine;
    public Coroutine replacementTables;

    private const float TIME_TO_FALL = 1f;
    private const float TIME_TO_REPLACEMENT = 2f;

    public Action ArrangeTables;

    private void Start()
    {
        ArrangeTables = () =>
        {
            foreach (var table in _tables)
            {
                var tableRb = table.GetComponent<Rigidbody>();
                tableRb.isKinematic = true;
            }
        };
    }

    private void OnTriggerStay(Collider other)
    {
        if (!other.CompareTag("PlayerFather")) return;

        _player = other.GetComponent<Player>();

        if (_player.CurrentPlayerSize == PlayerSize.Normal)
        {
            if (_gravityCoroutine == null)
                _gravityCoroutine = StartCoroutine(CountToActivateGravity());

            if (replacementTables != null)
            {
                StopCoroutine(replacementTables);
                replacementTables = null;
            }
        }
    }


    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("PlayerFather")) return;

        if (_gravityCoroutine != null)
            StopCoroutine(_gravityCoroutine);

        if (replacementTables == null)
            replacementTables = StartCoroutine(ReplacementTables());
    }


    private IEnumerator CountToActivateGravity()
    {
        yield return new WaitForSeconds(TIME_TO_FALL);

        foreach (var table in _tables)
        {
            if (table != null)
            {
                var tableRb = table.GetComponent<Rigidbody>();
                tableRb.isKinematic = false;

                yield return new WaitForSeconds(0.2f);
            }
        }

        replacementTables = StartCoroutine(ReplacementTables());
    }

    private IEnumerator ReplacementTables()
    {
        yield return new WaitForSeconds(TIME_TO_REPLACEMENT);

        ArrangeTables.Invoke();
    }
}