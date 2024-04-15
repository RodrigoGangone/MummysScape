using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraPos : MonoBehaviour
{
    [SerializeField] private Transform camOne, camTwo;

    void Update()
    {
        if (Input.GetKey(KeyCode.J))
        {
            if (Vector3.Distance(transform.position, camOne.position) > 0.1f)
            {
                transform.position = Vector3.Lerp(transform.position, camOne.position, 0.001f);
                transform.rotation = Quaternion.Lerp(transform.rotation, camOne.rotation, 0.001f);
            }
                
        }
        if (Input.GetKey(KeyCode.K))
        {
            if (Vector3.Distance(transform.position, camTwo.position) > 0.1f)
            {
                transform.position = Vector3.Lerp(transform.position, camTwo.position, 0.001f);
                transform.rotation = Quaternion.Lerp(transform.rotation, camTwo.rotation, 0.001f);
            }
        }
    }
}
