using System.Collections.Generic;
using UnityEngine;

public class DetectionHook : MonoBehaviour
{
    public List<Collider> _hooks;
    public Rigidbody currentHook;

    private bool _isWall = false;
    private ParticleSystem _hookParticleSystem;
    public Player _player;

    private void Start()
    {
        _player = FindObjectOfType<Player>();
    }

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

        var hookParticleSystem = other.transform.GetChild(0).GetComponent<ParticleSystem>();

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

                    if (!hookParticleSystem.isPlaying) hookParticleSystem.Play();
                }
                else
                {
                    if (hookParticleSystem.isPlaying) hookParticleSystem.Stop();
                }
            }
        }
        else if (_hooks.Count == 1)
        {
            currentHook = _hooks[0].GetComponent<Rigidbody>();
            if (!hookParticleSystem.isPlaying) hookParticleSystem.Play();
        }

        _player.rightHand.data.target = currentHook.transform;
    }

    private bool isPosibleHook(Collider other)
    {
        Vector3 direction = other.transform.position - transform.position;

        if (Physics.Raycast(transform.position, direction, out RaycastHit hit, direction.magnitude, LayerMask.GetMask("Wall")))
        {
            return false;
        }

        return true;
    }

    private void OnTriggerExit(Collider other) // Eliminar el Beetle que agregue
    {
        if (other.gameObject.layer != LayerMask.NameToLayer("Hook")) return;
        
        //Apagar particula del beetle que ya no veria
        var hookParticleSystem = other.transform.GetChild(0).GetComponent<ParticleSystem>();
        if (hookParticleSystem.isPlaying) hookParticleSystem.Stop();

        var cur = other.GetComponent<Rigidbody>();

        _hooks.Remove(other);

        if (currentHook == cur)
        {
            currentHook = null;
        }
    }
}