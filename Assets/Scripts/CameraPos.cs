using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraPos : MonoBehaviour
{
    [SerializeField] private Transform camOne, camTwo;
    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.J))
        {
            transform.position = camOne.transform.position;
            transform.rotation = camOne.transform.rotation;
        }
        if (Input.GetKeyDown(KeyCode.K))
        {
            transform.position = camTwo.transform.position;
            transform.rotation = camTwo.transform.rotation;
        }
    }
}
