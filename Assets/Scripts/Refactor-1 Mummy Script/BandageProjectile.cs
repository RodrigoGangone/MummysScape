using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BandageProjectile : MonoBehaviour
{
    public void Play(IReadOnlyList<Vector3> path, float speed) => StartCoroutine(Run(path, speed));
    
    private IEnumerator Run(IReadOnlyList<Vector3> path, float speed)
    {
        foreach (var target in path)
        {
            while (transform.position != target)
            {
                transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);
                transform.LookAt(target);
                yield return null;
            }
        }
    }
}

public static class SimpleShootData
{
    public static List<Vector3> Path;
}