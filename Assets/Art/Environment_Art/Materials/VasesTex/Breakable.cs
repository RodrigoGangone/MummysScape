using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Breakable : MonoBehaviour
{
    [SerializeField] private GameObject _destroyeVersion;
    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.B))
        {
            Instantiate(_destroyeVersion, transform.position, transform.rotation);
            Destroy(gameObject);
        }
    }
}
