using UnityEngine;

public class MovePlatform : MonoBehaviour
{
    [Header("MOVE")]
    [SerializeField] private float speed = 1.0f;
    [SerializeField] private float moveX;
    [SerializeField] private float moveY;
    [SerializeField] private float moveZ;
    
    [Header("ROTATE")]
    [SerializeField] private bool rotateYAxi;

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

    private void Rotate()
    {
        transform.Rotate(0f,90f,0f);
    }
}