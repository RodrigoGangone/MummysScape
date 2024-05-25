using System;
using UnityEngine;
using UnityEngine.Serialization;

public class MovePlatform : MonoBehaviour
{
    [Header("TYPE OF PLATFORM")] [SerializeField]
    private TypeOfPlatform _type;

    [Header("SPEED")] [SerializeField] private float speed = 1.0f;

    [Header("MOVE")] [SerializeField] private float moveX;
    [SerializeField] private float moveY;
    [SerializeField] private float moveZ;

    [Header("ROTATE CONSTANT")] [SerializeField]
    private float _speedRotateY;

    [Header("ROTATE 90")] [SerializeField] private float _rotateY;
    private Quaternion _destiny;

    [Header("START POSITION")] [SerializeField]
    private Transform centerOfSin;

    private bool isActive;
    private float _time;

    void Start()
    {
        //metodo para ir hasta el centerposition
        if (centerOfSin != null)
            transform.position = centerOfSin.position;
    }

    public void StartAction()
    {
        isActive = !isActive;
        
        _destiny = Quaternion.Euler(0f, transform.eulerAngles.y + _rotateY, 0f);
    }

    private void MoveInAxis()
    {
        _time += Time.deltaTime * speed;

        //Move
        float x = moveX != 0 ? Mathf.Sin(_time) * moveX : 0f;
        float y = moveY != 0 ? Mathf.Sin(_time) * moveY : 0f;
        float z = moveZ != 0 ? Mathf.Sin(_time) * moveZ : 0f;

        transform.position = centerOfSin.position + new Vector3(x, y, z);
    }

    private void RotateConstant()
    {
        //Rotation
        float ry = _speedRotateY != 0 ? _speedRotateY * 0.01f : 0f;

        transform.Rotate(0, ry, 0);
    }

    private void Rotate90()
    {
        transform.rotation = Quaternion.Lerp(transform.rotation, _destiny, 2 * Time.deltaTime);
    }

    void Update()
    {
        if (_type.Equals(TypeOfPlatform.Rotate90))
        {
            Rotate90();
        }

        if (!isActive)
            return;

        switch (_type)
        {
            case TypeOfPlatform.MoveAxis:
            {
                MoveInAxis();
                break;
            }
            case TypeOfPlatform.RotateConstant:
            {
                RotateConstant();
                break;
            }
            default:
                throw new ArgumentOutOfRangeException();
        }
    }


    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("PlayerFather"))
        {
            other.transform.SetParent(transform);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.CompareTag("PlayerFather"))
        {
            other.transform.SetParent(null);
        }
    }
}

public enum TypeOfPlatform
{
    MoveAxis,
    RotateConstant,
    Rotate90
}