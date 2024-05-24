using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovePlatform : MonoBehaviour
{
    [SerializeField] private float speed = 1.0f;
    [SerializeField] private float moveX = 0f;
    [SerializeField] private float moveY = 0f;
    [SerializeField] private float moveZ = 0f;

    private Vector3 _startPosition;
    private float _time;
    
    void Start()
    {
        _startPosition = transform.position;
    }

    void Update()
    {
        _time += Time.deltaTime * speed;

        float x = moveX != 0 ? Mathf.Sin(_time) * moveX : 0f;
        float y = moveY != 0 ? Mathf.Sin(_time) * moveY : 0f;
        float z = moveZ != 0 ? Mathf.Sin(_time) * moveZ : 0f;

        transform.position = _startPosition + new Vector3(x, y, z);
    }
}
