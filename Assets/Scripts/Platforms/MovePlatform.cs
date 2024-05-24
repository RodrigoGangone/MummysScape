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

    // Update is called once per frame
    void Update()
    {
        _time += Time.deltaTime * speed;

        float x = Mathf.Sin(_time) * moveX;
        float y = Mathf.Sin(_time) * moveY;
        float z = Mathf.Sin(_time) * moveZ;

        transform.position = _startPosition + new Vector3(x, y, z);
    }
}
