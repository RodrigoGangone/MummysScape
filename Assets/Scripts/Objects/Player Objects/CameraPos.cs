using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraPos : MonoBehaviour
{
    [SerializeField] private Transform camOne, camTwo;
    [SerializeField] private Transform _player;

    void Update()
    {
        Vector3 desiredPosition = _player.position + new Vector3(0, 10, -20);
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, 0.125f);
        transform.position = smoothedPosition;


        transform.LookAt(_player);

        //if (Input.GetKey(KeyCode.Q))
        //{
        //    if (Vector3.Distance(transform.position, camOne.position) > 0.1f)
        //    {
        //        transform.position = Vector3.Lerp(transform.position, camOne.position, 0.001f);
        //        transform.rotation = Quaternion.Lerp(transform.rotation, camOne.rotation, 0.001f);
        //    }
        //        
        //}
        //if (Input.GetKey(KeyCode.E))
        //{
        //    if (Vector3.Distance(transform.position, camTwo.position) > 0.1f)
        //    {
        //        transform.position = Vector3.Lerp(transform.position, camTwo.position, 0.001f);
        //        transform.rotation = Quaternion.Lerp(transform.rotation, camTwo.rotation, 0.001f);
        //    }
        //}
    }
}