using System;
using UnityEngine;
using UnityEngine.Serialization;

public class MovePlatform : MonoBehaviour
{
    [Header("SPEED")] [SerializeField] private float speed = 1.0f;

    [Header("MOVE")] [SerializeField] private float moveX;
    [SerializeField] private float moveY;
    [SerializeField] private float moveZ;

    [Header("ROTATE")] [SerializeField] private float rotateY;

    [Header("START POSITION")] [SerializeField]
    private Transform centerOfSin;

    private float _time;

    void Start()
    {
        if (centerOfSin != null)
        {
            //metodo para ir hasta el centerposition
            transform.position = centerOfSin.position;
        }
    }

    void Update()
    {
        _time += Time.deltaTime * speed;

        //Move
        float x = moveX != 0 ? Mathf.Sin(_time) * moveX : 0f;
        float y = moveY != 0 ? Mathf.Sin(_time) * moveY : 0f;
        float z = moveZ != 0 ? Mathf.Sin(_time) * moveZ : 0f;

        //Rotation
        float ry = rotateY != 0 ? rotateY * 0.01f * _time : 0f;

        transform.position = centerOfSin.position + new Vector3(x, y, z);
        transform.Rotate(0, ry, 0);
    }

    private void ActivatePlatform()
    {
        transform.Rotate(0f, 90f, 0f);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("PlayerFather"))
        {
            other.transform.SetParent(transform);
        }
    }

    private void OnTriggerStay(Collider other)
    {
        throw new NotImplementedException();
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.CompareTag("PlayerFather"))
        {
            other.transform.SetParent(null);
        }
    }
}