using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

public class Quicksand : MonoBehaviour
{
    [SerializeField] private Transform _startPosInvisible;
    [SerializeField] private List<Transform> _sandPos;
    
    [SerializeField] private float _speedSink;
    
    private bool _onQuicksand;
    private Player _player;
    private float _time;

    private Transform _invisiblePlatform;

    private void Start()
    {
        _invisiblePlatform = transform.Find("QuicksandPlatform");
    }

    private void OnCollisionEnter(Collision other)
    {
        if (!other.gameObject.CompareTag("PlayerFather")) return;
        
        _player = other.gameObject.GetComponent<Player>();
        _player.ChangeSpeed();
        _onQuicksand = true;
        other.transform.SetParent(transform);
    }

    private void NextPosSand()
    {
        
    }
    
    private void Update()
    {
        if (!_onQuicksand) return;
        DownPlatform();
    }

    private void DownPlatform()
    {
        _time += Time.deltaTime * _speedSink;

        var y = _speedSink != 0 ? _time : 0f;
        transform.position = _startPosInvisible.position + new Vector3(0, -y, 0);
    }

    private void OnCollisionExit(Collision other)
    {
        if (other.gameObject.CompareTag("PlayerFather")) ;
        {
            _player = other.gameObject.GetComponent<Player>();
            _player.Speed = _player.SpeedOriginal;
            _onQuicksand = false;
            other.transform.SetParent(null);
            transform.position = _startPosInvisible.position;
        }
    }
}