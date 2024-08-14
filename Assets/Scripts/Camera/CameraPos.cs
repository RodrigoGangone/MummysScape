using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraPos : MonoBehaviour
{
    [SerializeField] private Transform _player;
    [SerializeField] List<Transform> positions;
    public int pos;
    float minDistance = 0.1f;
    [SerializeField] float speed;

    void FixedUpdate()
    {
        if (Vector3.Distance(transform.position, positions[pos].position) > minDistance)
        {
            transform.position = Vector3.Lerp(transform.position, positions[pos].position, speed);
        }

        transform.LookAt(_player);
    }

    public void SetCam(int pos)
    {
        this.pos = pos;
    }
}