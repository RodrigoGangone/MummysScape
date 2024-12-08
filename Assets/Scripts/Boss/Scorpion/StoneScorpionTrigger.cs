using System;
using Unity.Mathematics;
using UnityEngine;

public class StoneScorpionTrigger : MonoBehaviour
{
    private Scorpion _scorpion;
    private CapsuleCollider _capsuleCollider;
    private int _layerMask;
    [SerializeField] private ParticleSystem _hitFx;

    private void Start()
    {
        _scorpion = FindObjectOfType<Scorpion>();
        _capsuleCollider = GetComponent<CapsuleCollider>();
        _layerMask = LayerMask.GetMask("Wall", "Box", "Floor");
    }

    private void Update()
    {
        if (Physics.Raycast(transform.position, Vector3.forward, 0.3f, _layerMask))
            gameObject.layer = LayerMask.NameToLayer("NoInteractable");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Wall") ||
            other.gameObject.layer == LayerMask.NameToLayer("Box") ||
            other.gameObject.layer == LayerMask.NameToLayer("Floor") ||
            other.gameObject.layer == LayerMask.NameToLayer("Player"))
            Instantiate(_hitFx, other.transform.position, quaternion.identity);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;

        Vector3 rayOrigin = transform.position;
        Vector3 rayDirection = Vector3.forward;
        float rayLength = 0.3f;

        Gizmos.DrawLine(rayOrigin, rayOrigin + rayDirection * rayLength);

        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(rayOrigin + rayDirection * rayLength, 0.05f);
    }
}