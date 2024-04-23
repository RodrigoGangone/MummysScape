using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectPool : MonoBehaviour
{

    public static ObjectPool instance;

    private List<GameObject> _pooledObjectsBullet = new List<GameObject>();
    
    private int _amountBullet = 5;

    [SerializeField] private GameObject _bulletBandage;

    private void Awake()
    {
        if (instance == null) { instance = this; }
    }

    private void Start()
    {
        for (int i = 0; i < _amountBullet; i++)
        {
            GameObject obj = Instantiate(_bulletBandage);
            obj.SetActive(false);
            _pooledObjectsBullet.Add(obj);
        }
    }

    public GameObject GetPooledObjectBullet()
    {
        for (int i = 0; i < _pooledObjectsBullet.Count; i++)
        {
            if (!_pooledObjectsBullet[i].activeInHierarchy) { return _pooledObjectsBullet[i]; }
        }
        
        return null;
    }
}
