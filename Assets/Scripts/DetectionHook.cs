using System.Collections.Generic;
using UnityEngine;

public class DetectionHook : MonoBehaviour
{
    public List<Collider> _hooks;
    public Rigidbody currentHook;

    private bool _isWall = false;
    private Transform _hook;

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Hook"))
        {
            _hooks.Add(other);
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.gameObject.layer != LayerMask.NameToLayer("Hook") || _hooks.Count == 0) return;
        if (!isPosibleHook(other)) return;

        _hook = other.transform.GetChild(0);

        float nearestDistance = float.MaxValue;

        if (_hooks.Count > 1)
        {
            foreach (Collider hook in _hooks)
            {
                float distance = Vector3.Distance(transform.position, hook.transform.position);

                if (distance < nearestDistance)
                {
                    nearestDistance = distance;

                    currentHook = hook.GetComponent<Rigidbody>();

                    _hook.gameObject.SetActive(true);
                }
                else
                {
                    _hook.gameObject.SetActive(false);
                }
            }
        }
        else if (_hooks.Count == 1)
        {
            currentHook = _hooks[0].GetComponent<Rigidbody>();
            _hook.gameObject.SetActive(true);
        }
    }
    
    private bool isPosibleHook(Collider other)
    {
        RaycastHit hit;
        Vector3 direction = other.transform.position - transform.position;

        if (Physics.Raycast(transform.position, direction, out hit, direction.magnitude, LayerMask.GetMask("Wall")))
        {
            Debug.Log("VERDADERO - Wall detected");
            return false;
        }

        return true;
    }

    private void OnTriggerExit(Collider other) // Eliminar el Beetle que agregue
    {
        if (other.gameObject.layer != LayerMask.NameToLayer("Hook")) return;

        _hook = other.transform.GetChild(0);
        _hook.gameObject.SetActive(false);

        var cur = other.GetComponent<Rigidbody>();

        _hooks.Remove(other);

        if (currentHook == cur)
        {
            currentHook = null;
        }
    }
}