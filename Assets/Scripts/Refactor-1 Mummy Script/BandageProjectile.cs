using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class BandageProjectile : Pausable
{
    private Rigidbody _rb;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.useGravity = false;
    }

    public void Play(IReadOnlyList<Vector3> path, float speed) => StartCoroutine(RunPhysics(path, speed));
    private IEnumerator RunPhysics(IReadOnlyList<Vector3> path, float speed)
    {
        foreach (var target in path)
        {
            while (Vector3.Distance(_rb.position, target) > 0.1f)
            {
                if (Paused) { yield return WaitWhilePaused(); continue; }

                var newPosition = Vector3.MoveTowards(_rb.position, target, speed * Time.fixedDeltaTime);
                _rb.MovePosition(newPosition);
                
                var dir = target - _rb.position;
                if (dir.sqrMagnitude > 0.001f)
                {
                    var targetRot = Quaternion.LookRotation(dir.normalized);
                    var newRot = Quaternion.Slerp(_rb.rotation, targetRot, Time.fixedDeltaTime);
                    _rb.MoveRotation(newRot);
                }
                yield return new WaitForFixedUpdate();
            }
        }
        
        _rb.useGravity = true;
    }

    public override void OnPauseChanged(bool paused) { }
}

public static class SimpleShootData
{
    public static List<Vector3> Path;
}